namespace KbStore.Catalog.Abstractions.Contracts;

using KbStore.Abstractions;
using MassTransit;

#region Value Types

public struct ProductDimensions
{
    public decimal? Width { get; set; }
    public decimal? Length { get; set; }
    public decimal? Height { get; set; }
    public decimal? Weight { get; set; }

    public decimal? CalculateVolume() => Width * Length * Height;

    public decimal? CalculateShippingVolume() => CalculateVolume() * 1.2m; // Add 20% padding

    public bool IsValidDimensions() =>
        Width.GetValueOrDefault() > 0 &&
        Length.GetValueOrDefault() > 0 &&
        Height.GetValueOrDefault() > 0;

    public bool IsOversized() => CalculateVolume() > 1000m; // Example threshold
}

#endregion

#region Base Product Items

public interface ProductCommand : CorrelatedBy<Guid>
{
    Guid ProductId { get; }
    DateTimeOffset Timestamp { get; }

    Guid CorrelationId => ProductId;
}

public interface ProductModel : CorrelatedBy<Guid>
{
    Guid ProductId { get; }
    string Sku { get; }
    string? Name { get; }
    ProductDimensions? Dimensions { get; }
    int Quantity { get; }
    Guid? InventoryId { get; }
    int? StockThreshold { get; }
    TimeSpan? LeadTime { get; }
    bool IsStocked { get; }
    bool IsEnabled { get; }
    bool IsAvailable { get; }
    DateTimeOffset CreatedOn { get; }
    DateTimeOffset UpdatedOn { get; }

    Guid CorrelationId => ProductId;
}

public interface BaseProductEvent : ProductModel;

public interface ProductFailure : RequestFailureBase;

#endregion

#region Creating Products

public interface CreateProductRequest
{
    string Sku { get; }
    string? Name { get; }
    ProductDimensions? Dimensions { get; }
    int Quantity { get; }
    Guid? InventoryId { get; }
    int? StockThreshold { get; }
    TimeSpan? LeadTime { get; }
    DateTimeOffset Timestamp { get; }
}

public interface CreateProductResponse : ProductModel;
public interface ProductCreated : BaseProductEvent;

#endregion

// Most events are variants of "product was modified"
public interface ProductUpdated : BaseProductEvent;

#region Updating Product Information

public interface UpdateProductNameRequest : ProductCommand
{
    string? Name { get; }
}

public interface UpdateProductDimensionsRequest : ProductCommand
{
    ProductDimensions? Dimensions { get; }
}

public interface UpdateProductStockThresholdRequest : ProductCommand
{
    int? StockThreshold { get; }
}

public interface UpdateProductLeadTimeRequest : ProductCommand
{
    TimeSpan? LeadTime { get; }
}
public interface UpdateProductQuantityRequest : ProductCommand
{
    int Quantity { get; }
}

public interface ProductQuantityUpdated : ProductUpdated;
public interface UpdateProductResponse : ProductModel;
public interface ProductNameUpdated : ProductUpdated;
public interface ProductDimensionsUpdated : ProductUpdated;
public interface ProductLeadTimeUpdated : ProductUpdated;

// multiple types of events will change availability, so we use an
// intermediate event type to reduce messaging noise
public interface ProductAvailabilityChanged : ProductUpdated;
public interface ProductStockThresholdUpdated : ProductAvailabilityChanged;

#endregion

#region Product Availability Management

public interface EnableProductRequest : ProductCommand;
public interface DisableProductRequest : ProductCommand;

public interface EnableProductResponse : ProductModel;
public interface DisableProductResponse : ProductModel;

public interface ProductEnabled : ProductUpdated;
public interface ProductDisabled : ProductUpdated;


#endregion

#region Deleting Products

public interface DeleteProductRequest : ProductCommand;
public interface DeleteProductResponse : ProductModel;
public interface ProductDiscontinued : ProductUpdated;
public interface ProductDeleted : BaseProductEvent;

#endregion

#region Querying Products

public interface ProductStatusRequest : ProductCommand;
public interface ProductStatusResponse : ProductModel;

#endregion

public static class ProductStates
{
    public const int Initial = 1;
    public const int Finalized = 2;
    public const int Enabled = 3;
    public const int Disabled = 4;
    public const int Discontinued = 5;
}