namespace KbStore.ApiService.Endpoints.Inventory;

using Contracts.Domains;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using System.Threading;


public static class InventoryEndpoints
{
    public static async Task<IResult> Create(
        CreateInventoryPayload payload,
        IRequestClient<CreateInventoryRequest> client,
        CancellationToken cancellationToken)
    {
        if (!payload.IsValid())
            return Results.BadRequest();

        Response response = await client.GetResponse<CreateInventoryResponse, InventoryFailure>(new
        {
            payload.PartNumber,
            payload.Description,
            payload.StockQuantity,
            payload.InventoryStatus
        }, cancellationToken);

        return response switch
        {
            (_, CreateInventoryResponse success) => Results.Ok(success),
            (_, InventoryFailure failure) => failure.AsResult(),
            _ => Results.InternalServerError("Unexpected response type from backend")
        };
    }

    public static async Task<IResult> UpdateQuantity(
        [FromRoute] Guid id,
        [FromRoute] int quantity,
        [FromServices] IRequestClient<UpdateInventoryQuantityRequest> client,
        CancellationToken cancellationToken = default)
    {
        if (quantity < 0)
            return Results.BadRequest();

        Response response = await client.GetResponse<UpdateInventoryResponse, InventoryFailure>(new
        {
            CorrelationId = id,
            StockQuantity = quantity
        }, cancellationToken);

        return response switch
        {
            (_, UpdateInventoryResponse success) => Results.Ok(success),
            (_, InventoryFailure failure) => failure.AsResult(),
            _ => Results.InternalServerError("Unexpected response type from backend")
        };
    }

    public static async Task<IResult> UpdateDescription(
        [FromRoute] Guid id,
        [FromBody] string? description,
        [FromServices] IRequestClient<UpdateInventoryDescriptionRequest> client,
        CancellationToken cancellationToken = default)
    {
        if (description == null)
            return Results.BadRequest();

        Response response = await client.GetResponse<UpdateInventoryResponse, InventoryFailure>(new
        {
            CorrelationId = id,
            Description = description
        }, cancellationToken);

        return response switch
        {
            (_, UpdateInventoryResponse success) => Results.Ok(success),
            (_, InventoryFailure failure) => failure.AsResult(),
            _ => Results.InternalServerError("Unexpected response type from backend")
        };
    }

    public static async Task<IResult> Delete(
        [FromRoute] Guid id,
        [FromServices] IRequestClient<DeleteInventoryRequest> client,
        CancellationToken cancellationToken = default)
    {
        Response response = await client.GetResponse<DeleteInventoryResponse, InventoryFailure>(new
        {
            CorrelationId = id,
        }, cancellationToken);

        return response switch
        {
            (_, DeleteInventoryResponse success) => Results.Ok(success),
            (_, InventoryFailure failure) => failure.AsResult(),
            _ => Results.InternalServerError("Unexpected response type from backend")
        };
    }

    public static async Task<IResult> GetById(
        [FromRoute] Guid id,
        [FromServices] IRequestClient<InventoryStatusRequest> client,
        CancellationToken cancellationToken = default
    )
    {
        Response response = await client.GetResponse<InventoryStatusResponse, InventoryFailure>(new
        {
            CorrelationId = id,
        }, cancellationToken);

        return response switch
        {
            (_, InventoryStatusResponse success) => Results.Ok(success),
            (_, InventoryFailure failure) => failure.AsResult(),
            _ => Results.InternalServerError("Unexpected response type from backend")
        };
    }

    public static void MapTo(WebApplication app)
    {
        app.MapPost("/inventory", Create);
        app.MapGet("/inventory/{id:guid}", GetById);
        app.MapPatch("/inventory/{id:guid}/quantity/{quantity:int}", UpdateQuantity);
        app.MapPatch("/inventory/{id:guid}/description", UpdateDescription);
        app.MapDelete("/inventory/{id:guid}", Delete);
    }
}

public class CreateInventoryPayload
{
    public string? PartNumber { get; set; }
    public string? Description { get; set; }
    public int StockQuantity { get; set; }
    public InventoryStatus InventoryStatus { get; set; }

    public bool IsValid() 
        => !string.IsNullOrWhiteSpace(PartNumber) && StockQuantity >= 0;
}