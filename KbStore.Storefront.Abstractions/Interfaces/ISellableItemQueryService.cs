namespace KbStore.Storefront.Abstractions.Interfaces;

using Contracts;


/// <summary>
/// Provides a variety of ways to locate sellable items from read models.
/// </summary>
public interface ISellableItemQueryService
{
    /// <summary>
    /// Retrieves a sellable item by its unique identifier.
    /// </summary>
    /// <returns>The sellable item if found; otherwise null.</returns>
    Task<SellableItemModel?> GetByIdAsync(
        Guid sellableItemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a sellable item by its SKU (stock keeping unit).
    /// </summary>
    /// <returns>The sellable item if found; otherwise null.</returns>
    Task<SellableItemModel?> GetBySkuAsync(
        string sku,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all sellable items.
    /// </summary>
    Task<List<SellableItemModel>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all sellable items of a specific type.
    /// </summary>
    Task<List<SellableItemModel>> GetByItemTypeAsync(
        string itemType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all published (visible to customers) sellable items.
    /// </summary>
    Task<List<SellableItemModel>> GetPublishedAsync(
        CancellationToken cancellationToken = default);
}
