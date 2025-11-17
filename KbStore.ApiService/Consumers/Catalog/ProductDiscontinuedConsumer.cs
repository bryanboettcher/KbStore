namespace KbStore.ApiService.Consumers.Catalog;

using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Storefront.Abstractions.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;


public class ProductDiscontinuedConsumer : IConsumer<ProductDiscontinued>
{
    private readonly IRequestClient<DiscontinueSellableItemRequest> _requestClient;
    private readonly ILogger<ProductDiscontinuedConsumer> _logger;

    public ProductDiscontinuedConsumer(
        IRequestClient<DiscontinueSellableItemRequest> requestClient,
        ILogger<ProductDiscontinuedConsumer> logger)
    {
        _requestClient = requestClient;
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
            var response = await _requestClient.GetResponse<DiscontinueSellableItemResponse>(
                new
                {
                    SellableItemId = msg.ProductId,
                    Timestamp = DateTimeOffset.UtcNow
                },
                context.CancellationToken);

            _logger.LogInformation(
                "Successfully discontinued SellableItem for Product {ProductId}, SKU {SKU}",
                msg.ProductId,
                msg.Sku);
        }
        catch (RequestFaultException ex) when (ex.Message.Contains("not found") || ex.Message.Contains("NotFound"))
        {
            _logger.LogWarning(
                "SellableItem not found for Product {ProductId}, SKU {SKU} - may not be created yet",
                msg.ProductId,
                msg.Sku);
        }
        catch (RequestFaultException ex) when (ex.Message.Contains("already discontinued") || ex.Message.Contains("Discontinued"))
        {
            _logger.LogInformation(
                "SellableItem for Product {ProductId}, SKU {SKU} is already discontinued - event is idempotent",
                msg.ProductId,
                msg.Sku);
        }
        catch (RequestFaultException ex)
        {
            _logger.LogError(
                ex,
                "Failed to discontinue SellableItem for Product {ProductId}, SKU {SKU}: {Error}",
                msg.ProductId,
                msg.Sku,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to discontinue SellableItem for Product {ProductId}, SKU {SKU}",
                msg.ProductId,
                msg.Sku);
            throw;
        }
    }
}
