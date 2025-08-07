namespace KbStore.ApiService.Endpoints;

using Abstractions;

public static class ResponseExtensions
{
    public static IResult AsResult<TFailure>(this TFailure failure)
        where TFailure : RequestFailureBase
    {
        return failure.FailureType switch
        {
            FailureTypes.Conflict => Results.Conflict(failure),
            FailureTypes.Missing => Results.NotFound(failure),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}