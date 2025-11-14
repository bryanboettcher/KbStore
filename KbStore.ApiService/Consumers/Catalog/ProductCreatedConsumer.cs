namespace KbStore.ApiService.Consumers.Catalog;

using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Storefront.Abstractions.Exceptions;
using KbStore.Storefront.Abstractions.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;


public class ProductCreatedConsumer : IConsumer<ProductCreated>
{
    private readonly ISellableItemCommandService _sellableItemService;
    private readonly ISellableItemQueryService _sellableItemQueryService;
    private readonly ILogger<ProductCreatedConsumer> _logger;

    public ProductCreatedConsumer(
        ISellableItemCommandService sellableItemService,
        ISellableItemQueryService sellableItemQueryService,
        ILogger<ProductCreatedConsumer> logger)
    {
        _sellableItemService = sellableItemService;
        _sellableItemQueryService = sellableItemQueryService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductCreated> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "Creating SellableItem for Product {ProductId}, SKU {SKU}",
            msg.ProductId,
            msg.Sku);

        try
        {
            // Idempotency check - verify if SellableItem already exists
            var existing = await _sellableItemQueryService.GetByIdAsync(
                msg.ProductId,
                context.CancellationToken);

            if (existing is not null)
            {
                _logger.LogInformation(
                    "SellableItem already exists for Product {ProductId}, SKU {SKU} - skipping creation",
                    msg.ProductId,
                    msg.Sku);
                return;
            }

            // Create SellableItem in Draft state with cross-domain correlation
            await _sellableItemService.CreateAsync(
                sku: msg.Sku,
                name: msg.Name ?? msg.Sku,  // Fallback to SKU if no name
                description: null,  // No description in Catalog domain
                basePrice: 0m,  // Default per architectural decision
                itemType: "Product",  // Discriminator for polymorphic SellableItem
                payload: new Dictionary<string, object?>
                {
                    ["CatalogSku"] = msg.Sku,
                    ["Dimensions"] = msg.Dimensions,
                    ["Quantity"] = msg.Quantity,
                    ["StockQuantity"] = msg.InventoryId,
                    ["IsStocked"] = msg.IsStocked
                },
                productId: msg.ProductId,  // Cross-domain correlation key
                cancellationToken: context.CancellationToken);

            _logger.LogInformation(
                "Successfully created SellableItem for Product {ProductId}",
                msg.ProductId);
        }
        catch (SellableItemConflictException ex)
        {
            // SKU conflict - likely race condition or event replay
            _logger.LogWarning(
                ex,
                "SellableItem conflict for Product {ProductId}, SKU {SKU} - event may be replayed",
                msg.ProductId,
                msg.Sku);
            // Do NOT rethrow - this is expected in distributed systems
        }
        catch (Exception ex)
        {
            // Transient failures should be retried by MassTransit
            _logger.LogError(
                ex,
                "Failed to create SellableItem for Product {ProductId}",
                msg.ProductId);
            throw; // Let MassTransit retry
        }
    }
}
