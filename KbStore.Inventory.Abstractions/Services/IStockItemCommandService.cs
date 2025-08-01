using KbStore.Inventory.Abstractions.Contracts;

namespace KbStore.Inventory.Abstractions.Services;

using Exceptions;


/// <summary>
/// Provides methods to create, update, or remove StockItems.
/// </summary>
public interface IStockItemCommandService
{
    /// <summary>
    /// Creates a new stock item with the specified details.
    /// </summary>
    /// <exception cref="StockItemValidationException">When business rules are violated</exception>
    /// <exception cref="StockItemConflictException">When part number already exists</exception>
    Task<StockItemModel> CreateAsync(
        string partNumber,
        string description,
        int stockQuantity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the stock quantity for an existing item.
    /// </summary>
    /// <exception cref="StockItemNotFoundException">When item doesn't exist</exception>
    /// <exception cref="StockItemValidationException">When quantity is invalid</exception>
    Task<StockItemModel> UpdateQuantityAsync(
        Guid itemId,
        int newQuantity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the description for an existing item.
    /// </summary>
    /// <exception cref="StockItemNotFoundException">When item doesn't exist</exception>
    /// <exception cref="StockItemValidationException">When description is invalid</exception>
    Task<StockItemModel> UpdateDescriptionAsync(
        Guid itemId,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Places a hold on a stock item, preventing modification.
    /// </summary>
    /// <exception cref="StockItemNotFoundException">When item doesn't exist</exception>
    /// <exception cref="StockItemStateException">When item is already held or discontinued</exception>
    Task<StockItemModel> HoldAsync(
        Guid itemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a hold on a stock item.
    /// </summary>
    /// <exception cref="StockItemNotFoundException">When item doesn't exist</exception>
    /// <exception cref="StockItemStateException">When item is not currently held</exception>
    Task<StockItemModel> ReleaseAsync(
        Guid itemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a stock item as discontinued and removes it from active inventory.
    /// </summary>
    /// <exception cref="StockItemNotFoundException">When item doesn't exist</exception>
    /// <exception cref="StockItemStateException">When item cannot be deleted (e.g., held items)</exception>
    Task<StockItemModel> DeleteAsync(
        Guid itemId,
        CancellationToken cancellationToken = default);
}