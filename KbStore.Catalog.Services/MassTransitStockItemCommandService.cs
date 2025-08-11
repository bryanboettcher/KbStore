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
    private readonly IBus _bus;
    private readonly Func<DateTimeOffset> _now;

    public MassTransitInventoryCommandService(IBus bus, Func<DateTimeOffset> now)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
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

        var client = _bus.CreateRequestClient<CreateInventoryRequest>();

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

        var client = _bus.CreateRequestClient<IncreaseInventoryQuantityRequest>();

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

        var client = _bus.CreateRequestClient<DecreaseInventoryQuantityRequest>();

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

        var client = _bus.CreateRequestClient<UpdateInventoryDescriptionRequest>();

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

        var client = _bus.CreateRequestClient<HoldInventoryRequest>();

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

        var client = _bus.CreateRequestClient<ReleaseInventoryRequest>();

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

        var client = _bus.CreateRequestClient<DeleteInventoryRequest>();

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

        var client = _bus.CreateRequestClient<InventoryStatusRequest>();

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