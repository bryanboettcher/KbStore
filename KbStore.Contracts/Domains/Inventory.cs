namespace KbStore.Contracts.Domains;

using MassTransit;

#region Base items
public enum InventoryStatus
{
    OnDemand,
    InStock,
    Backordered,
    Discontinued
}

[ExcludeFromConfigureEndpoints, ExcludeFromTopology, ExcludeFromImplementedTypes]
public interface InventoryModel
{
    Guid CorrelationId { get; }
    string PartNumber { get; }
    string Description { get; }
    int StockQuantity { get; }
    InventoryStatus InventoryStatus { get; }
}

[ExcludeFromConfigureEndpoints, ExcludeFromTopology, ExcludeFromImplementedTypes]
public interface BaseInventoryEvent : InventoryModel;

[ExcludeFromConfigureEndpoints, ExcludeFromTopology, ExcludeFromImplementedTypes]
public interface InventoryFailure : RequestFailureBase;

#endregion

#region Creating inventory
public interface CreateInventoryRequest : CorrelatedBy<Guid>
{
    string PartNumber { get; }
    string Description { get; }
    int StockQuantity { get; }
    InventoryStatus InventoryStatus { get; }
}
public interface CreateInventoryResponse : BaseInventoryEvent;
public interface InventoryCreated : BaseInventoryEvent;
#endregion

// most events are a variant of "thing was modified", so we use this as a common base
public interface InventoryUpdated : BaseInventoryEvent;


#region Updating inventory
public interface UpdateInventoryQuantityRequest : CorrelatedBy<Guid>
{
    int StockQuantity { get; }
}
public interface UpdateInventoryDescriptionRequest : CorrelatedBy<Guid>
{
    string Description { get; }
}
public interface UpdateInventoryResponse : BaseInventoryEvent;
public interface InventoryQuantityUpdated : InventoryUpdated;
public interface InventoryDescriptionUpdated : InventoryUpdated;
#endregion

#region Actions
public interface HoldInventoryRequest : CorrelatedBy<Guid>;
public interface HoldInventoryResponse : BaseInventoryEvent;
public interface InventoryHeld : InventoryUpdated;

public interface ReleaseInventoryRequest : CorrelatedBy<Guid>;
public interface ReleaseInventoryResponse : BaseInventoryEvent;
public interface InventoryReleased : InventoryUpdated;
#endregion

#region Deleting inventory
public interface DeleteInventoryRequest : CorrelatedBy<Guid>;
public interface DeleteInventoryResponse : BaseInventoryEvent;
public interface InventoryDiscontinued : InventoryUpdated;
public interface InventoryDeleted : BaseInventoryEvent;
#endregion

#region Validation
public interface InventoryStatusRequest : CorrelatedBy<Guid>;
public interface InventoryStatusResponse : BaseInventoryEvent;
#endregion