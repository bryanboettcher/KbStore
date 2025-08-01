namespace KbStore.Abstractions;

using Microsoft.AspNetCore.Mvc;


public class PaginatedRequest
{
    [FromQuery]
    public int Page { get; set; } = 0;

    [FromQuery]
    public int Size { get; set; } = 25;
}

public class PaginatedRequest<TQuery> : PaginatedRequest
{
    [FromQuery]
    public TQuery? Search { get; set; }
}


public class PaginatedResponse<TModel>
{
    public required int TotalItems { get; init; }
    public required int Page { get; init; }
    public required int Size { get; init; }
    public required IAsyncEnumerable<TModel> Results { get; init; }
}