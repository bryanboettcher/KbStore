namespace KbStore.Catalog.Tests.Domains.Products;

using Abstractions.Contracts;
using Catalog.Domains.Products;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Shouldly;


[TestFixture]
public class Product_Enable : StateMachine_Tests<ProductStateMachine, ProductEntity>
{
    protected IRequestClient<EnableProductRequest> Client = null!;
    protected Response<EnableProductResponse> Response = null!;

    protected override void Arrange()
    {
        Client = Harness.Bus.CreateRequestClient<EnableProductRequest>();

        Harness.AddOrUpdateSagaInstance<ProductEntity>(ExistingId, entity =>
        {
            entity.CurrentState = ProductStates.Disabled;
            entity.Sku = "TEST_SKU_123";
            entity.Name = "Test Product";
            entity.IsStocked = true;
            entity.CreatedOn = Now;
            entity.UpdatedOn = Now;
        });
    }

    protected override async Task Act()
    {
        Response = await Client.GetResponse<EnableProductResponse>(new
        {
            ProductId = ExistingId,
            Timestamp = Later
        });
    }

    public class When_enabling_disabled_product : Product_Enable
    {
        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.IsEnabled.ShouldBeTrue();

            var sagaId = Response.Message.ProductId;
            SagaHarness.Sagas.Contains(sagaId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(ExistingId);
                o.CurrentState.ShouldBe(ProductStates.Enabled);
                o.UpdatedOn.ShouldBe(Later);
            });

            (await Harness.Published.Any<ProductEnabled>()).ShouldBeTrue();
        });
    }

    public class When_enabling_already_enabled_product : Product_Enable
    {
        protected override void Arrange()
        {
            base.Arrange();

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

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<RequestFaultException>();

            SagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CurrentState.ShouldBe(ProductStates.Enabled);
            });

            (await Harness.Published.Any<ProductEnabled>()).ShouldBeFalse();
        });
    }
}