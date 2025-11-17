namespace KbStore.Catalog.Abstractions.Exceptions;

using KbStore.Catalog.Abstractions.Contracts;

/// <summary>
/// Base exception for all product related errors.
/// </summary>
public abstract class ProductException : Exception
{
    protected ProductException(string message) : base(message) { }
    protected ProductException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Generic product exception for unmapped fault types.
/// </summary>
public class GenericProductException : ProductException
{
    public GenericProductException(string message) : base(message) { }
    public GenericProductException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a product cannot be found by the specified identifier.
/// </summary>
public class ProductNotFoundException : ProductException
{
    public Guid ProductId { get; }

    public ProductNotFoundException(Guid productId)
        : base($"Product with ID '{productId}' was not found.")
    {
        ProductId = productId;
        Data["productId"] = productId;
    }

    public ProductNotFoundException(Guid productId, Exception innerException)
        : base($"Product with ID '{productId}' was not found.", innerException)
    {
        ProductId = productId;
        Data["productId"] = productId;
    }
}

/// <summary>
/// Thrown when business validation rules are violated for a product operation.
/// </summary>
public class ProductValidationException : ProductException
{
    public string ValidationError { get; }

    public ProductValidationException(string validationError)
        : base($"Product validation failed: {validationError}")
    {
        ValidationError = validationError;
        Data["validationError"] = validationError;
    }

    public ProductValidationException(string validationError, Exception innerException)
        : base($"Product validation failed: {validationError}", innerException)
    {
        ValidationError = validationError;
        Data["validationError"] = validationError;
    }

    // Convenience factory methods for common validation scenarios
    public static ProductValidationException InvalidSku(string? sku)
        => new($"SKU '{sku}' is invalid or empty.")
        {
            Data = { { "sku", sku } }
        };

    public static ProductValidationException InvalidName(string? name)
        => new($"Name '{name}' is invalid.")
        {
            Data = { { "name", name } }
        };

    public static ProductValidationException InvalidDimensions(ProductDimensions dimensions)
        => new($"Product dimensions are invalid: {dimensions}")
        {
            Data = { { "dimensions", dimensions } }
        };

    public static ProductValidationException InvalidStockThreshold(int? threshold)
        => new($"Stock threshold '{threshold}' must be greater than or equal to zero.")
        {
            Data = { { "stockThreshold", threshold } }
        };

    public static ProductValidationException InvalidLeadTime(TimeSpan? leadTime)
        => new($"Lead time '{leadTime}' must be a positive duration.")
        {
            Data = { { "leadTime", leadTime } }
        };
}

/// <summary>
/// Thrown when a product operation conflicts with existing data or business constraints.
/// </summary>
public class ProductConflictException : ProductException
{
    public string ConflictReason { get; }

    public ProductConflictException(string conflictReason)
        : base($"Product operation conflict: {conflictReason}")
    {
        ConflictReason = conflictReason;
        Data["conflictReason"] = conflictReason;
    }

    public ProductConflictException(string conflictReason, Exception innerException)
        : base($"Product operation conflict: {conflictReason}", innerException)
    {
        ConflictReason = conflictReason;
        Data["conflictReason"] = conflictReason;
    }

    // Convenience factory methods for common conflict scenarios
    public static ProductConflictException DuplicateSku(string sku, ProductModel? existingProduct = null)
        => new($"A product with SKU '{sku}' already exists.")
        {
            Data =
            {
                { "sku", sku },
                { "existingProductId", existingProduct?.ProductId },
                { "existingProduct", existingProduct }
            }
        };

    public static ProductConflictException InventoryItemAlreadyLinked(Guid inventoryId, ProductModel? existingProduct = null)
        => new($"Inventory item '{inventoryId}' is already linked to another product.")
        {
            Data =
            {
                { "inventoryId", inventoryId },
                { "existingProductId", existingProduct?.ProductId },
                { "existingProduct", existingProduct }
            }
        };
}

/// <summary>
/// Thrown when a product operation cannot be performed due to the current state of the product.
/// </summary>
public class ProductStateException : ProductException
{
    public Guid ProductId { get; }
    public string CurrentState { get; }
    public string AttemptedOperation { get; }

    public ProductStateException(Guid productId, string currentState, string attemptedOperation)
        : base($"Cannot perform '{attemptedOperation}' on product '{productId}' in state '{currentState}'.")
    {
        ProductId = productId;
        CurrentState = currentState;
        AttemptedOperation = attemptedOperation;
        Data["productId"] = productId;
        Data["currentState"] = currentState;
        Data["attemptedOperation"] = attemptedOperation;
    }

    public ProductStateException(Guid productId, string currentState, string attemptedOperation, Exception innerException)
        : base($"Cannot perform '{attemptedOperation}' on product '{productId}' in state '{currentState}'.", innerException)
    {
        ProductId = productId;
        CurrentState = currentState;
        AttemptedOperation = attemptedOperation;
        Data["productId"] = productId;
        Data["currentState"] = currentState;
        Data["attemptedOperation"] = attemptedOperation;
    }

    // Convenience factory methods for common state scenarios
    public static ProductStateException AlreadyEnabled(Guid productId)
        => new(productId, "Enabled", "Enable");

    public static ProductStateException AlreadyDisabled(Guid productId)
        => new(productId, "Disabled", "Disable");

    public static ProductStateException AlreadyDiscontinued(Guid productId)
        => new(productId, "Discontinued", "Update");

    public static ProductStateException CannotDeleteEnabledProduct(Guid productId)
        => new(productId, "Enabled", "Delete");

    public static ProductStateException CannotModifyDiscontinuedProduct(Guid productId, string operation)
        => new(productId, "Discontinued", operation);
}