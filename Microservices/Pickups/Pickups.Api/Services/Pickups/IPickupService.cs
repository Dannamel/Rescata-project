using Pickups.Api.Core;
using Pickups.Api.Core.Pagination;
using Pickups.Api.DTOs;

namespace Pickups.Api.Services.Pickups;

/// <summary>Casos de uso de las recogidas. Mismos métodos que IBookingService de Inmoby.</summary>
public interface IPickupService
{
    Task<Result<PickupDTO>> CreateAsync(Guid userId, CreatePickupDTO dto);

    Task<Result<PickupDTO>> GetOneAsync(Guid id, Guid userId, bool isAdmin, bool isBusiness);

    Task<Result<PaginationResponse<PickupDTO>>> GetPaginationAsync(PaginationRequest request, Guid userId, bool isAdmin, bool isBusiness);

    Task<Result<PickupDTO>> UpdateAsync(Guid id, Guid userId, UpdatePickupDTO dto, bool isAdmin);

    Task<Result<PickupDTO>> CancelAsync(Guid id, Guid userId, CancelPickupDTO dto, bool isAdmin);

    Task<Result<PickupDTO>> ConfirmAsync(Guid id, Guid userId, bool isAdmin);

    Task<Result<PickupDTO>> CompleteAsync(Guid id, Guid userId, bool isAdmin);
}
