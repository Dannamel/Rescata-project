using Microsoft.Data.SqlClient;
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
    private const string ClaimedDonationStatus = "Claimed";
    private const string PickedUpDonationStatus = "PickedUp";

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

        Error donationError = ValidateDonation(donation.Value, scheduledAt, dto.DurationInMinutes);

        if (donationError != Error.None)
        {
            return Result.Failure<PickupDTO>(donationError);
        }

        // Revisión rápida para dar un mensaje claro; la garantía real es el índice único UX_Pickups_ActiveDonation
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
            BusinessId = donation.Value.BusinessId,
            ScheduledAt = scheduledAt,
            DurationInMinutes = dto.DurationInMinutes,
            Status = PickupStatusEnum.Pending,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.Pickups.Add(pickup);

        return await SaveAndMapAsync(pickup);
    }

    public async Task<Result<PickupDTO>> GetOneAsync(Guid id, Guid userId, bool isAdmin, bool isBusiness)
    {
        Result<Pickup> pickup = await FindVisiblePickupAsync(id, userId, isAdmin, isBusiness);

        return pickup.IsFailure
            ? Result.Failure<PickupDTO>(pickup.Error)
            : Result.Success(ToDTO(pickup.Value));
    }

    public async Task<Result<PaginationResponse<PickupDTO>>> GetPaginationAsync(PaginationRequest request, Guid userId, bool isAdmin, bool isBusiness)
    {
        IQueryable<Pickup> query = _context.Pickups.AsQueryable();

        // El Admin ve todas las recogidas; el negocio, las de sus donaciones; la fundación, las que programó
        if (!isAdmin)
        {
            query = isBusiness
                ? query.Where(p => p.BusinessId == userId)
                : query.Where(p => p.UserId == userId);
        }

        query = query.OrderByDescending(p => p.ScheduledAt);

        PaginationResponse<Pickup> page = await query.ToPagedListAsync(request);

        return Result.Success(page.Map(ToDTO));
    }

    // Donaciones que ya tienen una recogida pendiente o confirmada, sin importar qué fundación la programó
    public async Task<Result<List<Guid>>> GetActiveDonationIdsAsync()
    {
        List<Guid> donationIds = await _context.Pickups
            .Where(p => p.Status == PickupStatusEnum.Pending || p.Status == PickupStatusEnum.Confirmed)
            .Select(p => p.DonationId)
            .Distinct()
            .ToListAsync();

        return Result.Success(donationIds);
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

        // Al reprogramar, la donación debe seguir disponible y la nueva fecha debe respetar su límite
        Result<DonationSummaryDTO> donation = await _donationsClient.GetDonationAsync(pickup.DonationId);

        if (donation.IsFailure)
        {
            return Result.Failure<PickupDTO>(donation.Error);
        }

        Error donationError = ValidateDonation(donation.Value, scheduledAt, dto.DurationInMinutes);

        if (donationError != Error.None)
        {
            return Result.Failure<PickupDTO>(donationError);
        }

        pickup.ScheduledAt = scheduledAt;
        pickup.DurationInMinutes = dto.DurationInMinutes;
        pickup.Notes = dto.Notes;
        pickup.UpdatedAt = DateTime.UtcNow;

        return await SaveAndMapAsync(pickup);
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

        // Se consulta el estado real de la donación en vez de deducirlo del estado de la recogida:
        // - Claimed: la recogida estaba confirmada (o un intento de confirmar no alcanzó a guardarse). Se libera.
        // - Available: no hay reserva, o un intento anterior de cancelar ya la liberó sin alcanzar a guardar. No se llama.
        // Así, si el guardado falla, basta con reintentar la cancelación: no hace falta volver a reservar
        // la donación (la fundación no tiene permiso para reservar).
        Result<DonationSummaryDTO> donation = await _donationsClient.GetDonationAsync(pickup.DonationId);

        if (donation.IsFailure)
        {
            return Result.Failure<PickupDTO>(donation.Error);
        }

        if (HasStatus(donation.Value, ClaimedDonationStatus))
        {
            Result release = await _donationsClient.ReleaseAsync(pickup.DonationId);

            if (release.IsFailure)
            {
                return Result.Failure<PickupDTO>(release.Error);
            }
        }

        pickup.Status = PickupStatusEnum.Cancelled;
        pickup.CancelReason = dto.CancelReason;
        pickup.CancelledAt = DateTime.UtcNow;
        pickup.UpdatedAt = DateTime.UtcNow;

        return await SaveAndMapAsync(pickup);
    }

    public async Task<Result<PickupDTO>> ConfirmAsync(Guid id, Guid userId, bool isAdmin)
    {
        Result<PickupWithDonation> found = await FindDonationOwnerPickupAsync(id, userId, isAdmin);

        if (found.IsFailure)
        {
            return Result.Failure<PickupDTO>(found.Error);
        }

        (Pickup pickup, DonationSummaryDTO donation) = found.Value;

        if (pickup.Status != PickupStatusEnum.Pending)
        {
            return Result.Failure<PickupDTO>(
                Error.Validation("Pickups.NotConfirmable", "Solo se pueden confirmar recogidas pendientes."));
        }

        // Comunicación síncrona: Pickups le pide a Donations que reserve la donación (Available → Claimed).
        // Si Donations la rechaza (por ejemplo, ya venció), la recogida no se confirma.
        // Si ya está Claimed, un intento anterior la reservó sin alcanzar a guardar la recogida: no se vuelve a reservar.
        if (!HasStatus(donation, ClaimedDonationStatus))
        {
            Result claim = await _donationsClient.ClaimAsync(pickup.DonationId);

            if (claim.IsFailure)
            {
                return Result.Failure<PickupDTO>(claim.Error);
            }
        }

        pickup.Status = PickupStatusEnum.Confirmed;
        pickup.UpdatedAt = DateTime.UtcNow;

        Result<PickupDTO> saved = await SaveAndMapAsync(pickup);

        if (saved.IsFailure)
        {
            await ReleaseIfPickupWasCancelledAsync(pickup);
        }

        return saved;
    }

    public async Task<Result<PickupDTO>> CompleteAsync(Guid id, Guid userId, bool isAdmin)
    {
        Result<PickupWithDonation> found = await FindDonationOwnerPickupAsync(id, userId, isAdmin);

        if (found.IsFailure)
        {
            return Result.Failure<PickupDTO>(found.Error);
        }

        (Pickup pickup, DonationSummaryDTO donation) = found.Value;

        if (pickup.Status != PickupStatusEnum.Confirmed)
        {
            return Result.Failure<PickupDTO>(
                Error.Validation("Pickups.NotCompletable", "Solo se pueden completar recogidas confirmadas."));
        }

        // Comunicación síncrona: Pickups le pide a Donations que marque la donación como recogida (Claimed → PickedUp).
        // Si ya está PickedUp, un intento anterior la marcó sin alcanzar a guardar la recogida: no se vuelve a marcar
        // (Donations solo lo permite desde Claimed) y el reintento solo guarda la recogida como Completed.
        if (!HasStatus(donation, PickedUpDonationStatus))
        {
            Result pickedUp = await _donationsClient.MarkPickedUpAsync(pickup.DonationId);

            if (pickedUp.IsFailure)
            {
                return Result.Failure<PickupDTO>(pickedUp.Error);
            }
        }

        pickup.Status = PickupStatusEnum.Completed;
        pickup.UpdatedAt = DateTime.UtcNow;

        return await SaveAndMapAsync(pickup);
    }

    /// <summary>
    /// Reglas de la donación para crear o reprogramar: debe estar Available, no debe estar vencida
    /// y la recogida debe terminar antes de su límite (AvailableUntil).
    /// </summary>
    private static Error ValidateDonation(DonationSummaryDTO donation, DateTime scheduledAt, int durationInMinutes)
    {
        if (!HasStatus(donation, AvailableDonationStatus))
        {
            return Error.Validation("Pickups.DonationUnavailable", "La donación no está disponible para recogida.");
        }

        DateTime availableUntil = ToUtc(donation.AvailableUntil);

        if (availableUntil <= DateTime.UtcNow)
        {
            return Error.Validation("Pickups.DonationExpired", "La donación ya venció y no se puede recoger.");
        }

        // Se compara la duración con el tiempo que queda hasta el límite, en vez de sumarle minutos a la fecha:
        // con fechas extremas (9999-12-31) AddMinutes se desborda y lanza excepción (500).
        // Si la fecha es posterior al límite, la resta es negativa y también entra aquí.
        if (availableUntil - scheduledAt < TimeSpan.FromMinutes(durationInMinutes))
        {
            return Error.Validation("Pickups.AfterDonationDeadline",
                $"La recogida debe terminar antes del límite de la donación ({availableUntil:yyyy-MM-dd HH:mm} UTC).");
        }

        return Error.None;
    }

    private static bool HasStatus(DonationSummaryDTO donation, string status) =>
        string.Equals(donation.Status, status, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// La confirmación no se guardó. Si fue porque otra operación canceló la recogida al mismo tiempo,
    /// la reserva hecha en Donations ya no tiene recogida activa y se libera (con el token del negocio o del Admin,
    /// que sí pueden liberar). Si la recogida sigue Pending, la reserva se deja: el reintento de confirmar
    /// la reutiliza y cancelar la libera.
    /// </summary>
    private async Task ReleaseIfPickupWasCancelledAsync(Pickup pickup)
    {
        PickupStatusEnum? storedStatus = await _context.Pickups
            .AsNoTracking()
            .Where(p => p.Id == pickup.Id)
            .Select(p => (PickupStatusEnum?)p.Status)
            .FirstOrDefaultAsync();

        if (storedStatus == PickupStatusEnum.Cancelled)
        {
            await _donationsClient.ReleaseAsync(pickup.DonationId);
        }
    }

    private async Task<Result<PickupDTO>> SaveAndMapAsync(Pickup pickup)
    {
        Error saveError = await SaveAsync();

        return saveError == Error.None
            ? Result.Success(ToDTO(pickup))
            : Result.Failure<PickupDTO>(saveError);
    }

    /// <summary>Guarda y convierte en 409 los choques entre solicitudes simultáneas.</summary>
    private async Task<Error> SaveAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            return Error.None;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Otra operación cambió la recogida entre que la leímos y la guardamos (RowVersion distinto)
            return Error.Conflict("Pickups.ConcurrencyConflict",
                "La recogida fue modificada por otra operación al mismo tiempo. Consulte su estado e intente de nuevo.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // 2601/2627 = violación de índice único: otra solicitud ya creó la recogida activa de esta donación
            return Error.Conflict("Pickups.AlreadyScheduled", "La donación ya tiene una recogida activa.");
        }
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

    /// <summary>
    /// Busca la recogida para consultarla: el Admin ve cualquiera; la fundación, las que programó;
    /// el negocio, las de sus donaciones.
    /// </summary>
    private async Task<Result<Pickup>> FindVisiblePickupAsync(Guid id, Guid userId, bool isAdmin, bool isBusiness)
    {
        Pickup? pickup = await _context.Pickups.FirstOrDefaultAsync(p => p.Id == id);

        if (pickup is null)
        {
            return Result.Failure<Pickup>(
                Error.NotFound("Pickups.NotFound", "La recogida no existe."));
        }

        bool canSee = isAdmin
                      || pickup.UserId == userId
                      || (isBusiness && pickup.BusinessId == userId);

        if (!canSee)
        {
            return Result.Failure<Pickup>(
                Error.Forbidden("Pickups.Forbidden", "No tiene permiso sobre esta recogida."));
        }

        return Result.Success(pickup);
    }

    /// <summary>
    /// Busca la recogida y su donación: el Admin puede actuar sobre cualquiera; un negocio, solo si la donación es suya.
    /// La donación se devuelve también para conocer su estado actual en Donations.
    /// </summary>
    private async Task<Result<PickupWithDonation>> FindDonationOwnerPickupAsync(Guid id, Guid userId, bool isAdmin)
    {
        Pickup? pickup = await _context.Pickups.FirstOrDefaultAsync(p => p.Id == id);

        if (pickup is null)
        {
            return Result.Failure<PickupWithDonation>(
                Error.NotFound("Pickups.NotFound", "La recogida no existe."));
        }

        // El dueño de la donación (BusinessId) y su estado los conoce Donations
        Result<DonationSummaryDTO> donation = await _donationsClient.GetDonationAsync(pickup.DonationId);

        if (donation.IsFailure)
        {
            return Result.Failure<PickupWithDonation>(donation.Error);
        }

        if (!isAdmin && donation.Value.BusinessId != userId)
        {
            return Result.Failure<PickupWithDonation>(
                Error.Forbidden("Pickups.NotDonationOwner", "Solo el negocio dueño de la donación puede gestionar esta recogida."));
        }

        return Result.Success(new PickupWithDonation(pickup, donation.Value));
    }

    private sealed record PickupWithDonation(Pickup Pickup, DonationSummaryDTO Donation);

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
        BusinessId = pickup.BusinessId,
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
