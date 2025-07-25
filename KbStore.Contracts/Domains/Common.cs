namespace KbStore.Contracts.Domains;

public interface RequestFailureBase
{
    string Message { get; }
    FailureType FailureType { get; }
}

public enum FailureType
{
    Conflict,
    Missing,
}