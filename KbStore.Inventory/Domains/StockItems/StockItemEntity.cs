namespace KbStore.Inventory.Domains.StockItems;

using Abstractions.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


public sealed class StockItemEntity : SagaStateMachineInstance, StockItemModel
{
    public Guid CorrelationId { get; set; }
    public uint RowVersion { get; set; }
    public int CurrentState { get; set; }

    public string PartNumber { get; set; } = "";
    public string Description { get; set; } = "";
    public int StockQuantity { get; set; }
}


public class InventorySagaMap : SagaClassMap<StockItemEntity>
{
    protected override void Configure(EntityTypeBuilder<StockItemEntity> entity, ModelBuilder model)
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