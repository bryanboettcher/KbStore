namespace KbStore.Storefront.Abstractions.Interfaces;

using Contracts;
using Exceptions;


/// <summary>
/// Provides methods to create, update, or remove sellable items.
/// </summary>
public interface ISellableItemCommandService
{
    /// <summary>
    /// Creates a new sellable item with the specified details.
    /// </summary>
    /// <exception cref="SellableItemValidationException">When business rules are violated</exception>
    /// <exception cref="SellableItemConflictException">When SKU already exists</exception>
    Task<SellableItemResponse> CreateAsync(
        string sku,
        string name,
        string? description,
        decimal basePrice,
        string itemType,
        Dictionary<string, object?> payload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the name for an existing sellable item.
    /// </summary>
    /// <exception cref="SellableItemNotFoundException">When sellable item doesn't exist</exception>
    /// <exception cref="SellableItemStateException">When sellable item is discontinued</exception>
    Task<SellableItemResponse> UpdateNameAsync(
        Guid sellableItemId,
        string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the base price for an existing sellable item.
    /// </summary>
    /// <exception cref="SellableItemNotFoundException">When sellable item doesn't exist</exception>
    /// <exception cref="SellableItemValidationException">When price is invalid</exception>
    /// <exception cref="SellableItemStateException">When sellable item is discontinued</exception>
    Task<SellableItemResponse> UpdatePriceAsync(
        Guid sellableItemId,
        decimal basePrice,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a sellable item, making it visible to customers.
    /// </summary>
    /// <exception cref="SellableItemNotFoundException">When sellable item doesn't exist</exception>
    /// <exception cref="SellableItemStateException">When sellable item cannot be published</exception>
    Task<SellableItemResponse> PublishAsync(
        Guid sellableItemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hides a sellable item from customer view.
    /// </summary>
    /// <exception cref="SellableItemNotFoundException">When sellable item doesn't exist</exception>
    /// <exception cref="SellableItemStateException">When sellable item is discontinued</exception>
    Task<SellableItemResponse> HideAsync(
        Guid sellableItemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a sellable item as discontinued and removes it from active storefront.
    /// </summary>
    /// <exception cref="SellableItemNotFoundException">When sellable item doesn't exist</exception>
    /// <exception cref="SellableItemStateException">When sellable item cannot be discontinued</exception>
    Task<SellableItemResponse> DiscontinueAsync(
        Guid sellableItemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reinstates a discontinued sellable item back to active status.
    /// </summary>
    /// <exception cref="SellableItemNotFoundException">When sellable item doesn't exist</exception>
    /// <exception cref="SellableItemStateException">When sellable item is not discontinued</exception>
    Task<SellableItemResponse> ReinstateAsync(
        Guid sellableItemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a sellable item.
    /// </summary>
    /// <exception cref="SellableItemNotFoundException">When sellable item doesn't exist</exception>
    /// <exception cref="SellableItemStateException">When sellable item cannot be deleted</exception>
    Task DeleteAsync(
        Guid sellableItemId,
        CancellationToken cancellationToken = default);
}
