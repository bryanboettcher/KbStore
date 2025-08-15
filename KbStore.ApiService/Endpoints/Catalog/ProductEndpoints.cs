namespace KbStore.ApiService.Endpoints.Catalog;

using Abstractions;
using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;

public static class ProductEndpoints
{
    [ProducesResponseType<ProductModel>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> Create(
        [FromBody] CreateProductPayload payload,
        [FromServices] IProductCommandService commandService,
        CancellationToken cancellationToken)
    {
        if (!payload.IsValid())
            return Results.BadRequest();

        var result = await commandService.CreateAsync(
            payload.Sku!,
            payload.Name,
            payload.Dimensions, 
            payload.Quantity,
            payload.InventoryId,
            payload.StockThreshold,
            payload.LeadTime,
            cancellationToken
        ).ConfigureAwait(false);

        return Results.Ok(result);
    }

    [ProducesResponseType<ProductModel>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> UpdateName(
        [FromRoute] Guid id,
        [FromBody] string? name,
        [FromServices] IProductCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        var result = await commandService.UpdateNameAsync(id, name, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    [ProducesResponseType<ProductModel>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> UpdateDimensions(
        [FromRoute] Guid id,
        [FromBody] ProductDimensions? dimensions,
        [FromServices] IProductCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        var result = await commandService.UpdateDimensionsAsync(id, dimensions, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    [ProducesResponseType<ProductModel>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> UpdateQuantity(
        [FromRoute] Guid id,
        [FromBody] int quantity,
        [FromServices] IProductCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        var result = await commandService.UpdateQuantityAsync(id, quantity, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    [ProducesResponseType<ProductModel>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> UpdateStockThreshold(
        [FromRoute] Guid id,
        [FromBody] int? stockThreshold,
        [FromServices] IProductCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        if (stockThreshold < 0)
            return Results.BadRequest("Stock threshold must be greater than or equal to zero");

        var result = await commandService.UpdateStockThresholdAsync(id, stockThreshold, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    [ProducesResponseType<ProductModel>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> UpdateLeadTime(
        [FromRoute] Guid id,
        [FromBody] TimeSpan? leadTime,
        [FromServices] IProductCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        if (leadTime?.TotalSeconds < 0)
            return Results.BadRequest("Lead time must be a positive duration");

        var result = await commandService.UpdateLeadTimeAsync(id, leadTime, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    [ProducesResponseType<ProductModel>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> Enable(
        [FromRoute] Guid id,
        [FromServices] IProductCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        var result = await commandService.EnableAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    [ProducesResponseType<ProductModel>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> Disable(
        [FromRoute] Guid id,
        [FromServices] IProductCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        var result = await commandService.DisableAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    [ProducesResponseType<ProductModel>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> Delete(
        [FromRoute] Guid id,
        [FromServices] IProductCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        var result = await commandService.DeleteAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    [ProducesResponseType<ProductModel>(200)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> GetById(
        [FromRoute] Guid id,
        [FromServices] IProductCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        var result = await commandService.GetAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    [ProducesResponseType<PaginatedResponse<ProductModel>>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> GetAll(
        [AsParameters] ProductPaginatedQuery pagination,
        [FromServices] IProductQueryService queryService,
        CancellationToken cancellationToken = default)
    {
        var result = await queryService.SearchAsync(pagination, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    public static void MapTo(WebApplication app)
    {
        var group = app.MapGroup("/products");

        // Create
        group.MapPost("", Create);

        // Read
        group.MapGet("{id:guid}", GetById);
        group.MapGet("", GetAll);

        // Update operations
        group.MapPatch("{id:guid}/name", UpdateName);
        group.MapPatch("{id:guid}/dimensions", UpdateDimensions);
        group.MapPatch("{id:guid}/quantity", UpdateQuantity);
        group.MapPatch("{id:guid}/stock-threshold", UpdateStockThreshold);
        group.MapPatch("{id:guid}/lead-time", UpdateLeadTime);

        // State operations
        group.MapPatch("{id:guid}/enable", Enable);
        group.MapPatch("{id:guid}/disable", Disable);

        // Delete
        group.MapDelete("{id:guid}", Delete);
    }
}

public class CreateProductPayload
{
    public string? Sku { get; set; }
    public string? Name { get; set; }
    public ProductDimensions? Dimensions { get; set; }
    public int Quantity { get; set; }
    public Guid? InventoryId { get; set; }
    public int? StockThreshold { get; set; }
    public TimeSpan? LeadTime { get; set; }

    public bool IsValid()
        => !string.IsNullOrWhiteSpace(Sku)
        && (StockThreshold == null || StockThreshold >= 0)
        && (LeadTime == null || LeadTime.Value.TotalSeconds >= 0);
}