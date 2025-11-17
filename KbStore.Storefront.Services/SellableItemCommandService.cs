namespace KbStore.Storefront.Services;

using Extensions;
using KbStore.Storefront.Abstractions.Contracts;
using KbStore.Storefront.Abstractions.Exceptions;
using KbStore.Storefront.Abstractions.Interfaces;
using MassTransit;


public class SellableItemCommandService : ISellableItemCommandService
{
    private readonly IClientFactory _clientFactory;
    private readonly Func<DateTimeOffset> _now;

    public SellableItemCommandService(IClientFactory clientFactory, Func<DateTimeOffset> now)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _now = now ?? throw new ArgumentNullException(nameof(now));
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
        if (string.IsNullOrWhiteSpace(sku))
            throw new SellableItemValidationException("SKU must have a value");

        if (string.IsNullOrWhiteSpace(name))
            throw new SellableItemValidationException("Name must have a value");

        if (basePrice < 0)
            throw new SellableItemValidationException("Base price cannot be negative");

        if (string.IsNullOrWhiteSpace(itemType))
            throw new SellableItemValidationException("Item type must have a value");

        var client = _clientFactory.CreateRequestClient<CreateSellableItemRequest>();

        try
        {
            var response = await client.GetResponse<CreateSellableItemResponse>(new
            {
                ProductId = productId,
                Sku = sku,
                Name = name,
                Description = description,
                BasePrice = basePrice,
                ItemType = itemType,
                Payload = payload,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToSellableItemException();
        }
    }

    public async Task<SellableItemModel> UpdateNameAsync(
        Guid sellableItemId,
        string name,
        CancellationToken cancellationToken = default)
    {
        if (sellableItemId == Guid.Empty)
            throw new ArgumentException("SellableItemId must be set", nameof(sellableItemId));

        if (string.IsNullOrWhiteSpace(name))
            throw new SellableItemValidationException("Name must have a value");

        var client = _clientFactory.CreateRequestClient<UpdateSellableItemNameRequest>();

        try
        {
            var response = await client.GetResponse<UpdateSellableItemResponse>(new
            {
                SellableItemId = sellableItemId,
                Name = name,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToSellableItemException();
        }
    }

    public async Task<SellableItemModel> UpdateDescriptionAsync(
        Guid sellableItemId,
        string? description,
        CancellationToken cancellationToken = default)
    {
        if (sellableItemId == Guid.Empty)
            throw new ArgumentException("SellableItemId must be set", nameof(sellableItemId));

        var client = _clientFactory.CreateRequestClient<UpdateSellableItemDescriptionRequest>();

        try
        {
            var response = await client.GetResponse<UpdateSellableItemResponse>(new
            {
                SellableItemId = sellableItemId,
                Description = description,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToSellableItemException();
        }
    }

    public async Task<SellableItemModel> UpdatePriceAsync(
        Guid sellableItemId,
        decimal basePrice,
        CancellationToken cancellationToken = default)
    {
        if (sellableItemId == Guid.Empty)
            throw new ArgumentException("SellableItemId must be set", nameof(sellableItemId));

        if (basePrice < 0)
            throw new SellableItemValidationException("Base price cannot be negative");

        var client = _clientFactory.CreateRequestClient<UpdateSellableItemPriceRequest>();

        try
        {
            var response = await client.GetResponse<UpdateSellableItemResponse>(new
            {
                SellableItemId = sellableItemId,
                BasePrice = basePrice,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToSellableItemException();
        }
    }

    public async Task<SellableItemModel> UpdatePayloadAsync(
        Guid sellableItemId,
        IReadOnlyDictionary<string, object?> payload,
        CancellationToken cancellationToken = default)
    {
        if (sellableItemId == Guid.Empty)
            throw new ArgumentException("SellableItemId must be set", nameof(sellableItemId));

        var client = _clientFactory.CreateRequestClient<UpdateSellableItemPayloadRequest>();

        try
        {
            var response = await client.GetResponse<UpdateSellableItemResponse>(new
            {
                SellableItemId = sellableItemId,
                Payload = payload,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToSellableItemException();
        }
    }

    public async Task<SellableItemModel> PublishAsync(
        Guid sellableItemId,
        CancellationToken cancellationToken = default)
    {
        if (sellableItemId == Guid.Empty)
            throw new ArgumentException("SellableItemId must be set", nameof(sellableItemId));

        var client = _clientFactory.CreateRequestClient<PublishSellableItemRequest>();

        try
        {
            var response = await client.GetResponse<PublishSellableItemResponse>(new
            {
                SellableItemId = sellableItemId,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToSellableItemException();
        }
    }

    public async Task<SellableItemModel> HideAsync(
        Guid sellableItemId,
        CancellationToken cancellationToken = default)
    {
        if (sellableItemId == Guid.Empty)
            throw new ArgumentException("SellableItemId must be set", nameof(sellableItemId));

        var client = _clientFactory.CreateRequestClient<HideSellableItemRequest>();

        try
        {
            var response = await client.GetResponse<HideSellableItemResponse>(new
            {
                SellableItemId = sellableItemId,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToSellableItemException();
        }
    }

    public async Task<SellableItemModel> DiscontinueAsync(
        Guid sellableItemId,
        CancellationToken cancellationToken = default)
    {
        if (sellableItemId == Guid.Empty)
            throw new ArgumentException("SellableItemId must be set", nameof(sellableItemId));

        var client = _clientFactory.CreateRequestClient<DiscontinueSellableItemRequest>();

        try
        {
            var response = await client.GetResponse<DiscontinueSellableItemResponse>(new
            {
                SellableItemId = sellableItemId,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToSellableItemException();
        }
    }

    public async Task<SellableItemModel> ReinstateAsync(
        Guid sellableItemId,
        CancellationToken cancellationToken = default)
    {
        if (sellableItemId == Guid.Empty)
            throw new ArgumentException("SellableItemId must be set", nameof(sellableItemId));

        var client = _clientFactory.CreateRequestClient<ReinstateSellableItemRequest>();

        try
        {
            var response = await client.GetResponse<ReinstateSellableItemResponse>(new
            {
                SellableItemId = sellableItemId,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToSellableItemException();
        }
    }

    public async Task DeleteAsync(
        Guid sellableItemId,
        CancellationToken cancellationToken = default)
    {
        if (sellableItemId == Guid.Empty)
            throw new ArgumentException("SellableItemId must be set", nameof(sellableItemId));

        var client = _clientFactory.CreateRequestClient<DeleteSellableItemRequest>();

        try
        {
            await client.GetResponse<DeleteSellableItemResponse>(new
            {
                SellableItemId = sellableItemId,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFaultException e)
        {
            throw e.ToSellableItemException();
        }
    }
}
