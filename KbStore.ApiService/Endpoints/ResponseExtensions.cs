namespace KbStore.ApiService.Endpoints;

using Contracts.Domains;


public static class ResponseExtensions
{
    public static IResult AsResult<TFailure>(this TFailure failure)
        where TFailure : RequestFailureBase
    {
        return failure.FailureType switch
        {
            FailureType.Conflict => Results.Conflict(failure),
            FailureType.Missing => Results.NotFound(failure),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}