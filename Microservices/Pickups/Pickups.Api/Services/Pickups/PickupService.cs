using Microsoft.EntityFrameworkCore;
using Pickups.Api.Core;
using Pickups.Api.Core.Pagination;
using Pickups.Api.Data;
using Pickups.Api.Data.Entities;
using Pickups.Api.DTOs;
using Pickups.Api.Services.Donations;

namespace Pickups.Api.Services.Pickups;

public sealed class PickupService : IPickupService
{
    private const string AvailableDonationStatus = "Available";

    private readonly DataContext _context;
    private readonly IDonationsClient _donationsClient;

    public PickupService(DataContext context, IDonationsClient donationsClient)
    {
        _context = context;
        _donationsClient = donationsClient;
    }

    public async Task<Result<PickupDTO>> CreateAsync(Guid userId, CreatePickupDTO dto)
    {
        DateTime scheduledAt = ToUtc(dto.ScheduledAt);

        if (scheduledAt <= DateTime.UtcNow)
        {
            return Result.Failure<PickupDTO>(
                Error.Validation("Pickups.InvalidDate", "La fecha de recogida debe ser futura."));
        }

        // Pickups le pregunta a Donations si la donación existe y está disponible
        Result<DonationSummaryDTO> donation = await _donationsClient.GetDonationAsync(dto.DonationId);

        if (donation.IsFailure)
        {
            return Result.Failure<PickupDTO>(donation.Error);
        }

        if (!string.Equals(donation.Value.Status, AvailableDonationStatus, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<PickupDTO>(
                Error.Validation("Pickups.DonationUnavailable", "La donación no está disponible para recogida."));
        }

        bool hasActivePickup = await _context.Pickups.AnyAsync(p => p.DonationId == dto.DonationId
                                                                 && (p.Status == PickupStatusEnum.Pending
                                                                     || p.Status == PickupStatusEnum.Confirmed));

        if (hasActivePickup)
        {
            return Result.Failure<PickupDTO>(
                Error.Conflict("Pickups.AlreadyScheduled", "La donación ya tiene una recogida activa."));
        }

        Pickup pickup = new()
        {
            Id = Guid.CreateVersion7(),
            DonationId = dto.DonationId,
            UserId = userId,
            ScheduledAt = scheduledAt,
            DurationInMinutes = dto.DurationInMinutes,
            Status = PickupStatusEnum.Pending,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.Pickups.Add(pickup);
        await _context.SaveChangesAsync();

        return Result.Success(ToDTO(pickup));
    }

    public async Task<Result<PickupDTO>> GetOneAsync(Guid id, Guid userId, bool isAdmin)
    {
        Result<Pickup> pickup = await FindOwnPickupAsync(id, userId, isAdmin);

        return pickup.IsFailure
            ? Result.Failure<PickupDTO>(pickup.Error)
            : Result.Success(ToDTO(pickup.Value));
    }

    public async Task<Result<PaginationResponse<PickupDTO>>> GetPaginationAsync(PaginationRequest request, Guid userId, bool isAdmin)
    {
        IQueryable<Pickup> query = _context.Pickups.AsQueryable();

        // El Admin ve todas las recogidas; los demás, solo las suyas
        if (!isAdmin)
        {
            query = query.Where(p => p.UserId == userId);
        }

        query = query.OrderByDescending(p => p.ScheduledAt);

        PaginationResponse<Pickup> page = await query.ToPagedListAsync(request);

        return Result.Success(page.Map(ToDTO));
    }

    public async Task<Result<PickupDTO>> UpdateAsync(Guid id, Guid userId, UpdatePickupDTO dto, bool isAdmin)
    {
        Result<Pickup> found = await FindOwnPickupAsync(id, userId, isAdmin);

        if (found.IsFailure)
        {
            return Result.Failure<PickupDTO>(found.Error);
        }

        Pickup pickup = found.Value;

        if (pickup.Status != PickupStatusEnum.Pending)
        {
            return Result.Failure<PickupDTO>(
                Error.Validation("Pickups.NotEditable", "Solo se pueden modificar recogidas pendientes."));
        }

        DateTime scheduledAt = ToUtc(dto.ScheduledAt);

        if (scheduledAt <= DateTime.UtcNow)
        {
            return Result.Failure<PickupDTO>(
                Error.Validation("Pickups.InvalidDate", "La fecha de recogida debe ser futura."));
        }

        pickup.ScheduledAt = scheduledAt;
        pickup.DurationInMinutes = dto.DurationInMinutes;
        pickup.Notes = dto.Notes;
        pickup.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Result.Success(ToDTO(pickup));
    }

    public async Task<Result<PickupDTO>> CancelAsync(Guid id, Guid userId, CancelPickupDTO dto, bool isAdmin)
    {
        Result<Pickup> found = await FindOwnPickupAsync(id, userId, isAdmin);

        if (found.IsFailure)
        {
            return Result.Failure<PickupDTO>(found.Error);
        }

        Pickup pickup = found.Value;

        if (pickup.Status is PickupStatusEnum.Cancelled or PickupStatusEnum.Completed)
        {
            return Result.Failure<PickupDTO>(
                Error.Validation("Pickups.NotCancellable", "No se puede cancelar una recogida cancelada o completada."));
        }

        pickup.Status = PickupStatusEnum.Cancelled;
        pickup.CancelReason = dto.CancelReason;
        pickup.CancelledAt = DateTime.UtcNow;
        pickup.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Result.Success(ToDTO(pickup));
    }

    public async Task<Result<PickupDTO>> ConfirmAsync(Guid id, Guid userId, bool isAdmin)
    {
        Result<Pickup> found = await FindDonationOwnerPickupAsync(id, userId, isAdmin);

        if (found.IsFailure)
        {
            return Result.Failure<PickupDTO>(found.Error);
        }

        Pickup pickup = found.Value;

        if (pickup.Status != PickupStatusEnum.Pending)
        {
            return Result.Failure<PickupDTO>(
                Error.Validation("Pickups.NotConfirmable", "Solo se pueden confirmar recogidas pendientes."));
        }

        pickup.Status = PickupStatusEnum.Confirmed;
        pickup.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Result.Success(ToDTO(pickup));
    }

    public async Task<Result<PickupDTO>> CompleteAsync(Guid id, Guid userId, bool isAdmin)
    {
        Result<Pickup> found = await FindDonationOwnerPickupAsync(id, userId, isAdmin);

        if (found.IsFailure)
        {
            return Result.Failure<PickupDTO>(found.Error);
        }

        Pickup pickup = found.Value;

        if (pickup.Status != PickupStatusEnum.Confirmed)
        {
            return Result.Failure<PickupDTO>(
                Error.Validation("Pickups.NotCompletable", "Solo se pueden completar recogidas confirmadas."));
        }

        pickup.Status = PickupStatusEnum.Completed;
        pickup.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Result.Success(ToDTO(pickup));
    }

    /// <summary>Busca la recogida: el Admin puede ver cualquiera; los demás, solo las que programaron.</summary>
    private async Task<Result<Pickup>> FindOwnPickupAsync(Guid id, Guid userId, bool isAdmin)
    {
        Pickup? pickup = await _context.Pickups.FirstOrDefaultAsync(p => p.Id == id);

        if (pickup is null)
        {
            return Result.Failure<Pickup>(
                Error.NotFound("Pickups.NotFound", "La recogida no existe."));
        }

        if (!isAdmin && pickup.UserId != userId)
        {
            return Result.Failure<Pickup>(
                Error.Forbidden("Pickups.Forbidden", "No tiene permiso sobre esta recogida."));
        }

        return Result.Success(pickup);
    }

    /// <summary>Busca la recogida: el Admin puede actuar sobre cualquiera; un negocio, solo si la donación es suya.</summary>
    private async Task<Result<Pickup>> FindDonationOwnerPickupAsync(Guid id, Guid userId, bool isAdmin)
    {
        Pickup? pickup = await _context.Pickups.FirstOrDefaultAsync(p => p.Id == id);

        if (pickup is null)
        {
            return Result.Failure<Pickup>(
                Error.NotFound("Pickups.NotFound", "La recogida no existe."));
        }

        if (isAdmin)
        {
            return Result.Success(pickup);
        }

        // El dueño de la donación (BusinessId) lo conoce Donations
        Result<DonationSummaryDTO> donation = await _donationsClient.GetDonationAsync(pickup.DonationId);

        if (donation.IsFailure)
        {
            return Result.Failure<Pickup>(donation.Error);
        }

        if (donation.Value.BusinessId != userId)
        {
            return Result.Failure<Pickup>(
                Error.Forbidden("Pickups.NotDonationOwner", "Solo el negocio dueño de la donación puede gestionar esta recogida."));
        }

        return Result.Success(pickup);
    }

    /// <summary>Lleva la fecha a UTC; si llega sin zona horaria, se asume que ya es UTC.</summary>
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static PickupDTO ToDTO(Pickup pickup) => new()
    {
        Id = pickup.Id,
        DonationId = pickup.DonationId,
        UserId = pickup.UserId,
        ScheduledAt = pickup.ScheduledAt,
        DurationInMinutes = pickup.DurationInMinutes,
        Status = pickup.Status,
        Notes = pickup.Notes,
        CreatedAt = pickup.CreatedAt,
        UpdatedAt = pickup.UpdatedAt,
        CancelledAt = pickup.CancelledAt,
        CancelReason = pickup.CancelReason
    };
}
