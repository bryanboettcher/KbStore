namespace KbStore.Catalog.Abstractions.Contracts;

public interface ProductModel
{
    Guid CorrelationId { get; }
    InventoryModel? Inventory { get; }
    string Name { get; }
    string Description { get; }
    int Quantity { get; }
    decimal Price { get; }
}