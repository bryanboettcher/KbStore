namespace KbStore.Catalog.Tests.Domains.Products;

using Abstractions.Contracts;
using Catalog.Domains.Products;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Shouldly;


[TestFixture]
public class Product_Disable : StateMachine_Tests<ProductStateMachine, ProductEntity>
{
    protected IRequestClient<DisableProductRequest> Client = null!;
    protected Response<DisableProductResponse> Response = null!;

    protected override void Arrange()
    {
        Client = CreateRequestClient<DisableProductRequest>();

        Harness.AddOrUpdateSagaInstance<ProductEntity>(ExistingId, entity =>
        {
            entity.CurrentState = ProductStates.Enabled;
            entity.Sku = "TEST_SKU_123";
            entity.Name = "Test Product";
            entity.IsStocked = true;
            entity.CreatedOn = Now;
            entity.UpdatedOn = Now;
        });
    }

    protected override async Task Act()
    {
        Response = await Client.GetResponse<DisableProductResponse>(new
        {
            ProductId = ExistingId,
            Timestamp = Later
        });
    }

    public class When_disabling_enabled_product : Product_Disable
    {
        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.IsEnabled.ShouldBeFalse();

            var sagaId = Response.Message.ProductId;
            SagaHarness.Sagas.Contains(sagaId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(ExistingId);
                o.CurrentState.ShouldBe(ProductStates.Disabled);
                o.UpdatedOn.ShouldBe(Later);
            });

            (await Harness.Published.Any<ProductDisabled>()).ShouldBeTrue();
        });
    }

    public class When_disabling_discontinued_product : Product_Disable
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddOrUpdateSagaInstance<ProductEntity>(ExistingId, entity =>
            {
                entity.CurrentState = ProductStates.Discontinued;
                entity.Sku = "TEST_SKU_123";
                entity.Name = "Test Product";
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
            });

            (await Harness.Published.Any<ProductDisabled>()).ShouldBeFalse();
        });
    }
}