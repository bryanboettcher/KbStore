namespace KbStore.ApiService.Consumers.Catalog;

using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Storefront.Abstractions.Exceptions;
using KbStore.Storefront.Abstractions.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;


public class ProductNameUpdatedConsumer : IConsumer<ProductNameUpdated>
{
    private readonly ISellableItemCommandService _sellableItemService;
    private readonly ISellableItemQueryService _sellableItemQueryService;
    private readonly ILogger<ProductNameUpdatedConsumer> _logger;

    public ProductNameUpdatedConsumer(
        ISellableItemCommandService sellableItemService,
        ISellableItemQueryService sellableItemQueryService,
        ILogger<ProductNameUpdatedConsumer> logger)
    {
        _sellableItemService = sellableItemService;
        _sellableItemQueryService = sellableItemQueryService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductNameUpdated> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "Updating SellableItem name for Product {ProductId}, new name: {Name}",
            msg.ProductId,
            msg.Name);

        try
        {
            // Verify SellableItem exists (handle out-of-order events)
            var existing = await _sellableItemQueryService.GetByIdAsync(
                msg.ProductId,
                context.CancellationToken);

            if (existing is null)
            {
                // Create-if-missing pattern: SellableItem doesn't exist yet
                _logger.LogWarning(
                    "SellableItem not found for Product {ProductId} - creating it (out-of-order event)",
                    msg.ProductId);

                await _sellableItemService.CreateAsync(
                    sku: msg.Sku,
                    name: msg.Name ?? msg.Sku,
                    description: null,
                    basePrice: 0m,
                    itemType: "Product",
                    payload: new Dictionary<string, object?>
                    {
                        ["CatalogSku"] = msg.Sku,
                        ["Dimensions"] = msg.Dimensions,
                        ["Quantity"] = msg.Quantity,
                        ["StockQuantity"] = msg.InventoryId,
                        ["IsStocked"] = msg.IsStocked
                    },
                    productId: msg.ProductId,
                    cancellationToken: context.CancellationToken);

                _logger.LogInformation(
                    "Created SellableItem for Product {ProductId} during name update",
                    msg.ProductId);
                return;
            }

            // Update existing SellableItem name
            await _sellableItemService.UpdateNameAsync(
                sellableItemId: msg.ProductId,
                name: msg.Name ?? msg.Sku,
                cancellationToken: context.CancellationToken);

            _logger.LogInformation(
                "Successfully updated SellableItem name for Product {ProductId}",
                msg.ProductId);
        }
        catch (SellableItemNotFoundException ex)
        {
            // SellableItem disappeared between check and update - log and skip
            _logger.LogWarning(
                ex,
                "SellableItem not found for Product {ProductId} during update",
                msg.ProductId);
            // Do NOT rethrow - this is a valid scenario in distributed systems
        }
        catch (SellableItemStateException ex)
        {
            // SellableItem is discontinued - cannot update
            _logger.LogWarning(
                ex,
                "Cannot update discontinued SellableItem for Product {ProductId}",
                msg.ProductId);
            // Do NOT rethrow - this is a valid business rule
        }
        catch (SellableItemConflictException ex)
        {
            // SKU conflict during create-if-missing
            _logger.LogWarning(
                ex,
                "SellableItem conflict for Product {ProductId} during name update",
                msg.ProductId);
            // Do NOT rethrow - event may be replayed
        }
        catch (Exception ex)
        {
            // Transient failures should be retried by MassTransit
            _logger.LogError(
                ex,
                "Failed to update SellableItem name for Product {ProductId}",
                msg.ProductId);
            throw; // Let MassTransit retry
        }
    }
}
