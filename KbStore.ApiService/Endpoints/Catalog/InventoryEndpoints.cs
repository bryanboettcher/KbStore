namespace KbStore.ApiService.Endpoints.Catalog;

using Abstractions;
using KbStore.Catalog.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;

public static class InventoryEndpoints
{
    public static async Task<IResult> Create(
        [FromBody] CreateInventoryPayload payload,
        [FromServices] IInventoryCommandService commandService,
        CancellationToken cancellationToken)
    {
        if (!payload.IsValid())
            return Results.BadRequest();

        var result = await commandService.CreateAsync(
            payload.PartNumber!,
            payload.Description!,
            payload.StockQuantity,
            cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    public static async Task<IResult> IncreaseQuantity(
        [FromRoute] Guid id,
        [FromRoute] int quantity,
        [FromServices] IInventoryCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            return Results.BadRequest("Quantity must be greater than zero");

        var result = await commandService.IncreaseQuantityAsync(id, quantity, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    public static async Task<IResult> DecreaseQuantity(
        [FromRoute] Guid id,
        [FromRoute] int quantity,
        [FromServices] IInventoryCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            return Results.BadRequest("Quantity must be greater than zero");

        var result = await commandService.DecreaseQuantityAsync(id, quantity, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    public static async Task<IResult> UpdateDescription(
        [FromRoute] Guid id,
        [FromBody] string? description,
        [FromServices] IInventoryCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(description))
            return Results.BadRequest("Description cannot be empty");

        var result = await commandService.UpdateDescriptionAsync(id, description, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    public static async Task<IResult> Hold(
        [FromRoute] Guid id,
        [FromServices] IInventoryCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        var result = await commandService.HoldAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    public static async Task<IResult> Release(
        [FromRoute] Guid id,
        [FromServices] IInventoryCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        var result = await commandService.ReleaseAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    public static async Task<IResult> Delete(
        [FromRoute] Guid id,
        [FromServices] IInventoryCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        var result = await commandService.DeleteAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    public static async Task<IResult> GetById(
        [FromRoute] Guid id,
        [FromServices] IInventoryCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        var result = await commandService.GetAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    public static async Task<IResult> GetAll(
        [AsParameters] PaginatedQuery pagination,
        [FromServices] IInventoryQueryService queryService,
        CancellationToken cancellationToken = default)
    {
        var result = await queryService.SearchAsync(pagination, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    public static void MapTo(WebApplication app)
    {
        var group = app.MapGroup("/inventory");

        // Create
        group.MapPost("", Create);

        // Read
        group.MapGet("{id:guid}", GetById);
        group.MapGet("", GetAll);

        // Update operations
        group.MapPatch("{id:guid}/increase/{quantity:int}", IncreaseQuantity);
        group.MapPatch("{id:guid}/decrease/{quantity:int}", DecreaseQuantity);
        group.MapPatch("{id:guid}/description", UpdateDescription);

        // State operations
        group.MapPatch("{id:guid}/hold", Hold);
        group.MapPatch("{id:guid}/release", Release);

        // Delete
        group.MapDelete("{id:guid}", Delete);
    }
}

public class CreateInventoryPayload
{
    public string? PartNumber { get; set; }
    public string? Description { get; set; }
    public int StockQuantity { get; set; }

    public bool IsValid()
        => !string.IsNullOrWhiteSpace(PartNumber)
        && !string.IsNullOrWhiteSpace(Description)
        && StockQuantity >= 0;
}