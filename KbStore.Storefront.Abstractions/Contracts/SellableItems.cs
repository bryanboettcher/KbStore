namespace KbStore.Storefront.Abstractions.Contracts;

using KbStore.Abstractions;
using MassTransit;

#region Base SellableItem Interfaces

[ExcludeFromTopology]
public interface SellableItemCommand : CorrelatedBy<Guid>
{
    Guid SellableItemId { get; }
    DateTimeOffset Timestamp { get; }

    Guid CorrelationId => SellableItemId;
}

[ExcludeFromTopology]
public interface SellableItemModel : CorrelatedBy<Guid>
{
    Guid SellableItemId { get; }
    Guid? ProductId { get; }
    string Sku { get; }
    string Name { get; }
    string? Description { get; }
    decimal BasePrice { get; }
    string ItemType { get; }
    IReadOnlyDictionary<string, object?> Payload { get; }
    bool IsAvailable { get; }
    int Version { get; }
    DateTimeOffset CreatedOn { get; }
    DateTimeOffset UpdatedOn { get; }

    Guid CorrelationId => SellableItemId;
}

[ExcludeFromTopology]
public interface BaseSellableItemEvent : SellableItemModel;

public interface SellableItemFailure : RequestFailureBase;

#endregion

#region Creating SellableItems

public interface CreateSellableItemRequest
{
    Guid? ProductId { get; }
    string Sku { get; }
    string Name { get; }
    string? Description { get; }
    decimal BasePrice { get; }
    string ItemType { get; }
    IReadOnlyDictionary<string, object?> Payload { get; }
    DateTimeOffset Timestamp { get; }
}

public interface CreateSellableItemResponse : SellableItemModel;
public interface SellableItemCreated : BaseSellableItemEvent;

#endregion

#region Updating SellableItem Information

[ExcludeFromTopology]
public interface SellableItemUpdated : BaseSellableItemEvent;

public interface UpdateSellableItemNameRequest : SellableItemCommand
{
    string Name { get; }
}

public interface UpdateSellableItemDescriptionRequest : SellableItemCommand
{
    string? Description { get; }
}

public interface UpdateSellableItemPriceRequest : SellableItemCommand
{
    decimal BasePrice { get; }
}

public interface UpdateSellableItemPayloadRequest : SellableItemCommand
{
    IReadOnlyDictionary<string, object?> Payload { get; }
}

public interface UpdateSellableItemResponse : SellableItemModel;

public interface SellableItemNameUpdated : SellableItemUpdated;
public interface SellableItemDescriptionUpdated : SellableItemUpdated;
public interface SellableItemPayloadUpdated : SellableItemUpdated;

[ExcludeFromTopology]
public interface SellableItemAvailabilityChanged : SellableItemUpdated;

public interface SellableItemPriceChanged : SellableItemAvailabilityChanged;

#endregion

#region SellableItem Visibility Management

public interface PublishSellableItemRequest : SellableItemCommand;
public interface HideSellableItemRequest : SellableItemCommand;

public interface PublishSellableItemResponse : SellableItemModel;
public interface HideSellableItemResponse : SellableItemModel;

public interface SellableItemPublished : SellableItemUpdated;
public interface SellableItemHidden : SellableItemUpdated;

#endregion

#region SellableItem Lifecycle Management

public interface DiscontinueSellableItemRequest : SellableItemCommand;
public interface ReinstateSellableItemRequest : SellableItemCommand;

public interface DiscontinueSellableItemResponse : SellableItemModel;
public interface ReinstateSellableItemResponse : SellableItemModel;

public interface SellableItemDiscontinued : SellableItemUpdated;
public interface SellableItemReinstated : SellableItemUpdated;

#endregion

#region Deleting SellableItems

public interface DeleteSellableItemRequest : SellableItemCommand;
public interface DeleteSellableItemResponse : SellableItemModel;
public interface SellableItemDeleted : BaseSellableItemEvent;

#endregion

#region Querying SellableItems

public interface SellableItemStatusRequest : SellableItemCommand;
public interface SellableItemStatusResponse : SellableItemModel;

#endregion

public static class SellableItemStates
{
    public const int Draft = 1;
    public const int Published = 2;
    public const int Hidden = 3;
    public const int Discontinued = 4;
}
