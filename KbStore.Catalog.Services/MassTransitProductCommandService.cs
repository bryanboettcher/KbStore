namespace KbStore.Catalog.Services;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using Abstractions.Services;
using Extensions;
using MassTransit;


/// <summary>
/// Implementation of product command operations using MassTransit messaging.
/// </summary>
public class MassTransitProductCommandService : IProductCommandService
{
    private readonly IClientFactory _clientFactory;
    private readonly Func<DateTimeOffset> _now;

    public MassTransitProductCommandService(IClientFactory clientFactory, Func<DateTimeOffset> now)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _now = now ?? throw new ArgumentNullException(nameof(now));
    }

    public async Task<ProductModel> CreateAsync(
        string sku,
        string? name,
        ProductDimensions? dimensions,
        Guid? inventoryId,
        int? stockThreshold,
        TimeSpan? leadTime,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ProductValidationException("SKU must have a value");

        if (stockThreshold < 0)
            throw ProductValidationException.InvalidStockThreshold(stockThreshold);

        if (leadTime?.TotalSeconds < 0)
            throw ProductValidationException.InvalidLeadTime(leadTime);

        var client = _clientFactory.CreateRequestClient<CreateProductRequest>();

        try
        {
            var response = await client.GetResponse<CreateProductResponse>(new
            {
                Sku = sku,
                Name = name,
                Dimensions = dimensions,
                InventoryId = inventoryId,
                StockThreshold = stockThreshold,
                LeadTime = leadTime,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToProductException();
        }
    }

    public async Task<ProductModel> UpdateNameAsync(
        Guid productId,
        string? name,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId must be set", nameof(productId));

        var client = _clientFactory.CreateRequestClient<UpdateProductNameRequest>();

        try
        {
            var response = await client.GetResponse<UpdateProductResponse>(new
            {
                ProductId = productId,
                Name = name,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToProductException();
        }
    }

    public async Task<ProductModel> UpdateDimensionsAsync(
        Guid productId,
        ProductDimensions? dimensions,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId must be set", nameof(productId));

        var client = _clientFactory.CreateRequestClient<UpdateProductDimensionsRequest>();

        try
        {
            var response = await client.GetResponse<UpdateProductResponse>(new
            {
                ProductId = productId,
                Dimensions = dimensions,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToProductException();
        }
    }

    public async Task<ProductModel> UpdateStockThresholdAsync(
        Guid productId,
        int? stockThreshold,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId must be set", nameof(productId));

        if (stockThreshold < 0)
            throw ProductValidationException.InvalidStockThreshold(stockThreshold);

        var client = _clientFactory.CreateRequestClient<UpdateProductStockThresholdRequest>();

        try
        {
            var response = await client.GetResponse<UpdateProductResponse>(new
            {
                ProductId = productId,
                StockThreshold = stockThreshold,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToProductException();
        }
    }

    public async Task<ProductModel> UpdateLeadTimeAsync(
        Guid productId,
        TimeSpan? leadTime,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId must be set", nameof(productId));

        if (leadTime?.TotalSeconds < 0)
            throw ProductValidationException.InvalidLeadTime(leadTime);

        var client = _clientFactory.CreateRequestClient<UpdateProductLeadTimeRequest>();

        try
        {
            var response = await client.GetResponse<UpdateProductResponse>(new
            {
                ProductId = productId,
                LeadTime = leadTime,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToProductException();
        }
    }

    public async Task<ProductModel> EnableAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId must be set", nameof(productId));

        var client = _clientFactory.CreateRequestClient<EnableProductRequest>();

        try
        {
            var response = await client.GetResponse<EnableProductResponse>(new
            {
                ProductId = productId,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToProductException();
        }
    }

    public async Task<ProductModel> DisableAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId must be set", nameof(productId));

        var client = _clientFactory.CreateRequestClient<DisableProductRequest>();

        try
        {
            var response = await client.GetResponse<DisableProductResponse>(new
            {
                ProductId = productId,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToProductException();
        }
    }

    public async Task<ProductModel> DeleteAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId must be set", nameof(productId));

        var client = _clientFactory.CreateRequestClient<DeleteProductRequest>();

        try
        {
            var response = await client.GetResponse<DeleteProductResponse>(new
            {
                ProductId = productId,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToProductException();
        }
    }

    public async Task<ProductModel> GetAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId must be set", nameof(productId));

        var client = _clientFactory.CreateRequestClient<ProductStatusRequest>();

        try
        {
            var response = await client.GetResponse<ProductStatusResponse>(new
            {
                ProductId = productId,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToProductException();
        }
    }
}