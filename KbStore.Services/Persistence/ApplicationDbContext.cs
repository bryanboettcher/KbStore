namespace KbStore.Services.Persistence;

using Domains.Inventory;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : SagaDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> contextOptions)
        : base(contextOptions)
    {
    }

    public DbSet<InventorySaga> Inventory { get; set; }

    protected override IEnumerable<ISagaClassMap> Configurations => 
    [
        new InventorySagaMap()
    ];
}
