namespace KbStore.Inventory.Persistence;

using Domains.Products;
using Domains.StockItems;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;


public class ApplicationDbContext : SagaDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> contextOptions)
        : base(contextOptions)
    {
    }

    public DbSet<StockItemEntity> Inventory { get; set; }
    public DbSet<ProductEntity> Products { get; set; }

    protected override IEnumerable<ISagaClassMap> Configurations => 
    [
        new InventorySagaMap(),
        new ProductSagaMap()
    ];
}
