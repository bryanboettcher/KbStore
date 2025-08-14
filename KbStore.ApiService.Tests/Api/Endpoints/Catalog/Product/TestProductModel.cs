namespace KbStore.ApiService.Tests.Api.Endpoints.Catalog.Product;

using KbStore.Catalog.Abstractions.Contracts;


public class TestProductModel : ProductModel
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; }
    public string? Name { get; set; }
    public ProductDimensions? Dimensions { get; set; }
    public Guid? InventoryId { get; set; }
    public int? StockThreshold { get; set; }
    public TimeSpan? LeadTime { get; set; }
    public bool IsStocked { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsAvailable { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset UpdatedOn { get; set; }
}