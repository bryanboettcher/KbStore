namespace KbStore.Catalog.Abstractions.Services;

using Contracts;
using KbStore.Abstractions;


/// <summary>
/// Provides a variety of ways to locate inventory items from read models.
/// </summary>
public interface IInventoryQueryService
{
    /// <summary>
    /// Search through existing inventory items via pagination.
    /// </summary>
    Task<PaginatedResponse<InventoryModel>> SearchAsync<TQuery>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : PaginatedQuery;
}

/// <summary>
/// Basic pagination query for inventory items.
/// </summary>
public class PaginatedQuery
{
    public int Page { get; init; } = 0;
    public int Size { get; init; } = 25;
}


/// <summary>
/// Basic pagination query for products.
/// </summary>
public class ProductPaginatedQuery
{
    public int Page { get; init; } = 0;
    public int Size { get; init; } = 25;
}