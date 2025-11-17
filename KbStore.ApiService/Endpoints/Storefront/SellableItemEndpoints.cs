namespace KbStore.ApiService.Endpoints.Storefront;

using KbStore.Storefront.Abstractions.Contracts;
using KbStore.Storefront.Abstractions.Interfaces;
using Microsoft.AspNetCore.Mvc;

public static class SellableItemEndpoints
{
    [ProducesResponseType<SellableItemModel>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> Create(
        [FromBody] CreateSellableItemPayload payload,
        [FromServices] ISellableItemCommandService commandService,
        CancellationToken cancellationToken)
    {
        if (!payload.IsValid())
            return Results.BadRequest();

        var result = await commandService.CreateAsync(
            payload.Sku!,
            payload.Name!,
            payload.Description,
            payload.BasePrice,
            payload.ItemType!,
            payload.Payload ?? new Dictionary<string, object?>(),
            payload.ProductId,
            cancellationToken
        ).ConfigureAwait(false);

        return Results.Ok(result);
    }

    [ProducesResponseType<SellableItemModel>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> UpdateName(
        [FromRoute] Guid id,
        [FromBody] UpdateNamePayload payload,
        [FromServices] ISellableItemCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        if (!payload.IsValid())
            return Results.BadRequest("Name is required");

        var result = await commandService.UpdateNameAsync(id, payload.Name!, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    [ProducesResponseType<SellableItemModel>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> UpdateDescription(
        [FromRoute] Guid id,
        [FromBody] UpdateDescriptionPayload payload,
        [FromServices] ISellableItemCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        var result = await commandService.UpdateDescriptionAsync(id, payload.Description, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    [ProducesResponseType<SellableItemModel>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> UpdatePrice(
        [FromRoute] Guid id,
        [FromBody] UpdatePricePayload payload,
        [FromServices] ISellableItemCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        if (!payload.IsValid())
            return Results.BadRequest("Price must be greater than or equal to zero");

        var result = await commandService.UpdatePriceAsync(id, payload.BasePrice, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    [ProducesResponseType<SellableItemModel>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> UpdatePayload(
        [FromRoute] Guid id,
        [FromBody] UpdatePayloadPayload payload,
        [FromServices] ISellableItemCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        if (!payload.IsValid())
            return Results.BadRequest("Payload is required");

        var result = await commandService.UpdatePayloadAsync(id, payload.Payload!, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    [ProducesResponseType<SellableItemModel>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> Publish(
        [FromRoute] Guid id,
        [FromServices] ISellableItemCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        var result = await commandService.PublishAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    [ProducesResponseType<SellableItemModel>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> Hide(
        [FromRoute] Guid id,
        [FromServices] ISellableItemCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        var result = await commandService.HideAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    [ProducesResponseType<SellableItemModel>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> Discontinue(
        [FromRoute] Guid id,
        [FromServices] ISellableItemCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        var result = await commandService.DiscontinueAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    [ProducesResponseType<SellableItemModel>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> Reinstate(
        [FromRoute] Guid id,
        [FromServices] ISellableItemCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        var result = await commandService.ReinstateAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> Delete(
        [FromRoute] Guid id,
        [FromServices] ISellableItemCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        await commandService.DeleteAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return Results.NoContent();
    }

    [ProducesResponseType<SellableItemModel>(200)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> GetById(
        [FromRoute] Guid id,
        [FromServices] ISellableItemQueryService queryService,
        CancellationToken cancellationToken = default)
    {
        var result = await queryService.GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }

    [ProducesResponseType<SellableItemModel>(200)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> GetBySku(
        [FromQuery] string sku,
        [FromServices] ISellableItemQueryService queryService,
        CancellationToken cancellationToken = default)
    {
        var result = await queryService.GetBySkuAsync(sku, cancellationToken)
            .ConfigureAwait(false);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }

    [ProducesResponseType<List<SellableItemModel>>(200)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> GetAll(
        [FromServices] ISellableItemQueryService queryService,
        CancellationToken cancellationToken = default)
    {
        var result = await queryService.GetAllAsync(cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    public static void MapTo(WebApplication app)
    {
        var group = app.MapGroup("/sellableitems");

        // Create
        group.MapPost("", Create);

        // Read
        group.MapGet("{id:guid}", GetById);
        group.MapGet("by-sku", GetBySku);
        group.MapGet("", GetAll);

        // Update operations
        group.MapPatch("{id:guid}/name", UpdateName);
        group.MapPatch("{id:guid}/description", UpdateDescription);
        group.MapPatch("{id:guid}/price", UpdatePrice);
        group.MapPatch("{id:guid}/payload", UpdatePayload);

        // State operations
        group.MapPatch("{id:guid}/publish", Publish);
        group.MapPatch("{id:guid}/hide", Hide);
        group.MapPatch("{id:guid}/discontinue", Discontinue);
        group.MapPatch("{id:guid}/reinstate", Reinstate);

        // Delete
        group.MapDelete("{id:guid}", Delete);
    }
}

public class CreateSellableItemPayload
{
    public string? Sku { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public string? ItemType { get; set; }
    public IReadOnlyDictionary<string, object?>? Payload { get; set; }
    public Guid? ProductId { get; set; }

    public bool IsValid()
        => !string.IsNullOrWhiteSpace(Sku)
        && !string.IsNullOrWhiteSpace(Name)
        && !string.IsNullOrWhiteSpace(ItemType)
        && BasePrice >= 0;
}

public class UpdateNamePayload
{
    public string? Name { get; set; }

    public bool IsValid()
        => !string.IsNullOrWhiteSpace(Name);
}

public class UpdateDescriptionPayload
{
    public string? Description { get; set; }
}

public class UpdatePricePayload
{
    public decimal BasePrice { get; set; }

    public bool IsValid()
        => BasePrice >= 0;
}

public class UpdatePayloadPayload
{
    public IReadOnlyDictionary<string, object?>? Payload { get; set; }

    public bool IsValid()
        => Payload is not null;
}
