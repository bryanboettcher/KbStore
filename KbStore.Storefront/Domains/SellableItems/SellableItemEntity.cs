namespace KbStore.Storefront.Domains.SellableItems;

using MassTransit;

public class SellableItemEntity : SagaStateMachineInstance, ISagaVersion
{
    public Guid CorrelationId { get; set; }
    public int Version { get; set; }
    public int CurrentState { get; set; }

    public Guid? ProductId { get; set; }
    public string SKU { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public string ItemType { get; set; } = "";
    public Dictionary<string, object?> Payload { get; set; } = new();

    public bool IsAvailable { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public static class SellableItemStates
{
    public const int Draft = 1;
    public const int Published = 2;
    public const int Hidden = 3;
    public const int Discontinued = 4;
}
