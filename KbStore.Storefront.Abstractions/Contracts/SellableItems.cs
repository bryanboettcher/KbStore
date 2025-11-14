namespace KbStore.Storefront.Abstractions.Contracts;

#region Request Contracts

public record CreateSellableItemRequest
{
    public Guid? ProductId { get; init; }
    public string SKU { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public decimal BasePrice { get; init; }
    public string ItemType { get; init; } = "";
    public Dictionary<string, object?> Payload { get; init; } = new();
}

public record UpdateSellableItemNameRequest
{
    public Guid SellableItemId { get; init; }
    public string Name { get; init; } = "";
}

public record UpdateSellableItemPriceRequest
{
    public Guid SellableItemId { get; init; }
    public decimal BasePrice { get; init; }
}

public record PublishSellableItemRequest
{
    public Guid SellableItemId { get; init; }
}

public record HideSellableItemRequest
{
    public Guid SellableItemId { get; init; }
}

public record DiscontinueSellableItemRequest
{
    public Guid SellableItemId { get; init; }
}

public record DeleteSellableItemRequest
{
    public Guid SellableItemId { get; init; }
}

public record ReinstateSellableItemRequest
{
    public Guid SellableItemId { get; init; }
}

#endregion

#region Response Contract

public record SellableItemResponse(
    Guid Id,
    Guid? ProductId,
    string SKU,
    string Name,
    string? Description,
    decimal BasePrice,
    string ItemType,
    Dictionary<string, object?> Payload,
    string State,
    bool IsAvailable,
    int Version,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

#endregion

#region Event Contracts

public record SellableItemCreated(
    Guid SellableItemId,
    string SKU,
    string Name,
    decimal BasePrice,
    string ItemType,
    DateTime CreatedAt
);

public record SellableItemPublished(
    Guid SellableItemId,
    DateTime PublishedAt
);

public record SellableItemHidden(
    Guid SellableItemId,
    DateTime HiddenAt
);

public record SellableItemDiscontinued(
    Guid SellableItemId,
    DateTime DiscontinuedAt
);

public record SellableItemDeleted(
    Guid SellableItemId,
    DateTime DeletedAt
);

public record SellableItemPriceChanged(
    Guid SellableItemId,
    decimal OldPrice,
    decimal NewPrice,
    DateTime ChangedAt
);

public record SellableItemAvailabilityChanged(
    Guid SellableItemId,
    bool IsAvailable,
    DateTime ChangedAt
);

#endregion
