namespace KbStore.ApiService.Tests.Api.Endpoints.Catalog;

using KbStore.Catalog.Abstractions.Contracts;


public class TestInventoryModel : InventoryModel
{
    public Guid InventoryId { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public InventoryStatus Status { get; set; }
}