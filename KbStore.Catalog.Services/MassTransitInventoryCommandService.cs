namespace KbStore.Catalog.Services;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using Abstractions.Services;
using Extensions;
using MassTransit;


/// <summary>
/// Implementation of stock item command operations using MassTransit messaging.
/// </summary>
public class MassTransitInventoryCommandService : IInventoryCommandService
{
    private readonly IClientFactory _clientFactory;
    private readonly Func<DateTimeOffset> _now;

    public MassTransitInventoryCommandService(IClientFactory clientFactory, Func<DateTimeOffset> now)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _now = now ?? throw new ArgumentNullException(nameof(now));
    }

    public async Task<InventoryModel> CreateAsync(
        string partNumber,
        string description,
        int stockQuantity,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(partNumber))
            throw new InventoryValidationException("PartNumber must have a value");

        if (string.IsNullOrEmpty(description))
            throw new InventoryValidationException("Description must have a value");

        if (stockQuantity < 0)
            throw new InventoryValidationException("StockQuantity must be a positive value");

        var client = _clientFactory.CreateRequestClient<CreateInventoryRequest>();

        try
        {
            var response = await client.GetResponse<CreateInventoryResponse>(new
            {
                PartNumber = partNumber,
                Description = description,
                StockQuantity = stockQuantity,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToInventoryException();
        }
    }

    public async Task<InventoryModel> IncreaseQuantityAsync(Guid inventoryId, int quantity, CancellationToken cancellationToken = default)
    {
        if (inventoryId == Guid.Empty)
            throw new ArgumentException("InventoryId must be set", nameof(inventoryId));

        if (quantity <= 0)
            throw new InventoryValidationException("Quantity can only be increased by a positive whole number");

        var client = _clientFactory.CreateRequestClient<IncreaseInventoryQuantityRequest>();

        try
        {
            var response = await client.GetResponse<UpdateInventoryResponse>(new
            {
                InventoryId = inventoryId,
                Amount = quantity,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToInventoryException();
        }
    }

    public async Task<InventoryModel> DecreaseQuantityAsync(Guid inventoryId, int quantity, CancellationToken cancellationToken = default)
    {
        if (inventoryId == Guid.Empty)
            throw new ArgumentException("InventoryId must be set", nameof(inventoryId));

        if (quantity <= 0)
            throw new InventoryValidationException("Quantity can only be decreased by a positive whole number");

        var client = _clientFactory.CreateRequestClient<DecreaseInventoryQuantityRequest>();

        try
        {
            var response = await client.GetResponse<UpdateInventoryResponse>(new
            {
                InventoryId = inventoryId,
                Amount = quantity,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        } 
        catch (RequestFaultException e)
        {
            throw e.ToInventoryException();
        }
    }
    
    public async Task<InventoryModel> UpdateDescriptionAsync(
        Guid inventoryId,
        string description,
        CancellationToken cancellationToken = default)
    {
        if (inventoryId == Guid.Empty)
            throw new ArgumentException("InventoryId must be set", nameof(inventoryId));

        if (string.IsNullOrEmpty(description))
            throw new InventoryValidationException("Description cannot be empty");

        var client = _clientFactory.CreateRequestClient<UpdateInventoryDescriptionRequest>();

        try
        {
            var response = await client.GetResponse<UpdateInventoryResponse>(new
            {
                InventoryId = inventoryId,
                Description = description,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToInventoryException();
        }
    }

    public async Task<InventoryModel> HoldAsync(
        Guid inventoryId,
        CancellationToken cancellationToken = default)
    {
        if (inventoryId == Guid.Empty)
            throw new ArgumentException("InventoryId must be set", nameof(inventoryId));

        var client = _clientFactory.CreateRequestClient<HoldInventoryRequest>();

        try
        {
            var response = await client.GetResponse<HoldInventoryResponse>(new
            {
                InventoryId = inventoryId,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToInventoryException();
        }
    }

    public async Task<InventoryModel> ReleaseAsync(
        Guid inventoryId,
        CancellationToken cancellationToken = default)
    {
        if (inventoryId == Guid.Empty)
            throw new ArgumentException("InventoryId must be set", nameof(inventoryId));

        var client = _clientFactory.CreateRequestClient<ReleaseInventoryRequest>();

        try
        {
            var response = await client.GetResponse<ReleaseInventoryResponse>(new
            {
                InventoryId = inventoryId,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToInventoryException();
        }
    }

    public async Task<InventoryModel> DeleteAsync(
        Guid inventoryId,
        CancellationToken cancellationToken = default)
    {
        if (inventoryId == Guid.Empty)
            throw new ArgumentException("InventoryId must be set", nameof(inventoryId));

        var client = _clientFactory.CreateRequestClient<DeleteInventoryRequest>();

        try
        {
            var response = await client.GetResponse<DeleteInventoryResponse>(new
            {
                InventoryId = inventoryId,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToInventoryException();
        }
    }

    public async Task<InventoryModel> GetAsync(Guid inventoryId, CancellationToken cancellationToken = default)
    {
        if (inventoryId == Guid.Empty)
            throw new ArgumentException("InventoryId must be set", nameof(inventoryId));

        var client = _clientFactory.CreateRequestClient<InventoryStatusRequest>();

        try
        {
            var response = await client.GetResponse<InventoryStatusResponse>(new
            {
                InventoryId = inventoryId,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToInventoryException();
        }
    }
}