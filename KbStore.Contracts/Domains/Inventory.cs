namespace KbStore.Contracts.Domains;

using MassTransit;

#region Base items

[ExcludeFromConfigureEndpoints, ExcludeFromTopology, ExcludeFromImplementedTypes]
public interface StockItemModel
{
    Guid CorrelationId { get; }
    string PartNumber { get; }
    string Description { get; }
    int StockQuantity { get; }
}

[ExcludeFromConfigureEndpoints, ExcludeFromTopology, ExcludeFromImplementedTypes]
public interface BaseStockItemEvent : StockItemModel;

[ExcludeFromConfigureEndpoints, ExcludeFromTopology, ExcludeFromImplementedTypes]
public interface StockItemFailure : RequestFailureBase;

#endregion

#region Creating stock items
public interface CreateStockItemRequest : CorrelatedBy<Guid>
{
    string PartNumber { get; }
    string Description { get; }
    int StockQuantity { get; }
}
public interface CreateStockItemResponse : BaseStockItemEvent;
public interface StockItemCreated : BaseStockItemEvent;
#endregion

// most events are a variant of "thing was modified", so we use this as a common base
public interface StockItemUpdated : BaseStockItemEvent;


#region Updating stock items
public interface UpdateStockItemQuantityRequest : CorrelatedBy<Guid>
{
    int StockQuantity { get; }
}
public interface UpdateStockItemDescriptionRequest : CorrelatedBy<Guid>
{
    string Description { get; }
}
public interface UpdateStockItemResponse : BaseStockItemEvent;
public interface StockItemQuantityUpdated : StockItemUpdated;
public interface StockItemDescriptionUpdated : StockItemUpdated;
#endregion

#region Actions
public interface HoldStockItemRequest : CorrelatedBy<Guid>;
public interface HoldStockItemResponse : BaseStockItemEvent;
public interface StockItemHeld : StockItemUpdated;

public interface ReleaseStockItemRequest : CorrelatedBy<Guid>;
public interface ReleaseStockItemResponse : BaseStockItemEvent;
public interface StockItemReleased : StockItemUpdated;
#endregion

#region Deleting stock items
public interface DeleteStockItemRequest : CorrelatedBy<Guid>;
public interface DeleteStockItemResponse : BaseStockItemEvent;
public interface StockItemDiscontinued : StockItemUpdated;
public interface StockItemDeleted : BaseStockItemEvent;
#endregion

#region Validation
public interface StockItemStatusRequest : CorrelatedBy<Guid>;
public interface StockItemStatusResponse : BaseStockItemEvent;
#endregion