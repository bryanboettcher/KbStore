namespace KbStore.Catalog.Abstractions.Contracts;

using KbStore.Abstractions;

#region Base items

public enum InventoryStatus
{
    Invalid,
    Available,
    Held,
    Backordered,
    Discontinued
}

public interface InventoryCommand
{
    Guid InventoryId { get; }
    DateTimeOffset Timestamp { get; }
}

public interface InventoryModel
{
    Guid InventoryId { get; }
    string PartNumber { get; }
    string Description { get; }
    int StockQuantity { get; }
    InventoryStatus Status { get; }

    DateTimeOffset CreatedOn { get; }
    DateTimeOffset UpdatedOn { get; }
}

public interface BaseInventoryEvent : InventoryModel;

public interface InventoryFailure : RequestFailureBase;

#endregion

#region Creating stock items
public interface CreateInventoryRequest : InventoryCommand
{
    string PartNumber { get; }
    string Description { get; }
    int StockQuantity { get; }
}
public interface CreateInventoryResponse : InventoryModel;
public interface InventoryCreated : BaseInventoryEvent;
#endregion

// most events are a variant of "thing was modified", so we use this as a common base
public interface InventoryUpdated : BaseInventoryEvent;


#region Updating stock items
public interface IncreaseInventoryQuantityRequest : InventoryCommand
{
    int Amount { get; }
}

public interface DecreaseInventoryQuantityRequest : InventoryCommand
{
    int Amount { get; }
}

public interface UpdateInventoryDescriptionRequest : InventoryCommand
{
    string Description { get; }
}

public interface UpdateInventoryResponse : InventoryModel;

public interface InventoryQuantityChanged : InventoryUpdated;
public interface InventoryQuantityIncreased : InventoryQuantityChanged;
public interface InventoryQuantityDecreased : InventoryQuantityChanged;
public interface InventoryDescriptionUpdated : InventoryUpdated;
#endregion

#region Actions
public interface HoldInventoryRequest : InventoryCommand;
public interface HoldInventoryResponse : InventoryModel;
public interface InventoryHeld : InventoryUpdated;

public interface ReleaseInventoryRequest : InventoryCommand;
public interface ReleaseInventoryResponse : InventoryModel;
public interface InventoryReleased : InventoryUpdated;
#endregion

#region Deleting stock items
public interface DeleteInventoryRequest : InventoryCommand;
public interface DeleteInventoryResponse : InventoryModel;
public interface InventoryDiscontinued : InventoryUpdated;
public interface InventoryDeleted : BaseInventoryEvent;
#endregion

#region Validation

public interface InventoryStatusRequest
{
    Guid InventoryId { get; }
};
public interface InventoryStatusResponse : InventoryModel;
#endregion

public static class InventoryStates
{
    public const int Initial = 1;
    public const int Finalized = 2;
    public const int Available = 3;
    public const int OnHold = 4;
    public const int Backordered = 5;
    public const int Discontinued = 6;
}