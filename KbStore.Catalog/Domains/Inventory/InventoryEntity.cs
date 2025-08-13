namespace KbStore.Catalog.Domains.Inventory;

using Abstractions.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


public sealed class InventoryEntity : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public uint RowVersion { get; set; }
    public int CurrentState { get; set; }

    public string PartNumber { get; set; } = "";
    public string Description { get; set; } = "";
    public int StockQuantity { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset UpdatedOn { get; set; }
    public InventoryStatus Status => CurrentState switch
    {
        InventoryStates.Available => InventoryStatus.Available,    // Available state
        InventoryStates.OnHold => InventoryStatus.Held,         // OnHold state  
        InventoryStates.Backordered => InventoryStatus.Backordered,  // Backordered state
        InventoryStates.Discontinued => InventoryStatus.Discontinued, // Discontinued state
        _ => InventoryStatus.Invalid
    };
}

public class InventorySagaMap : SagaClassMap<InventoryEntity>
{
    protected override void Configure(EntityTypeBuilder<InventoryEntity> entity, ModelBuilder model)
    {
        entity.Property(x => x.RowVersion)
            .HasColumnType("xid")
            .IsRowVersion();

        entity.Property(x => x.PartNumber)
            .HasMaxLength(200)
            .IsRequired();

        entity.HasIndex(x => x.PartNumber)
            .IsUnique();

        entity.Property(x => x.Description)
            .HasMaxLength(2000);
    }
}