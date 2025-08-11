namespace KbStore.Catalog.Abstractions.Services;

using Contracts;
using Exceptions;


/// <summary>
/// Provides methods to create, update, or remove product items.
/// </summary>
public interface IProductCommandService
{
    /// <summary>
    /// Creates a new product with the specified details.
    /// </summary>
    /// <exception cref="ProductValidationException">When business rules are violated</exception>
    /// <exception cref="ProductConflictException">When SKU already exists</exception>
    Task<ProductModel> CreateAsync(
        string sku,
        string? name,
        ProductDimensions? dimensions,
        Guid? inventoryItemId,
        int? stockThreshold,
        TimeSpan? leadTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the name for an existing product.
    /// </summary>
    /// <exception cref="ProductNotFoundException">When product doesn't exist</exception>
    /// <exception cref="ProductStateException">When product is discontinued</exception>
    Task<ProductModel> UpdateNameAsync(
        Guid productId,
        string? name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the dimensions for an existing product.
    /// </summary>
    /// <exception cref="ProductNotFoundException">When product doesn't exist</exception>
    /// <exception cref="ProductValidationException">When dimensions are invalid</exception>
    /// <exception cref="ProductStateException">When product is discontinued</exception>
    Task<ProductModel> UpdateDimensionsAsync(
        Guid productId,
        ProductDimensions? dimensions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the stock threshold for an existing product.
    /// </summary>
    /// <exception cref="ProductNotFoundException">When product doesn't exist</exception>
    /// <exception cref="ProductValidationException">When threshold is invalid</exception>
    /// <exception cref="ProductStateException">When product is discontinued</exception>
    Task<ProductModel> UpdateStockThresholdAsync(
        Guid productId,
        int? stockThreshold,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the lead time for an existing product.
    /// </summary>
    /// <exception cref="ProductNotFoundException">When product doesn't exist</exception>
    /// <exception cref="ProductValidationException">When lead time is invalid</exception>
    /// <exception cref="ProductStateException">When product is discontinued</exception>
    Task<ProductModel> UpdateLeadTimeAsync(
        Guid productId,
        TimeSpan? leadTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables a product, making it available for sale.
    /// </summary>
    /// <exception cref="ProductNotFoundException">When product doesn't exist</exception>
    /// <exception cref="ProductStateException">When product is already enabled or discontinued</exception>
    Task<ProductModel> EnableAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables a product, making it unavailable for sale.
    /// </summary>
    /// <exception cref="ProductNotFoundException">When product doesn't exist</exception>
    /// <exception cref="ProductStateException">When product is already disabled or discontinued</exception>
    Task<ProductModel> DisableAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a product as discontinued and removes it from active catalog.
    /// </summary>
    /// <exception cref="ProductNotFoundException">When product doesn't exist</exception>
    /// <exception cref="ProductStateException">When product cannot be deleted</exception>
    Task<ProductModel> DeleteAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of a product.
    /// </summary>
    /// <exception cref="ProductNotFoundException">When product doesn't exist</exception>
    Task<ProductModel> GetAsync(
        Guid productId,
        CancellationToken cancellationToken = default);
}