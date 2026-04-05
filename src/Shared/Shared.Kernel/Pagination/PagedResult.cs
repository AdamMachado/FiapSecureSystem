namespace Shared.Kernel.Pagination;

public class PagedResult<T>
{
    public PagedResult(
        IReadOnlyCollection<T> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public IReadOnlyCollection<T> Items { get; }
    public int TotalCount { get; }
    public int PageNumber { get; }
    public int PageSize { get; }

    public int TotalPages =>
        PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public static PagedResult<T> Create(
        IReadOnlyCollection<T> items,
        int totalCount,
        int pageNumber,
        int pageSize)
        => new(items, totalCount, pageNumber, pageSize);

    public static PagedResult<T> Empty(int pageNumber, int pageSize)
        => new(Array.Empty<T>(), 0, pageNumber, pageSize);
}