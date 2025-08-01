namespace KbStore.Inventory.Abstractions.Contracts;

public interface ProductModel
{
    Guid CorrelationId { get; }
    StockItemModel? StockItem { get; }
    string Name { get; }
    string Description { get; }
    int Quantity { get; }
    decimal Price { get; }
}