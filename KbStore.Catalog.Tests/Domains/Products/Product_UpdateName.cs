namespace KbStore.Catalog.Tests.Domains.Products;

using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Domains.Products;
using KbStore.Catalog.Tests.Domains;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Shouldly;


[TestFixture]
public class Product_UpdateName : StateMachine_Tests<ProductStateMachine, ProductEntity>
{
    protected IRequestClient<UpdateProductNameRequest> Client = null!;
    protected Response<UpdateProductResponse> Response = null!;

    protected override void Arrange()
    {
        Client = CreateRequestClient<UpdateProductNameRequest>();

        Harness.AddOrUpdateSagaInstance<ProductEntity>(ExistingId, entity =>
        {
            entity.CurrentState = ProductStates.Enabled;
            entity.Sku = "TEST_SKU_123";
            entity.Name = "Original Name";
            entity.IsStocked = true;
            entity.CreatedOn = Now;
            entity.UpdatedOn = Now;
        });
    }

    protected override async Task Act()
    {
        Response = await Client.GetResponse<UpdateProductResponse>(new
        {
            ProductId = ExistingId,
            Name = "Updated Product Name",
            Timestamp = Later
        });
    }

    public class When_updating_name_enabled_product : Product_UpdateName
    {
        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Name.ShouldBe("Updated Product Name");

            var sagaId = Response.Message.ProductId;
            SagaHarness.Sagas.Contains(sagaId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(ExistingId);
                o.CurrentState.ShouldBe(ProductStates.Enabled);
                o.Name.ShouldBe("Updated Product Name");
                o.UpdatedOn.ShouldBe(Later);
            });

            (await Harness.Published.Any<ProductNameUpdated>()).ShouldBeTrue();
        });
    }

    public class When_updating_name_discontinued_product : Product_UpdateName
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddOrUpdateSagaInstance<ProductEntity>(ExistingId, entity =>
            {
                entity.CurrentState = ProductStates.Discontinued;
                entity.Sku = "TEST_SKU_123";
                entity.Name = "Original Name";
                entity.IsStocked = true;
                entity.CreatedOn = Now;
                entity.UpdatedOn = Now;
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<RequestFaultException>();

            SagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CurrentState.ShouldBe(ProductStates.Discontinued);
                o.Name.ShouldBe("Original Name");
            });

            (await Harness.Published.Any<ProductNameUpdated>()).ShouldBeFalse();
        });
    }
}