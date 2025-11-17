namespace KbStore.Catalog.Abstractions.Services;

using Contracts;
using Exceptions;


/// <summary>
/// Provides methods to create, update, or remove inventory items.
/// </summary>
public interface IInventoryCommandService
{
    /// <summary>
    /// Creates a new inventory item with the specified details.
    /// </summary>
    /// <exception cref="InventoryValidationException">When business rules are violated</exception>
    /// <exception cref="InventoryConflictException">When part number already exists</exception>
    Task<InventoryModel> CreateAsync(
        string partNumber,
        string description,
        int stockQuantity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Increases the stock quantity for an existing inventory item.
    /// </summary>
    /// <exception cref="InventoryNotFoundException">When item doesn't exist</exception>
    /// <exception cref="InventoryStateException">When item cannot be modified (e.g., held items)</exception>
    Task<InventoryModel> IncreaseQuantityAsync(
        Guid inventoryId,
        int quantity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decreases the stock quantity for an existing inventory item.
    /// </summary>
    /// <exception cref="InventoryNotFoundException">When item doesn't exist</exception>
    /// <exception cref="InventoryValidationException">When quantity would go below zero</exception>
    /// <exception cref="InventoryStateException">When item cannot be modified (e.g., held items)</exception>
    Task<InventoryModel> DecreaseQuantityAsync(
        Guid inventoryId,
        int quantity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the description for an existing inventory item.
    /// </summary>
    /// <exception cref="InventoryNotFoundException">When item doesn't exist</exception>
    /// <exception cref="InventoryValidationException">When description is invalid</exception>
    Task<InventoryModel> UpdateDescriptionAsync(
        Guid inventoryId,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Places a hold on an inventory item, preventing quantity modifications.
    /// </summary>
    /// <exception cref="InventoryNotFoundException">When item doesn't exist</exception>
    /// <exception cref="InventoryStateException">When item is already held or discontinued</exception>
    Task<InventoryModel> HoldAsync(
        Guid inventoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a hold on an inventory item.
    /// </summary>
    /// <exception cref="InventoryNotFoundException">When item doesn't exist</exception>
    /// <exception cref="InventoryStateException">When item is not currently held</exception>
    Task<InventoryModel> ReleaseAsync(
        Guid inventoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an inventory item as discontinued and removes it from active inventory.
    /// </summary>
    /// <exception cref="InventoryNotFoundException">When item doesn't exist</exception>
    /// <exception cref="InventoryStateException">When item cannot be deleted</exception>
    Task<InventoryModel> DeleteAsync(
        Guid inventoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current live status of an inventory item.
    /// </summary>
    /// <exception cref="InventoryNotFoundException">When item doesn't exist</exception>
    Task<InventoryModel> GetAsync(
        Guid inventoryId,
        CancellationToken cancellationToken = default);
}