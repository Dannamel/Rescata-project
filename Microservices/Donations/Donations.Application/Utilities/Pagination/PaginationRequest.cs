namespace Donations.Application.Utilities.Pagination;

/// <summary>Parámetros de paginación (pageNumber empieza en 1).</summary>
public sealed record PaginationRequest
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 50;

    public int PageNumber { get; }
    public int PageSize { get; }

    public PaginationRequest(int pageNumber = 1, int pageSize = DefaultPageSize)
    {
        PageNumber = pageNumber < 1 ? 1 : pageNumber;
        PageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
    }

    public int Skip => (PageNumber - 1) * PageSize;
}
