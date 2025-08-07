namespace KbStore.Catalog.Services;

using System.Diagnostics;
using Abstractions.Contracts;
using Abstractions.Exceptions;
using Abstractions.Services;
using KbStore.Abstractions;
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
        
        Response response = await client.GetResponse<CreateInventoryResponse, InventoryFailure>(new
        {
            PartNumber = partNumber,
            Description = description,
            StockQuantity = stockQuantity,
            Timestamp = _now()
        }, cancellationToken).ConfigureAwait(false);

        return response switch
        {
            (_, CreateInventoryResponse success) => success,
            (_, InventoryFailure failure) => throw MapFailureToException(failure),
            _ => throw new InvalidOperationException("Unexpected response type from backend")
        };
    }

    public async Task<InventoryModel> IncreaseQuantityAsync(Guid inventoryId, int quantity, CancellationToken cancellationToken = default)
    {
        if (inventoryId == Guid.Empty)
            throw new ArgumentException("InventoryId must be set", nameof(inventoryId));

        if (quantity <= 0)
            throw new InventoryValidationException("Quantity can only be increased by a positive whole number");

        var client = _bus.CreateRequestClient<IncreaseInventoryQuantityRequest>();

        Response response = await client.GetResponse<UpdateInventoryResponse, InventoryFailure>(new
        {
            InventoryId = inventoryId,
            Amount = quantity,
            Timestamp = _now()
        }, cancellationToken).ConfigureAwait(false);

        return response switch
        {
            (_, UpdateInventoryResponse success) => success,
            (_, InventoryFailure failure) => throw MapFailureToException(failure, inventoryId),
            _ => throw new InvalidOperationException("Unexpected response type from backend")
        };
    }

    public async Task<InventoryModel> DecreaseQuantityAsync(Guid inventoryId, int quantity, CancellationToken cancellationToken = default)
    {
        var client = _bus.CreateRequestClient<IncreaseInventoryQuantityRequest>();

        Response response = await client.GetResponse<UpdateInventoryResponse, InventoryFailure>(new
        {
            InventoryId = inventoryId,
            Amount = quantity,
            Timestamp = _now()
        }, cancellationToken).ConfigureAwait(false);

        return response switch
        {
            (_, UpdateInventoryResponse success) => success,
            (_, InventoryFailure failure) => throw MapFailureToException(failure, inventoryId),
            _ => throw new InvalidOperationException("Unexpected response type from backend")
        };
    }
    
    public async Task<InventoryModel> UpdateDescriptionAsync(
        Guid inventoryId,
        string description,
        CancellationToken cancellationToken = default)
    {
        var client = _bus.CreateRequestClient<UpdateInventoryDescriptionRequest>();

        Response response = await client.GetResponse<UpdateInventoryResponse, InventoryFailure>(new
        {
            InventoryId = inventoryId,
            Description = description,
            Timestamp = _now()
        }, cancellationToken).ConfigureAwait(false);

        return response switch
        {
            (_, UpdateInventoryResponse success) => success,
            (_, InventoryFailure failure) => throw MapFailureToException(failure, inventoryId),
            _ => throw new InvalidOperationException("Unexpected response type from backend")
        };
    }

    public async Task<InventoryModel> HoldAsync(
        Guid inventoryId,
        CancellationToken cancellationToken = default)
    {
        var client = _bus.CreateRequestClient<HoldInventoryRequest>();

        Response response = await client.GetResponse<HoldInventoryResponse, InventoryFailure>(new
        {
            InventoryId = inventoryId,
            Timestamp = _now()
        }, cancellationToken).ConfigureAwait(false);

        return response switch
        {
            (_, HoldInventoryResponse success) => success,
            (_, InventoryFailure failure) => throw MapFailureToException(failure, inventoryId),
            _ => throw new InvalidOperationException("Unexpected response type from backend")
        };
    }

    public async Task<InventoryModel> ReleaseAsync(
        Guid inventoryId,
        CancellationToken cancellationToken = default)
    {
        var client = _bus.CreateRequestClient<ReleaseInventoryRequest>();

        Response response = await client.GetResponse<ReleaseInventoryResponse, InventoryFailure>(new
        {
            InventoryId = inventoryId,
            Timestamp = _now()
        }, cancellationToken).ConfigureAwait(false);

        return response switch
        {
            (_, ReleaseInventoryResponse success) => success,
            (_, InventoryFailure failure) => throw MapFailureToException(failure, inventoryId),
            _ => throw new InvalidOperationException("Unexpected response type from backend")
        };
    }

    public async Task<InventoryModel> DeleteAsync(
        Guid inventoryId,
        CancellationToken cancellationToken = default)
    {
        var client = _bus.CreateRequestClient<DeleteInventoryRequest>();

        Response response = await client.GetResponse<DeleteInventoryResponse, InventoryFailure>(new
        {
            InventoryId = inventoryId,
            Timestamp = _now()
        }, cancellationToken).ConfigureAwait(false);

        return response switch
        {
            (_, DeleteInventoryResponse success) => success,
            (_, InventoryFailure failure) => throw MapFailureToException(failure, inventoryId),
            _ => throw new InvalidOperationException("Unexpected response type from backend")
        };
    }

    public async Task<InventoryModel> GetAsync(Guid inventoryId, CancellationToken cancellationToken = default)
    {
        var client = _bus.CreateRequestClient<InventoryStatusRequest>();

        Response response = await client.GetResponse<InventoryStatusResponse, InventoryFailure>(new
        {
            InventoryId = inventoryId,
            Timestamp = _now()
        }, cancellationToken).ConfigureAwait(false);

        return response switch
        {
            (_, InventoryStatusResponse success) => success,
            (_, InventoryFailure failure) => throw MapFailureToException(failure, inventoryId),
            _ => throw new InvalidOperationException("Unexpected response type from backend")
        };
    }

    /// <summary>
    /// Maps InventoryFailure responses to appropriate domain exceptions.
    /// </summary>
    private static Exception MapFailureToException(InventoryFailure failure, Guid? inventoryId = null)
    {
        var message = failure.Message ?? "Unknown error";

        return failure.FailureType switch
        {
            FailureTypes.Missing => new InventoryNotFoundException(inventoryId ?? Guid.Empty),
            FailureTypes.Conflict => new InventoryConflictException(message),
            FailureTypes.Validation => new InventoryValidationException(message),
            FailureTypes.InvalidState => new InventoryStateException(inventoryId ?? Guid.Empty, "Unknown", "Unknown"),
            FailureTypes.Forbidden => new InventoryStateException(inventoryId ?? Guid.Empty, "Unknown", "Unknown"),
            FailureTypes.InternalError => new InventoryException($"Internal error: {message}"),
            _ => new InventoryException($"Unmapped failure type '{failure.FailureType}': {message}")
        };
    }
}