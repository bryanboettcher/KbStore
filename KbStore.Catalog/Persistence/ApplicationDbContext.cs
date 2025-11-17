namespace KbStore.Catalog.Persistence;

using Domains.Inventory;
using Domains.Products;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;


public class ApplicationDbContext : SagaDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> contextOptions)
        : base(contextOptions)
    {
    }

    public DbSet<InventoryEntity> Inventory { get; set; }
    public DbSet<ProductEntity> Products { get; set; }

    protected override IEnumerable<ISagaClassMap> Configurations => 
    [
        new InventorySagaMap(),
        new ProductSagaMap()
    ];
}
