namespace KbStore.ApiService.Endpoints.Inventory;

using Contracts.Domains;
using MassTransit;
using Microsoft.AspNetCore.Mvc;


public static class StockItemEndpoints
{
    public static async Task<IResult> Create(
        CreateStockItemPayload payload,
        IRequestClient<CreateStockItemRequest> client,
        CancellationToken cancellationToken)
    {
        if (!payload.IsValid())
            return Results.BadRequest();

        Response response = await client.GetResponse<CreateStockItemResponse, StockItemFailure>(new
        {
            payload.PartNumber,
            payload.Description,
            payload.StockQuantity,
        }, cancellationToken);

        return response switch
        {
            (_, CreateStockItemResponse success) => Results.Ok(success),
            (_, StockItemFailure failure) => failure.AsResult(),
            _ => Results.InternalServerError("Unexpected response type from backend")
        };
    }

    public static async Task<IResult> UpdateQuantity(
        [FromRoute] Guid id,
        [FromRoute] int quantity,
        [FromServices] IRequestClient<UpdateStockItemQuantityRequest> client,
        CancellationToken cancellationToken = default)
    {
        if (quantity < 0)
            return Results.BadRequest();

        Response response = await client.GetResponse<UpdateStockItemResponse, StockItemFailure>(new
        {
            CorrelationId = id,
            StockQuantity = quantity
        }, cancellationToken);

        return response switch
        {
            (_, UpdateStockItemResponse success) => Results.Ok(success),
            (_, StockItemFailure failure) => failure.AsResult(),
            _ => Results.InternalServerError("Unexpected response type from backend")
        };
    }

    public static async Task<IResult> UpdateDescription(
        [FromRoute] Guid id,
        [FromBody] string? description,
        [FromServices] IRequestClient<UpdateStockItemDescriptionRequest> client,
        CancellationToken cancellationToken = default)
    {
        if (description == null)
            return Results.BadRequest();

        Response response = await client.GetResponse<UpdateStockItemResponse, StockItemFailure>(new
        {
            CorrelationId = id,
            Description = description
        }, cancellationToken);

        return response switch
        {
            (_, UpdateStockItemResponse success) => Results.Ok(success),
            (_, StockItemFailure failure) => failure.AsResult(),
            _ => Results.InternalServerError("Unexpected response type from backend")
        };
    }

    public static async Task<IResult> Delete(
        [FromRoute] Guid id,
        [FromServices] IRequestClient<DeleteStockItemRequest> client,
        CancellationToken cancellationToken = default)
    {
        Response response = await client.GetResponse<DeleteStockItemResponse, StockItemFailure>(new
        {
            CorrelationId = id,
        }, cancellationToken);

        return response switch
        {
            (_, DeleteStockItemResponse success) => Results.Ok(success),
            (_, StockItemFailure failure) => failure.AsResult(),
            _ => Results.InternalServerError("Unexpected response type from backend")
        };
    }

    public static async Task<IResult> GetById(
        [FromRoute] Guid id,
        [FromServices] IRequestClient<StockItemStatusRequest> client,
        CancellationToken cancellationToken = default
    )
    {
        Response response = await client.GetResponse<StockItemStatusResponse, StockItemFailure>(new
        {
            CorrelationId = id,
        }, cancellationToken);

        return response switch
        {
            (_, StockItemStatusResponse success) => Results.Ok(success),
            (_, StockItemFailure failure) => failure.AsResult(),
            _ => Results.InternalServerError("Unexpected response type from backend")
        };
    }

    public static void MapTo(WebApplication app)
    {
        app.MapPost("/StockItem", Create);
        app.MapGet("/StockItem/{id:guid}", GetById);
        app.MapPatch("/StockItem/{id:guid}/quantity/{quantity:int}", UpdateQuantity);
        app.MapPatch("/StockItem/{id:guid}/description", UpdateDescription);
        app.MapDelete("/StockItem/{id:guid}", Delete);
    }
}

public class CreateStockItemPayload
{
    public string? PartNumber { get; set; }
    public string? Description { get; set; }
    public int StockQuantity { get; set; }
    public bool IsValid() 
        => !string.IsNullOrWhiteSpace(PartNumber) && StockQuantity >= 0;
}