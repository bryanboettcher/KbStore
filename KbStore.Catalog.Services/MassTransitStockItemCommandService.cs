namespace KbStore.Catalog.Services;

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

    public MassTransitInventoryCommandService(IBus bus)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
    }

    public async Task<InventoryModel> CreateAsync(
        string partNumber,
        string description,
        int stockQuantity,
        CancellationToken cancellationToken = default)
    {
        var client = _bus.CreateRequestClient<CreateInventoryRequest>();

        Response response = await client.GetResponse<CreateInventoryResponse, InventoryFailure>(new
        {
            PartNumber = partNumber,
            Description = description,
            StockQuantity = stockQuantity
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
        var client = _bus.CreateRequestClient<IncreaseInventoryQuantityRequest>();

        Response response = await client.GetResponse<UpdateInventoryResponse, InventoryFailure>(new
        {
            CorrelationId = inventoryId,
            StockQuantity = quantity
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
            CorrelationId = inventoryId,
            StockQuantity = quantity
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
            CorrelationId = inventoryId,
            Description = description
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
            CorrelationId = inventoryId
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
            CorrelationId = inventoryId
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
            CorrelationId = inventoryId
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
            InventoryId = inventoryId
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
            FailureType.Missing => new InventoryNotFoundException(inventoryId ?? Guid.Empty),
            FailureType.Conflict => new InventoryConflictException(message),
            FailureType.Validation => new InventoryValidationException(message),
            FailureType.InvalidState => new InventoryStateException(inventoryId ?? Guid.Empty, "Unknown", "Unknown"),
            FailureType.Forbidden => new InventoryStateException(inventoryId ?? Guid.Empty, "Unknown", "Unknown"),
            FailureType.InternalError => new InventoryException($"Internal error: {message}"),
            _ => new InventoryException($"Unmapped failure type '{failure.FailureType}': {message}")
        };
    }
}