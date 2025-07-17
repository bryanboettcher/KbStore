namespace KbStore.ApiService.Endpoints.Inventory;

using Contracts.Domains;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using System.Threading;


public static class InventoryEndpoints
{
    public static async Task<IResult> Create(
        [FromBody] CreateInventoryPayload payload,
        [FromServices] IRequestClient<CreateInventoryRequest> client,
        CancellationToken cancellationToken = default)
    {
        if (!payload.IsValid())
            return Results.BadRequest();

        Response response = await client.GetResponse<CreateInventoryResponse, CreateInventoryFailure>(new
        {
            payload.PartNumber,
            payload.Description,
            payload.StockQuantity,
            payload.InventoryStatus
        }, cancellationToken);

        return response switch
        {
            (_, CreateInventoryResponse success) => Results.Ok(success),
            (_, CreateInventoryFailure failure) => Results.Conflict(failure),
            _ => Results.InternalServerError("Unexpected response type from backend")
        };
    }

    public static async Task<IResult> UpdateQuantity(
        [FromRoute] string partNumber,
        [FromRoute] int quantity,
        [FromServices] IRequestClient<UpdateInventoryQuantityRequest> client,
        CancellationToken cancellationToken = default)
    {
        Response response = await client.GetResponse<UpdateInventoryQuantityResponse, InventoryMissing>(new
        {
            PartNumber = partNumber,
            StockQuantity = quantity
        }, cancellationToken);

        return response switch
        {
            (_, UpdateInventoryQuantityResponse success) => Results.Ok(success),
            (_, InventoryMissing failure) => Results.NotFound(failure),
            _ => Results.InternalServerError("Unexpected response type from backend")
        };
    }

    public static async Task<IResult> Delete(
        [FromRoute] string partNumber,
        [FromServices] IRequestClient<DeleteInventoryRequest> client,
        CancellationToken cancellationToken = default)
    {
        Response response = await client.GetResponse<DeleteInventoryResponse, InventoryMissing>(new
        {
            PartNumber = partNumber,
        }, cancellationToken);

        return response switch
        {
            (_, DeleteInventoryResponse success) => Results.Ok(success),
            (_, InventoryMissing failure) => Results.NotFound(failure),
            _ => Results.InternalServerError("Unexpected response type from backend")
        };
    }

    public static async Task<IResult> GetById(
        [FromRoute] string partNumber,
        [FromServices] IRequestClient<InventoryStatusRequest> client,
        CancellationToken cancellationToken = default
    )
    {
        Response response = await client.GetResponse<InventoryStatusResponse, InventoryMissing>(new
        {
            PartNumber = partNumber
        }, cancellationToken);

        return response switch
        {
            (_, InventoryStatusResponse success) => Results.Ok(success),
            (_, InventoryMissing failure) => Results.NotFound(failure),
            _ => Results.InternalServerError("Unexpected response type from backend")
        };
    }

    public static void MapTo(WebApplication app)
    {
        app.MapPost("/inventory", Create);
        app.MapGet("/inventory/{partNumber}", GetById);
        app.MapPatch("/inventory/{partNumber}/quantity/{quantity:int}", UpdateQuantity);
        app.MapDelete("/inventory/{partNumber}", Delete);
    }
}

public class CreateInventoryPayload
{
    public string? PartNumber { get; set; }
    public string? Description { get; set; }
    public int StockQuantity { get; set; }
    public InventoryStatus InventoryStatus { get; set; }

    public bool IsValid() 
        => !string.IsNullOrEmpty(PartNumber) && StockQuantity >= 0;
}