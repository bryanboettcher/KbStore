namespace KbStore.ApiService.Consumers.Catalog;

using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Storefront.Abstractions.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;


public class ProductCreatedConsumer : IConsumer<ProductCreated>
{
    private readonly IRequestClient<CreateSellableItemRequest> _requestClient;
    private readonly ILogger<ProductCreatedConsumer> _logger;

    public ProductCreatedConsumer(
        IRequestClient<CreateSellableItemRequest> requestClient,
        ILogger<ProductCreatedConsumer> logger)
    {
        _requestClient = requestClient;
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
            var response = await _requestClient.GetResponse<CreateSellableItemResponse>(
                new
                {
                    ProductId = (Guid?)msg.ProductId,
                    Sku = msg.Sku,
                    Name = msg.Name ?? msg.Sku,
                    Description = (string?)null,
                    BasePrice = 0m,
                    ItemType = "Product",
                    Payload = (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
                    {
                        ["CatalogSku"] = msg.Sku,
                        ["Dimensions"] = msg.Dimensions,
                        ["Quantity"] = msg.Quantity,
                        ["StockQuantity"] = msg.InventoryId,
                        ["IsStocked"] = msg.IsStocked
                    },
                    Timestamp = DateTimeOffset.UtcNow
                },
                context.CancellationToken);

            if (response.Message.SellableItemId != msg.ProductId)
            {
                _logger.LogError(
                    "CorrelationId mismatch! Product {ProductId} vs SellableItem {SellableItemId}",
                    msg.ProductId, response.Message.SellableItemId);
                throw new InvalidOperationException("Deterministic correlation failed");
            }

            _logger.LogInformation(
                "Successfully created SellableItem {SellableItemId} for Product {ProductId}, SKU {SKU}",
                response.Message.SellableItemId,
                msg.ProductId,
                msg.Sku);
        }
        catch (RequestFaultException ex) when (ex.Message.Contains("SKU") && ex.Message.Contains("already exists"))
        {
            _logger.LogInformation(
                "SellableItem already exists for SKU {SKU} - event is idempotent",
                msg.Sku);
        }
        catch (RequestFaultException ex)
        {
            _logger.LogError(
                ex,
                "Failed to create SellableItem for Product {ProductId}, SKU {SKU}: {Error}",
                msg.ProductId,
                msg.Sku,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create SellableItem for Product {ProductId}, SKU {SKU}",
                msg.ProductId,
                msg.Sku);
            throw;
        }
    }
}
