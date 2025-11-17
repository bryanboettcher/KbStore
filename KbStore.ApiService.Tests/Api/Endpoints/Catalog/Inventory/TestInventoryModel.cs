namespace KbStore.ApiService.Tests.Api.Endpoints.Catalog.Inventory;

using KbStore.Catalog.Abstractions.Contracts;


public class TestInventoryModel : InventoryModel
{
    public Guid InventoryId { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public InventoryStatus Status { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset UpdatedOn { get; set; }

    public Guid CorrelationId => InventoryId;
}