using KbStore.Inventory.Abstractions.Contracts;

namespace KbStore.Inventory.Abstractions.Services;

using KbStore.Abstractions;

/// <summary>
/// Provides a variety of ways to locate StockItem instances.
/// </summary>
public interface IStockItemQueryService
{
    /// <summary>
    /// Search through existing StockItems via <typeparamref name="TQuery"/>.
    /// </summary>
    Task<PaginatedResponse<StockItemModel>> SearchAsync<TQuery>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : PaginatedRequest;
}