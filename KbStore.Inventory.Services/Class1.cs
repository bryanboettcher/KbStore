namespace KbStore.Inventory.Services;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using Abstractions.Services;
using KbStore.Abstractions;
using MassTransit;


/// <summary>
/// Implementation of stock item command operations using MassTransit messaging.
/// </summary>
public class StockItemCommandService : IStockItemCommandService
{
    private readonly IBus _bus;

    public StockItemCommandService(IBus bus)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
    }

    public async Task<StockItemModel> CreateAsync(
        string partNumber,
        string description,
        int stockQuantity,
        CancellationToken cancellationToken = default)
    {
        var client = _bus.CreateRequestClient<CreateStockItemRequest>();

        Response response = await client.GetResponse<CreateStockItemResponse, StockItemFailure>(new
        {
            PartNumber = partNumber,
            Description = description,
            StockQuantity = stockQuantity
        }, cancellationToken);

        return response switch
        {
            (_, CreateStockItemResponse success) => success,
            (_, StockItemFailure failure) => throw MapFailureToException(failure),
            _ => throw new InvalidOperationException("Unexpected response type from backend")
        };
    }

    public async Task<StockItemModel> UpdateQuantityAsync(
        Guid itemId,
        int newQuantity,
        CancellationToken cancellationToken = default)
    {
        var client = _bus.CreateRequestClient<UpdateStockItemQuantityRequest>();

        Response response = await client.GetResponse<UpdateStockItemResponse, StockItemFailure>(new
        {
            CorrelationId = itemId,
            StockQuantity = newQuantity
        }, cancellationToken);

        return response switch
        {
            (_, UpdateStockItemResponse success) => success,
            (_, StockItemFailure failure) => throw MapFailureToException(failure, itemId),
            _ => throw new InvalidOperationException("Unexpected response type from backend")
        };
    }

    public async Task<StockItemModel> UpdateDescriptionAsync(
        Guid itemId,
        string description,
        CancellationToken cancellationToken = default)
    {
        var client = _bus.CreateRequestClient<UpdateStockItemDescriptionRequest>();

        Response response = await client.GetResponse<UpdateStockItemResponse, StockItemFailure>(new
        {
            CorrelationId = itemId,
            Description = description
        }, cancellationToken);

        return response switch
        {
            (_, UpdateStockItemResponse success) => success,
            (_, StockItemFailure failure) => throw MapFailureToException(failure, itemId),
            _ => throw new InvalidOperationException("Unexpected response type from backend")
        };
    }

    public async Task<StockItemModel> HoldAsync(
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var client = _bus.CreateRequestClient<HoldStockItemRequest>();

        Response response = await client.GetResponse<HoldStockItemResponse, StockItemFailure>(new
        {
            CorrelationId = itemId
        }, cancellationToken);

        return response switch
        {
            (_, HoldStockItemResponse success) => success,
            (_, StockItemFailure failure) => throw MapFailureToException(failure, itemId),
            _ => throw new InvalidOperationException("Unexpected response type from backend")
        };
    }

    public async Task<StockItemModel> ReleaseAsync(
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var client = _bus.CreateRequestClient<ReleaseStockItemRequest>();

        Response response = await client.GetResponse<ReleaseStockItemResponse, StockItemFailure>(new
        {
            CorrelationId = itemId
        }, cancellationToken);

        return response switch
        {
            (_, ReleaseStockItemResponse success) => success,
            (_, StockItemFailure failure) => throw MapFailureToException(failure, itemId),
            _ => throw new InvalidOperationException("Unexpected response type from backend")
        };
    }

    public async Task<StockItemModel> DeleteAsync(
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var client = _bus.CreateRequestClient<DeleteStockItemRequest>();

        Response response = await client.GetResponse<DeleteStockItemResponse, StockItemFailure>(new
        {
            CorrelationId = itemId
        }, cancellationToken).ConfigureAwait(false);

        return response switch
        {
            (_, DeleteStockItemResponse success) => success,
            (_, StockItemFailure failure) => throw MapFailureToException(failure, itemId),
            _ => throw new InvalidOperationException("Unexpected response type from backend")
        };
    }

    /// <summary>
    /// Maps StockItemFailure responses to appropriate domain exceptions.
    /// </summary>
    private static Exception MapFailureToException(StockItemFailure failure, Guid? itemId = null)
    {
        var message = failure.Message ?? "Unknown error";

        return failure.FailureType switch
        {
            FailureType.Missing => new StockItemNotFoundException(itemId ?? Guid.Empty),
            FailureType.Conflict => new StockItemConflictException(message),
            FailureType.Validation => new StockItemValidationException(message),
            FailureType.InvalidState => new StockItemStateException(itemId ?? Guid.Empty, "Unknown", "Unknown"),
            FailureType.Forbidden => new StockItemStateException(itemId ?? Guid.Empty, "Unknown", "Unknown"),
            FailureType.InternalError => new StockItemException($"Internal error: {message}"),
            _ => new StockItemException($"Unmapped failure type '{failure.FailureType}': {message}")
        };
    }
}

/// <summary>
/// Generic StockItemException for unmapped failure types.
/// </summary>
public class StockItemException : Exception
{
    public StockItemException(string message) : base(message) { }
    public StockItemException(string message, Exception innerException) : base(message, innerException) { }
}