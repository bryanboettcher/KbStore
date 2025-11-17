namespace KbStore.Abstractions;

public interface RequestFailureBase
{
    /// <summary>
    /// A human-readable description of the error.
    /// </summary>
    string Message { get; }

    /// <summary>
    /// A high-level mapping of the classification of failure.
    /// </summary>
    FailureTypes FailureType { get; }

    /// <summary>
    /// An optional value with additional details.
    /// </summary>
    string? CurrentState { get; }

    /// <summary>
    /// An optional value with additional details.
    /// </summary>
    string? Operation { get; }
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