namespace KbStore.Catalog.Tests.Domains.Products;

using Abstractions.Contracts;
using Catalog.Domains.Products;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Shouldly;


[TestFixture]
public class Product_UpdateQuantity : StateMachine_Tests<ProductStateMachine, ProductEntity>
{
    protected IRequestClient<UpdateProductQuantityRequest> Client = null!;
    protected Response<UpdateProductResponse> Response = null!;

    protected override void Arrange()
    {
        Client = Harness.Bus.CreateRequestClient<UpdateProductQuantityRequest>();

        Harness.AddOrUpdateSagaInstance<ProductEntity>(ExistingId, entity =>
        {
            entity.CurrentState = ProductStates.Enabled;
            entity.Sku = "TEST_SKU_123";
            entity.Name = "Test Product";
            entity.Quantity = 1;
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
            Quantity = 5,
            Timestamp = Later
        });
    }

    public class When_updating_quantity_enabled_product : Product_UpdateQuantity
    {
        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Quantity.ShouldBe(5);

            var sagaId = Response.Message.ProductId;
            SagaHarness.Sagas.Contains(sagaId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(ExistingId);
                o.CurrentState.ShouldBe(ProductStates.Enabled);
                o.Quantity.ShouldBe(5);
                o.UpdatedOn.ShouldBe(Later);
            });

            (await Harness.Published.Any<ProductQuantityUpdated>()).ShouldBeTrue();
        });
    }

    public class When_updating_quantity_discontinued_product : Product_UpdateQuantity
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddOrUpdateSagaInstance<ProductEntity>(ExistingId, entity =>
            {
                entity.CurrentState = ProductStates.Discontinued;
                entity.Sku = "TEST_SKU_123";
                entity.Name = "Test Product";
                entity.Quantity = 1;
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
                o.Quantity.ShouldBe(1);  // Should remain unchanged
            });

            (await Harness.Published.Any<ProductQuantityUpdated>()).ShouldBeFalse();
        });
    }
}