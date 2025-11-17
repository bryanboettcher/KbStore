namespace KbStore.Storefront.Domains.SellableItems;

using MassTransit;

public class SellableItemEntity : SagaStateMachineInstance, ISagaVersion
{
    public Guid CorrelationId { get; set; }
    public int Version { get; set; }
    public int CurrentState { get; set; }

    public Guid? ProductId { get; set; }
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public string ItemType { get; set; } = "";
    public Dictionary<string, object?> Payload { get; set; } = new();

    public bool IsAvailable { get; set; }

    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset UpdatedOn { get; set; }
}
