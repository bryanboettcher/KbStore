namespace KbStore.Storefront.Abstractions.Interfaces;

using Contracts;
using KbStore.Abstractions;


/// <summary>
/// Provides a variety of ways to locate sellable items from read models.
/// </summary>
public interface ISellableItemQueryService
{
    /// <summary>
    /// Retrieves a sellable item by its unique identifier.
    /// </summary>
    /// <returns>The sellable item if found; otherwise null.</returns>
    Task<SellableItemResponse?> GetByIdAsync(
        Guid sellableItemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a sellable item by its SKU (stock keeping unit).
    /// </summary>
    /// <returns>The sellable item if found; otherwise null.</returns>
    Task<SellableItemResponse?> GetBySkuAsync(
        string sku,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all sellable items.
    /// </summary>
    Task<List<SellableItemResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all sellable items of a specific type.
    /// </summary>
    Task<List<SellableItemResponse>> GetByItemTypeAsync(
        string itemType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all published (visible to customers) sellable items.
    /// </summary>
    Task<List<SellableItemResponse>> GetPublishedAsync(
        CancellationToken cancellationToken = default);
}
