namespace KbStore.Abstractions;

public interface RequestFailureBase
{
    string Message { get; }
    FailureTypes FailureType { get; }
}

public enum FailureTypes
{
    /// <summary>
    /// The requested resource was not found.
    /// </summary>
    Missing,

    /// <summary>
    /// The operation conflicts with existing data (e.g., duplicate part numbers).
    /// </summary>
    Conflict,

    /// <summary>
    /// Input validation failed due to invalid data.
    /// </summary>
    Validation,

    /// <summary>
    /// The operation cannot be performed due to the current state of the resource.
    /// </summary>
    InvalidState,

    /// <summary>
    /// The operation is not permitted due to business rules or authorization.
    /// </summary>
    Forbidden,

    /// <summary>
    /// An unexpected error occurred during processing.
    /// </summary>
    InternalError
}