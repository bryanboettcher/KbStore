namespace KbStore.ApiService.Tests.Api.Endpoints.Storefront.SellableItem;

using KbStore.Storefront.Abstractions.Contracts;


public class TestSellableItemModel : SellableItemModel
{
    public Guid SellableItemId { get; set; }
    public Guid? ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public IReadOnlyDictionary<string, object?> Payload { get; set; } = new Dictionary<string, object?>();
    public bool IsAvailable { get; set; }
    public int Version { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset UpdatedOn { get; set; }

    public Guid CorrelationId => SellableItemId;
}
