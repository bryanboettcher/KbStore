namespace KbStore.Storefront.Abstractions.Exceptions;

/// <summary>
/// Base exception for all sellable item related errors.
/// </summary>
public abstract class SellableItemException : Exception
{
    protected SellableItemException(string message) : base(message) { }
    protected SellableItemException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a sellable item cannot be found by the specified identifier.
/// </summary>
public class SellableItemNotFoundException : SellableItemException
{
    public Guid SellableItemId { get; }

    public SellableItemNotFoundException(Guid sellableItemId)
        : base($"Sellable item with ID '{sellableItemId}' was not found.")
    {
        SellableItemId = sellableItemId;
        Data["sellableItemId"] = sellableItemId;
    }

    public SellableItemNotFoundException(Guid sellableItemId, Exception innerException)
        : base($"Sellable item with ID '{sellableItemId}' was not found.", innerException)
    {
        SellableItemId = sellableItemId;
        Data["sellableItemId"] = sellableItemId;
    }
}

/// <summary>
/// Thrown when business validation rules are violated for a sellable item operation.
/// </summary>
public class SellableItemValidationException : SellableItemException
{
    public string ValidationError { get; }

    public SellableItemValidationException(string message)
        : base(message)
    {
        ValidationError = message;
        Data["validationError"] = message;
    }

    public SellableItemValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
        ValidationError = message;
        Data["validationError"] = message;
    }
}

/// <summary>
/// Thrown when a sellable item operation cannot be performed due to the current state of the item.
/// </summary>
public class SellableItemStateException : SellableItemException
{
    public string CurrentState { get; }
    public string AttemptedAction { get; }

    public SellableItemStateException(string currentState, string attemptedAction)
        : base($"Cannot {attemptedAction} when sellable item is in {currentState} state.")
    {
        CurrentState = currentState;
        AttemptedAction = attemptedAction;
        Data["currentState"] = currentState;
        Data["attemptedAction"] = attemptedAction;
    }

    public SellableItemStateException(string currentState, string attemptedAction, Exception innerException)
        : base($"Cannot {attemptedAction} when sellable item is in {currentState} state.", innerException)
    {
        CurrentState = currentState;
        AttemptedAction = attemptedAction;
        Data["currentState"] = currentState;
        Data["attemptedAction"] = attemptedAction;
    }
}

/// <summary>
/// Thrown when a sellable item operation conflicts with existing data or business constraints.
/// </summary>
public class SellableItemConflictException : SellableItemException
{
    public string ConflictReason { get; }

    public SellableItemConflictException(string conflictReason)
        : base($"Sellable item operation conflict: {conflictReason}")
    {
        ConflictReason = conflictReason;
        Data["conflictReason"] = conflictReason;
    }

    public SellableItemConflictException(string conflictReason, Exception innerException)
        : base($"Sellable item operation conflict: {conflictReason}", innerException)
    {
        ConflictReason = conflictReason;
        Data["conflictReason"] = conflictReason;
    }

    public static SellableItemConflictException DuplicateSku(string sku)
        => new($"A sellable item with SKU '{sku}' already exists.")
        {
            Data = { { "sku", sku } }
        };
}

/// <summary>
/// Thrown when a sellable item operation fails for an unknown or unhandled reason.
/// </summary>
public class GenericSellableItemException : SellableItemException
{
    public GenericSellableItemException(string message) : base(message) { }
    public GenericSellableItemException(string message, Exception innerException) : base(message, innerException) { }
}
