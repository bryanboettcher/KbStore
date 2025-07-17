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
public interface BaseInventoryCommand
{
    Guid CorrelationId { get; set; }
    string PartNumber { get; }
}

[ExcludeFromConfigureEndpoints, ExcludeFromTopology, ExcludeFromImplementedTypes]
public interface BaseInventoryEvent : InventoryModel;

[ExcludeFromConfigureEndpoints, ExcludeFromTopology, ExcludeFromImplementedTypes]
public interface InventoryFailure : BaseInventoryCommand;

public interface InventoryMissing : InventoryFailure;
public interface InventoryUpdatedEvent : BaseInventoryEvent;

#endregion

#region Creating inventory
public interface CreateInventoryRequest : BaseInventoryCommand
{
    string Description { get; }
    int StockQuantity { get; }
    InventoryStatus InventoryStatus { get; }
}
public interface CreateInventoryResponse : BaseInventoryEvent;
public interface CreateInventoryFailure : InventoryFailure;
public interface InventoryCreatedEvent : BaseInventoryEvent;
#endregion

#region Updating inventory
public interface UpdateInventoryQuantityRequest : BaseInventoryCommand
{
    int StockQuantity { get; }
}
public interface UpdateInventoryQuantityResponse : BaseInventoryEvent;
public interface InventoryQuantityUpdatedEvent : InventoryUpdatedEvent;
#endregion

#region Actions
public interface HoldInventoryRequest : BaseInventoryCommand;
public interface HoldInventoryResponse : BaseInventoryEvent;
public interface InventoryHeldEvent : InventoryUpdatedEvent;

public interface ReleaseInventoryRequest : BaseInventoryCommand;
public interface ReleaseInventoryResponse : BaseInventoryEvent;
public interface InventoryReleasedEvent : InventoryUpdatedEvent;
#endregion

#region Deleting inventory
public interface DeleteInventoryRequest : BaseInventoryCommand;
public interface DeleteInventoryResponse : BaseInventoryEvent;
public interface InventoryDiscontinuedEvent : InventoryUpdatedEvent;
public interface InventoryDeletedEvent : BaseInventoryEvent;
#endregion

#region Validation
public interface InventoryStatusRequest : BaseInventoryCommand;
public interface InventoryStatusResponse : BaseInventoryEvent;
#endregion