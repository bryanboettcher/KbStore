namespace KbStore.Inventory.Abstractions.Exceptions;

/// <summary>
/// Base exception for all stock item related errors.
/// </summary>
public abstract class StockItemException : Exception
{
    protected StockItemException(string message) : base(message) { }
    protected StockItemException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a stock item cannot be found by the specified identifier.
/// </summary>
public class StockItemNotFoundException : StockItemException
{
    public Guid ItemId { get; }

    public StockItemNotFoundException(Guid itemId)
        : base($"Stock item with ID '{itemId}' was not found.")
    {
        ItemId = itemId;
    }

    public StockItemNotFoundException(Guid itemId, Exception innerException)
        : base($"Stock item with ID '{itemId}' was not found.", innerException)
    {
        ItemId = itemId;
    }
}

/// <summary>
/// Thrown when business validation rules are violated for a stock item operation.
/// </summary>
public class StockItemValidationException : StockItemException
{
    public string ValidationError { get; }

    public StockItemValidationException(string validationError)
        : base($"Stock item validation failed: {validationError}")
    {
        ValidationError = validationError;
    }

    public StockItemValidationException(string validationError, Exception innerException)
        : base($"Stock item validation failed: {validationError}", innerException)
    {
        ValidationError = validationError;
    }

    // Convenience factory methods for common validation scenarios
    public static StockItemValidationException InvalidPartNumber(string partNumber)
        => new($"Part number '{partNumber}' is invalid or empty.");

    public static StockItemValidationException InvalidDescription(string description)
        => new($"Description '{description}' is invalid or empty.");

    public static StockItemValidationException InvalidQuantity(int quantity)
        => new($"Stock quantity '{quantity}' must be greater than or equal to zero.");
}

/// <summary>
/// Thrown when a stock item operation conflicts with existing data or business constraints.
/// </summary>
public class StockItemConflictException : StockItemException
{
    public string ConflictReason { get; }

    public StockItemConflictException(string conflictReason)
        : base($"Stock item operation conflict: {conflictReason}")
    {
        ConflictReason = conflictReason;
    }

    public StockItemConflictException(string conflictReason, Exception innerException)
        : base($"Stock item operation conflict: {conflictReason}", innerException)
    {
        ConflictReason = conflictReason;
    }

    // Convenience factory methods for common conflict scenarios
    public static StockItemConflictException DuplicatePartNumber(string partNumber)
        => new($"A stock item with part number '{partNumber}' already exists.");
}


/// <summary>
/// Thrown when a stock item operation cannot be performed due to the current state of the item.
/// </summary>
public class StockItemStateException : StockItemException
{
    public Guid ItemId { get; }
    public string CurrentState { get; }
    public string AttemptedOperation { get; }

    public StockItemStateException(Guid itemId, string currentState, string attemptedOperation)
        : base($"Cannot perform '{attemptedOperation}' on stock item '{itemId}' in state '{currentState}'.")
    {
        ItemId = itemId;
        CurrentState = currentState;
        AttemptedOperation = attemptedOperation;
    }

    public StockItemStateException(Guid itemId, string currentState, string attemptedOperation, Exception innerException)
        : base($"Cannot perform '{attemptedOperation}' on stock item '{itemId}' in state '{currentState}'.", innerException)
    {
        ItemId = itemId;
        CurrentState = currentState;
        AttemptedOperation = attemptedOperation;
    }

    // Convenience factory methods for common state scenarios
    public static StockItemStateException AlreadyHeld(Guid itemId) => new(itemId, "Held", "Hold");

    public static StockItemStateException NotHeld(Guid itemId) => new(itemId, "Available", "Release");

    public static StockItemStateException AlreadyDiscontinued(Guid itemId) => new(itemId, "Discontinued", "Update");

    public static StockItemStateException CannotDeleteHeldItem(Guid itemId) => new(itemId, "Held", "Delete");
}