namespace KbStore.Services.Domains.Products;

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


public class ProductSaga : ISaga
{
    public Guid CorrelationId { get; set; }
    public uint RowVersion { get; set; }
    public int CurrentState { get; set; }

    public string Sku { get; set; } = "";
    public string? Name { get; set; }
    public string? Description { get; set; }

    public Guid? InventoryId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}


public class ProductSagaMap : SagaClassMap<ProductSaga>
{
    protected override void Configure(EntityTypeBuilder<ProductSaga> entity, ModelBuilder model)
    {
        entity.Property(x => x.RowVersion)
            .HasColumnType("xid")
            .IsRowVersion();

        entity.Property(x => x.Sku)
            .IsRequired();
    }
}