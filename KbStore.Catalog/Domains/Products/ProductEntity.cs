namespace KbStore.Catalog.Domains.Products;

using Inventory;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


public class ProductEntity : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public uint RowVersion { get; set; }
    public int CurrentState { get; set; }
    public Guid? InventoryStatusId { get; set; }


    public string Sku { get; set; } = "";
    public string? Name { get; set; }
    public string? Description { get; set; }

    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Height { get; set; }
    public decimal? Width { get; set; }
    public decimal? Depth { get; set; }

    public Guid? InventoryId { get; set; }
    public int? StockThreshold { get; set; }
    public TimeSpan? LeadTime { get; set; }
    public bool IsStocked { get; set; }
    public bool IsEnabled => CurrentState switch
    {
        3 => true,  // Enabled
        4 => false, // Disabled
        5 => false, // Discontinued
        _ => false
    };
    public bool IsAvailable => IsStocked && IsEnabled;

    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset UpdatedOn { get; set; }

    public InventoryEntity? Inventory { get; set; }

}

public class ProductSagaMap : SagaClassMap<ProductEntity>
{
    protected override void Configure(EntityTypeBuilder<ProductEntity> entity, ModelBuilder model)
    {
        entity.Property(x => x.RowVersion)
            .HasColumnType("xid")
            .IsRowVersion();

        entity.Property(x => x.Sku)
            .HasMaxLength(32)
            .IsRequired();

        entity.HasIndex(x => x.Sku)
            .IsUnique();

        entity.Property(x => x.Name)
            .HasMaxLength(200);

        entity.Property(x => x.Description)
            .HasMaxLength(2000);

        entity.Property(x => x.Price)
            .HasPrecision(18, 2);

        entity.Property(x => x.Quantity)
            .HasDefaultValue(0);

        entity.HasOne(p => p.Inventory)
            .WithMany()
            .HasForeignKey("InventoryId")
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}