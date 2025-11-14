namespace KbStore.ApiService.Consumers.Catalog;

using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Storefront.Abstractions.Exceptions;
using KbStore.Storefront.Abstractions.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;


public class ProductDiscontinuedConsumer : IConsumer<ProductDiscontinued>
{
    private readonly ISellableItemCommandService _sellableItemService;
    private readonly ISellableItemQueryService _sellableItemQueryService;
    private readonly ILogger<ProductDiscontinuedConsumer> _logger;

    public ProductDiscontinuedConsumer(
        ISellableItemCommandService sellableItemService,
        ISellableItemQueryService sellableItemQueryService,
        ILogger<ProductDiscontinuedConsumer> logger)
    {
        _sellableItemService = sellableItemService;
        _sellableItemQueryService = sellableItemQueryService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductDiscontinued> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "Discontinuing SellableItem for Product {ProductId}, SKU {SKU}",
            msg.ProductId,
            msg.Sku);

        try
        {
            // Verify SellableItem exists before discontinuing
            var existing = await _sellableItemQueryService.GetByIdAsync(
                msg.ProductId,
                context.CancellationToken);

            if (existing is null)
            {
                // SellableItem doesn't exist - log and skip
                _logger.LogWarning(
                    "SellableItem not found for Product {ProductId} - cannot discontinue (may not have been created yet)",
                    msg.ProductId);
                return; // Nothing to discontinue
            }

            // Discontinue the SellableItem (soft-delete preserves order history)
            await _sellableItemService.DiscontinueAsync(
                sellableItemId: msg.ProductId,
                cancellationToken: context.CancellationToken);

            _logger.LogInformation(
                "Successfully discontinued SellableItem for Product {ProductId}",
                msg.ProductId);
        }
        catch (SellableItemNotFoundException ex)
        {
            // SellableItem disappeared between check and discontinue - log and skip
            _logger.LogWarning(
                ex,
                "SellableItem not found for Product {ProductId} during discontinue",
                msg.ProductId);
            // Do NOT rethrow - this is a valid scenario in distributed systems
        }
        catch (SellableItemStateException ex)
        {
            // SellableItem may already be discontinued (idempotency)
            _logger.LogWarning(
                ex,
                "Cannot discontinue SellableItem for Product {ProductId} - may already be discontinued",
                msg.ProductId);
            // Do NOT rethrow - idempotent operation
        }
        catch (Exception ex)
        {
            // Transient failures should be retried by MassTransit
            _logger.LogError(
                ex,
                "Failed to discontinue SellableItem for Product {ProductId}",
                msg.ProductId);
            throw; // Let MassTransit retry
        }
    }
}
