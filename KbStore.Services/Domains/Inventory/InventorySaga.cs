namespace KbStore.Services.Domains.Inventory;

using KbStore.Contracts.Domains;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class InventorySaga : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public uint RowVersion { get; set; }
    public int CurrentState { get; set; }

    public string PartNumber { get; set; } = "";
    public string Description { get; set; } = "";
    public int StockQuantity { get; set; }
    public InventoryStatus InventoryStatus { get; set; }
}


public class InventorySagaMap : SagaClassMap<InventorySaga>
{
    protected override void Configure(EntityTypeBuilder<InventorySaga> entity, ModelBuilder model)
    {
        entity.Property(x => x.RowVersion)
            .HasColumnType("xid")
            .IsRowVersion();
    }
}