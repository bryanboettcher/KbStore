namespace KbStore.Storefront.Services;

using KbStore.Storefront.Abstractions.Contracts;
using KbStore.Storefront.Abstractions.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

/// <summary>
/// Command service for SellableItem state machine operations.
/// Wraps MassTransit request/response pattern to provide simple awaitable methods.
/// </summary>
public class SellableItemCommandService : ISellableItemCommandService
{
    private readonly IRequestClient<CreateSellableItemRequest> _createClient;
    private readonly IRequestClient<UpdateSellableItemNameRequest> _updateNameClient;
    private readonly IRequestClient<UpdateSellableItemPriceRequest> _updatePriceClient;
    private readonly IRequestClient<PublishSellableItemRequest> _publishClient;
    private readonly IRequestClient<HideSellableItemRequest> _hideClient;
    private readonly IRequestClient<DiscontinueSellableItemRequest> _discontinueClient;
    private readonly IRequestClient<ReinstateSellableItemRequest> _reinstateClient;
    private readonly IRequestClient<DeleteSellableItemRequest> _deleteClient;
    private readonly ILogger<SellableItemCommandService> _logger;

    public SellableItemCommandService(
        IRequestClient<CreateSellableItemRequest> createClient,
        IRequestClient<UpdateSellableItemNameRequest> updateNameClient,
        IRequestClient<UpdateSellableItemPriceRequest> updatePriceClient,
        IRequestClient<PublishSellableItemRequest> publishClient,
        IRequestClient<HideSellableItemRequest> hideClient,
        IRequestClient<DiscontinueSellableItemRequest> discontinueClient,
        IRequestClient<ReinstateSellableItemRequest> reinstateClient,
        IRequestClient<DeleteSellableItemRequest> deleteClient,
        ILogger<SellableItemCommandService> logger)
    {
        _createClient = createClient;
        _updateNameClient = updateNameClient;
        _updatePriceClient = updatePriceClient;
        _publishClient = publishClient;
        _hideClient = hideClient;
        _discontinueClient = discontinueClient;
        _reinstateClient = reinstateClient;
        _deleteClient = deleteClient;
        _logger = logger;
    }

    public async Task<SellableItemResponse> CreateAsync(
        string sku,
        string name,
        string? description,
        decimal basePrice,
        string itemType,
        Dictionary<string, object?> payload,
        Guid? productId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating sellable item with SKU {SKU}", sku);

        var request = new CreateSellableItemRequest
        {
            ProductId = productId,
            SKU = sku,
            Name = name,
            Description = description,
            BasePrice = basePrice,
            ItemType = itemType,
            Payload = payload
        };

        var response = await _createClient.GetResponse<SellableItemResponse>(request, cancellationToken);

        _logger.LogInformation("Created sellable item {SellableItemId} with SKU {SKU}",
            response.Message.Id, sku);

        return response.Message;
    }

    public async Task<SellableItemResponse> UpdateNameAsync(
        Guid sellableItemId,
        string name,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating name for sellable item {SellableItemId}", sellableItemId);

        var request = new UpdateSellableItemNameRequest
        {
            SellableItemId = sellableItemId,
            Name = name
        };

        var response = await _updateNameClient.GetResponse<SellableItemResponse>(request, cancellationToken);

        _logger.LogInformation("Updated name for sellable item {SellableItemId}", sellableItemId);

        return response.Message;
    }

    public async Task<SellableItemResponse> UpdatePriceAsync(
        Guid sellableItemId,
        decimal basePrice,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating price for sellable item {SellableItemId} to {BasePrice}",
            sellableItemId, basePrice);

        var request = new UpdateSellableItemPriceRequest
        {
            SellableItemId = sellableItemId,
            BasePrice = basePrice
        };

        var response = await _updatePriceClient.GetResponse<SellableItemResponse>(request, cancellationToken);

        _logger.LogInformation("Updated price for sellable item {SellableItemId}", sellableItemId);

        return response.Message;
    }

    public async Task<SellableItemResponse> PublishAsync(
        Guid sellableItemId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing sellable item {SellableItemId}", sellableItemId);

        var request = new PublishSellableItemRequest { SellableItemId = sellableItemId };

        var response = await _publishClient.GetResponse<SellableItemResponse>(request, cancellationToken);

        _logger.LogInformation("Published sellable item {SellableItemId}", sellableItemId);

        return response.Message;
    }

    public async Task<SellableItemResponse> HideAsync(
        Guid sellableItemId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Hiding sellable item {SellableItemId}", sellableItemId);

        var request = new HideSellableItemRequest { SellableItemId = sellableItemId };

        var response = await _hideClient.GetResponse<SellableItemResponse>(request, cancellationToken);

        _logger.LogInformation("Hidden sellable item {SellableItemId}", sellableItemId);

        return response.Message;
    }

    public async Task<SellableItemResponse> DiscontinueAsync(
        Guid sellableItemId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Discontinuing sellable item {SellableItemId}", sellableItemId);

        var request = new DiscontinueSellableItemRequest { SellableItemId = sellableItemId };

        var response = await _discontinueClient.GetResponse<SellableItemResponse>(request, cancellationToken);

        _logger.LogInformation("Discontinued sellable item {SellableItemId}", sellableItemId);

        return response.Message;
    }

    public async Task<SellableItemResponse> ReinstateAsync(
        Guid sellableItemId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reinstating sellable item {SellableItemId}", sellableItemId);

        var request = new ReinstateSellableItemRequest { SellableItemId = sellableItemId };

        var response = await _reinstateClient.GetResponse<SellableItemResponse>(request, cancellationToken);

        _logger.LogInformation("Reinstated sellable item {SellableItemId}", sellableItemId);

        return response.Message;
    }

    public async Task DeleteAsync(
        Guid sellableItemId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting sellable item {SellableItemId}", sellableItemId);

        var request = new DeleteSellableItemRequest { SellableItemId = sellableItemId };

        await _deleteClient.GetResponse<SellableItemResponse>(request, cancellationToken);

        _logger.LogInformation("Deleted sellable item {SellableItemId}", sellableItemId);
    }
}
