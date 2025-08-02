namespace KbStore.Catalog.Abstractions.Exceptions;

/// <summary>
/// Base exception for all stock item related errors.
/// </summary>
public abstract class InventoryException : Exception
{
    protected InventoryException(string message) : base(message) { }
    protected InventoryException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a stock item cannot be found by the specified identifier.
/// </summary>
public class InventoryNotFoundException : InventoryException
{
    public Guid ItemId { get; }

    public InventoryNotFoundException(Guid itemId)
        : base($"Stock item with ID '{itemId}' was not found.")
    {
        ItemId = itemId;
    }

    public InventoryNotFoundException(Guid itemId, Exception innerException)
        : base($"Stock item with ID '{itemId}' was not found.", innerException)
    {
        ItemId = itemId;
    }
}

/// <summary>
/// Thrown when business validation rules are violated for a stock item operation.
/// </summary>
public class InventoryValidationException : InventoryException
{
    public string ValidationError { get; }

    public InventoryValidationException(string validationError)
        : base($"Stock item validation failed: {validationError}")
    {
        ValidationError = validationError;
    }

    public InventoryValidationException(string validationError, Exception innerException)
        : base($"Stock item validation failed: {validationError}", innerException)
    {
        ValidationError = validationError;
    }

    // Convenience factory methods for common validation scenarios
    public static InventoryValidationException InvalidPartNumber(string partNumber)
        => new($"Part number '{partNumber}' is invalid or empty.");

    public static InventoryValidationException InvalidDescription(string description)
        => new($"Description '{description}' is invalid or empty.");

    public static InventoryValidationException InvalidQuantity(int quantity)
        => new($"Stock quantity '{quantity}' must be greater than or equal to zero.");
}

/// <summary>
/// Thrown when a stock item operation conflicts with existing data or business constraints.
/// </summary>
public class InventoryConflictException : InventoryException
{
    public string ConflictReason { get; }

    public InventoryConflictException(string conflictReason)
        : base($"Stock item operation conflict: {conflictReason}")
    {
        ConflictReason = conflictReason;
    }

    public InventoryConflictException(string conflictReason, Exception innerException)
        : base($"Stock item operation conflict: {conflictReason}", innerException)
    {
        ConflictReason = conflictReason;
    }

    // Convenience factory methods for common conflict scenarios
    public static InventoryConflictException DuplicatePartNumber(string partNumber)
        => new($"A stock item with part number '{partNumber}' already exists.");
}


/// <summary>
/// Thrown when a stock item operation cannot be performed due to the current state of the item.
/// </summary>
public class InventoryStateException : InventoryException
{
    public Guid ItemId { get; }
    public string CurrentState { get; }
    public string AttemptedOperation { get; }

    public InventoryStateException(Guid itemId, string currentState, string attemptedOperation)
        : base($"Cannot perform '{attemptedOperation}' on stock item '{itemId}' in state '{currentState}'.")
    {
        ItemId = itemId;
        CurrentState = currentState;
        AttemptedOperation = attemptedOperation;
    }

    public InventoryStateException(Guid itemId, string currentState, string attemptedOperation, Exception innerException)
        : base($"Cannot perform '{attemptedOperation}' on stock item '{itemId}' in state '{currentState}'.", innerException)
    {
        ItemId = itemId;
        CurrentState = currentState;
        AttemptedOperation = attemptedOperation;
    }

    // Convenience factory methods for common state scenarios
    public static InventoryStateException AlreadyHeld(Guid itemId) => new(itemId, "Held", "Hold");

    public static InventoryStateException NotHeld(Guid itemId) => new(itemId, "Available", "Release");

    public static InventoryStateException AlreadyDiscontinued(Guid itemId) => new(itemId, "Discontinued", "Update");

    public static InventoryStateException CannotDeleteHeldItem(Guid itemId) => new(itemId, "Held", "Delete");
}