using KbStore.Catalog.Abstractions.Contracts;

namespace KbStore.Catalog.Abstractions.Exceptions;

/// <summary>
/// Base exception for all inventory related errors.
/// </summary>
public abstract class InventoryException : Exception
{
    protected InventoryException(string message) : base(message) { }
    protected InventoryException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Generic inventory exception for unmapped fault types.
/// </summary>
public class GenericInventoryException : InventoryException
{
    public GenericInventoryException(string message) : base(message) { }
    public GenericInventoryException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when an inventory item cannot be found by the specified identifier.
/// </summary>
public class InventoryNotFoundException : InventoryException
{
    public Guid InventoryId { get; }

    public InventoryNotFoundException(Guid inventoryId)
        : base($"Inventory item with ID '{inventoryId}' was not found.")
    {
        InventoryId = inventoryId;
        Data["inventoryId"] = inventoryId;
    }

    public InventoryNotFoundException(Guid inventoryId, Exception innerException)
        : base($"Inventory item with ID '{inventoryId}' was not found.", innerException)
    {
        InventoryId = inventoryId;
        Data["inventoryId"] = inventoryId;
    }
}

/// <summary>
/// Thrown when business validation rules are violated for an inventory item operation.
/// </summary>
public class InventoryValidationException : InventoryException
{
    public string ValidationError { get; }

    public InventoryValidationException(string validationError)
        : base($"Inventory validation failed: {validationError}")
    {
        ValidationError = validationError;
        Data["validationError"] = validationError;
    }

    public InventoryValidationException(string validationError, Exception innerException)
        : base($"Inventory validation failed: {validationError}", innerException)
    {
        ValidationError = validationError;
        Data["validationError"] = validationError;
    }

    // Convenience factory methods for common validation scenarios
    public static InventoryValidationException InvalidPartNumber(string? partNumber)
        => new($"Part number '{partNumber}' is invalid or empty.")
        {
            Data = { { "partNumber", partNumber } }
        };

    public static InventoryValidationException InvalidDescription(string? description)
        => new($"Description '{description}' is invalid or empty.")
        {
            Data = { { "description", description } }
        };

    public static InventoryValidationException InvalidQuantity(int quantity)
        => new($"Stock quantity '{quantity}' must be greater than or equal to zero.")
        {
            Data = { { "quantity", quantity } }
        };

    public static InventoryValidationException InsufficientStock(int requested, int available, Guid inventoryId)
        => new($"Cannot decrease quantity by {requested}. Current stock: {available}")
        {
            Data =
            {
                { "requestedQuantity", requested },
                { "availableQuantity", available },
                { "inventoryId", inventoryId }
            }
        };
}

/// <summary>
/// Thrown when an inventory item operation conflicts with existing data or business constraints.
/// </summary>
public class InventoryConflictException : InventoryException
{
    public string ConflictReason { get; }

    public InventoryConflictException(string conflictReason)
        : base($"Inventory operation conflict: {conflictReason}")
    {
        ConflictReason = conflictReason;
        Data["conflictReason"] = conflictReason;
    }

    public InventoryConflictException(string conflictReason, Exception innerException)
        : base($"Inventory operation conflict: {conflictReason}", innerException)
    {
        ConflictReason = conflictReason;
        Data["conflictReason"] = conflictReason;
    }

    // Convenience factory methods for common conflict scenarios
    public static InventoryConflictException DuplicatePartNumber(string partNumber, InventoryModel? existingItem = null)
        => new($"A inventory item with part number '{partNumber}' already exists.")
        {
            Data =
            {
                { "partNumber", partNumber },
                { "existingInventoryId", existingItem?.InventoryId },
                { "existingItem", existingItem }
            }
        };
}

/// <summary>
/// Thrown when an inventory item operation cannot be performed due to the current state of the item.
/// </summary>
public class InventoryStateException : InventoryException
{
    public Guid InventoryId { get; }
    public string CurrentState { get; }
    public string AttemptedOperation { get; }

    public InventoryStateException(Guid inventoryId, string currentState, string attemptedOperation)
        : base($"Cannot perform '{attemptedOperation}' on inventory item '{inventoryId}' in state '{currentState}'.")
    {
        InventoryId = inventoryId;
        CurrentState = currentState;
        AttemptedOperation = attemptedOperation;
        Data["inventoryId"] = inventoryId;
        Data["currentState"] = currentState;
        Data["attemptedOperation"] = attemptedOperation;
    }

    public InventoryStateException(Guid inventoryId, string currentState, string attemptedOperation, Exception innerException)
        : base($"Cannot perform '{attemptedOperation}' on inventory item '{inventoryId}' in state '{currentState}'.", innerException)
    {
        InventoryId = inventoryId;
        CurrentState = currentState;
        AttemptedOperation = attemptedOperation;
        Data["inventoryId"] = inventoryId;
        Data["currentState"] = currentState;
        Data["attemptedOperation"] = attemptedOperation;
    }

    // Convenience factory methods for common state scenarios
    public static InventoryStateException AlreadyHeld(Guid inventoryId)
        => new(inventoryId, "Held", "Hold");

    public static InventoryStateException NotHeld(Guid inventoryId)
        => new(inventoryId, "Available", "Release");

    public static InventoryStateException AlreadyDiscontinued(Guid inventoryId)
        => new(inventoryId, "Discontinued", "Update");

    public static InventoryStateException CannotDeleteHeldItem(Guid inventoryId)
        => new(inventoryId, "Held", "Delete");

    public static InventoryStateException CannotModifyHeldItem(Guid inventoryId, string operation)
        => new(inventoryId, "Held", operation);

    public static InventoryStateException CannotModifyDiscontinuedItem(Guid inventoryId, string operation)
        => new(inventoryId, "Discontinued", operation);

    public static InventoryStateException CannotModifyBackorderedItem(Guid inventoryId, string operation)
        => new(inventoryId, "Backordered", operation);
}