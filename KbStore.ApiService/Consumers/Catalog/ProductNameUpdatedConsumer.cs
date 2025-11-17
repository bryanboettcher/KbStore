namespace KbStore.ApiService.Consumers.Catalog;

using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Storefront.Abstractions.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;


public class ProductNameUpdatedConsumer : IConsumer<ProductNameUpdated>
{
    private readonly IRequestClient<UpdateSellableItemNameRequest> _requestClient;
    private readonly ILogger<ProductNameUpdatedConsumer> _logger;

    public ProductNameUpdatedConsumer(
        IRequestClient<UpdateSellableItemNameRequest> requestClient,
        ILogger<ProductNameUpdatedConsumer> logger)
    {
        _requestClient = requestClient;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductNameUpdated> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "Updating SellableItem name for Product {ProductId}, SKU {SKU}, new name: {Name}",
            msg.ProductId,
            msg.Sku,
            msg.Name);

        try
        {
            var response = await _requestClient.GetResponse<UpdateSellableItemResponse>(
                new
                {
                    SellableItemId = msg.ProductId,
                    Name = msg.Name ?? msg.Sku,
                    Timestamp = DateTimeOffset.UtcNow
                },
                context.CancellationToken);

            _logger.LogInformation(
                "Successfully updated SellableItem name for Product {ProductId}, SKU {SKU}",
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
        catch (RequestFaultException ex) when (ex.Message.Contains("discontinued") || ex.Message.Contains("Discontinued"))
        {
            _logger.LogWarning(
                "Cannot update discontinued SellableItem for Product {ProductId}, SKU {SKU}",
                msg.ProductId,
                msg.Sku);
        }
        catch (RequestFaultException ex)
        {
            _logger.LogError(
                ex,
                "Failed to update SellableItem name for Product {ProductId}, SKU {SKU}: {Error}",
                msg.ProductId,
                msg.Sku,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update SellableItem name for Product {ProductId}, SKU {SKU}",
                msg.ProductId,
                msg.Sku);
            throw;
        }
    }
}
