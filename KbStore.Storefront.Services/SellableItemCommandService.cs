namespace KbStore.Storefront.Services;

using KbStore.Storefront.Abstractions.Contracts;
using KbStore.Storefront.Abstractions.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

public class SellableItemCommandService : ISellableItemCommandService
{
    private readonly IRequestClient<CreateSellableItemRequest> _createClient;
    private readonly IRequestClient<UpdateSellableItemNameRequest> _updateNameClient;
    private readonly IRequestClient<UpdateSellableItemDescriptionRequest> _updateDescriptionClient;
    private readonly IRequestClient<UpdateSellableItemPriceRequest> _updatePriceClient;
    private readonly IRequestClient<UpdateSellableItemPayloadRequest> _updatePayloadClient;
    private readonly IRequestClient<PublishSellableItemRequest> _publishClient;
    private readonly IRequestClient<HideSellableItemRequest> _hideClient;
    private readonly IRequestClient<DiscontinueSellableItemRequest> _discontinueClient;
    private readonly IRequestClient<ReinstateSellableItemRequest> _reinstateClient;
    private readonly IRequestClient<DeleteSellableItemRequest> _deleteClient;
    private readonly ILogger<SellableItemCommandService> _logger;

    public SellableItemCommandService(
        IRequestClient<CreateSellableItemRequest> createClient,
        IRequestClient<UpdateSellableItemNameRequest> updateNameClient,
        IRequestClient<UpdateSellableItemDescriptionRequest> updateDescriptionClient,
        IRequestClient<UpdateSellableItemPriceRequest> updatePriceClient,
        IRequestClient<UpdateSellableItemPayloadRequest> updatePayloadClient,
        IRequestClient<PublishSellableItemRequest> publishClient,
        IRequestClient<HideSellableItemRequest> hideClient,
        IRequestClient<DiscontinueSellableItemRequest> discontinueClient,
        IRequestClient<ReinstateSellableItemRequest> reinstateClient,
        IRequestClient<DeleteSellableItemRequest> deleteClient,
        ILogger<SellableItemCommandService> logger)
    {
        _createClient = createClient;
        _updateNameClient = updateNameClient;
        _updateDescriptionClient = updateDescriptionClient;
        _updatePriceClient = updatePriceClient;
        _updatePayloadClient = updatePayloadClient;
        _publishClient = publishClient;
        _hideClient = hideClient;
        _discontinueClient = discontinueClient;
        _reinstateClient = reinstateClient;
        _deleteClient = deleteClient;
        _logger = logger;
    }

    public async Task<SellableItemModel> CreateAsync(
        string sku,
        string name,
        string? description,
        decimal basePrice,
        string itemType,
        IReadOnlyDictionary<string, object?> payload,
        Guid? productId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating sellable item with SKU {SKU}", sku);

        var response = await _createClient.GetResponse<CreateSellableItemResponse>(
            new
            {
                ProductId = productId,
                Sku = sku,
                Name = name,
                Description = description,
                BasePrice = basePrice,
                ItemType = itemType,
                Payload = payload,
                Timestamp = DateTimeOffset.UtcNow
            },
            cancellationToken);

        _logger.LogInformation("Created sellable item {SellableItemId} with SKU {SKU}",
            response.Message.SellableItemId, sku);

        return response.Message;
    }

    public async Task<SellableItemModel> UpdateNameAsync(
        Guid sellableItemId,
        string name,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating name for sellable item {SellableItemId}", sellableItemId);

        var response = await _updateNameClient.GetResponse<UpdateSellableItemResponse>(
            new
            {
                SellableItemId = sellableItemId,
                Name = name,
                Timestamp = DateTimeOffset.UtcNow
            },
            cancellationToken);

        _logger.LogInformation("Updated name for sellable item {SellableItemId}", sellableItemId);

        return response.Message;
    }

    public async Task<SellableItemModel> UpdateDescriptionAsync(
        Guid sellableItemId,
        string? description,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating description for sellable item {SellableItemId}", sellableItemId);

        var response = await _updateDescriptionClient.GetResponse<UpdateSellableItemResponse>(
            new
            {
                SellableItemId = sellableItemId,
                Description = description,
                Timestamp = DateTimeOffset.UtcNow
            },
            cancellationToken);

        _logger.LogInformation("Updated description for sellable item {SellableItemId}", sellableItemId);

        return response.Message;
    }

    public async Task<SellableItemModel> UpdatePriceAsync(
        Guid sellableItemId,
        decimal basePrice,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating price for sellable item {SellableItemId} to {BasePrice}",
            sellableItemId, basePrice);

        var response = await _updatePriceClient.GetResponse<UpdateSellableItemResponse>(
            new
            {
                SellableItemId = sellableItemId,
                BasePrice = basePrice,
                Timestamp = DateTimeOffset.UtcNow
            },
            cancellationToken);

        _logger.LogInformation("Updated price for sellable item {SellableItemId}", sellableItemId);

        return response.Message;
    }

    public async Task<SellableItemModel> UpdatePayloadAsync(
        Guid sellableItemId,
        IReadOnlyDictionary<string, object?> payload,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating payload for sellable item {SellableItemId}", sellableItemId);

        var response = await _updatePayloadClient.GetResponse<UpdateSellableItemResponse>(
            new
            {
                SellableItemId = sellableItemId,
                Payload = payload,
                Timestamp = DateTimeOffset.UtcNow
            },
            cancellationToken);

        _logger.LogInformation("Updated payload for sellable item {SellableItemId}", sellableItemId);

        return response.Message;
    }

    public async Task<SellableItemModel> PublishAsync(
        Guid sellableItemId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing sellable item {SellableItemId}", sellableItemId);

        var response = await _publishClient.GetResponse<PublishSellableItemResponse>(
            new
            {
                SellableItemId = sellableItemId,
                Timestamp = DateTimeOffset.UtcNow
            },
            cancellationToken);

        _logger.LogInformation("Published sellable item {SellableItemId}", sellableItemId);

        return response.Message;
    }

    public async Task<SellableItemModel> HideAsync(
        Guid sellableItemId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Hiding sellable item {SellableItemId}", sellableItemId);

        var response = await _hideClient.GetResponse<HideSellableItemResponse>(
            new
            {
                SellableItemId = sellableItemId,
                Timestamp = DateTimeOffset.UtcNow
            },
            cancellationToken);

        _logger.LogInformation("Hidden sellable item {SellableItemId}", sellableItemId);

        return response.Message;
    }

    public async Task<SellableItemModel> DiscontinueAsync(
        Guid sellableItemId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Discontinuing sellable item {SellableItemId}", sellableItemId);

        var response = await _discontinueClient.GetResponse<DiscontinueSellableItemResponse>(
            new
            {
                SellableItemId = sellableItemId,
                Timestamp = DateTimeOffset.UtcNow
            },
            cancellationToken);

        _logger.LogInformation("Discontinued sellable item {SellableItemId}", sellableItemId);

        return response.Message;
    }

    public async Task<SellableItemModel> ReinstateAsync(
        Guid sellableItemId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reinstating sellable item {SellableItemId}", sellableItemId);

        var response = await _reinstateClient.GetResponse<ReinstateSellableItemResponse>(
            new
            {
                SellableItemId = sellableItemId,
                Timestamp = DateTimeOffset.UtcNow
            },
            cancellationToken);

        _logger.LogInformation("Reinstated sellable item {SellableItemId}", sellableItemId);

        return response.Message;
    }

    public async Task DeleteAsync(
        Guid sellableItemId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting sellable item {SellableItemId}", sellableItemId);

        await _deleteClient.GetResponse<DeleteSellableItemResponse>(
            new
            {
                SellableItemId = sellableItemId,
                Timestamp = DateTimeOffset.UtcNow
            },
            cancellationToken);

        _logger.LogInformation("Deleted sellable item {SellableItemId}", sellableItemId);
    }
}
