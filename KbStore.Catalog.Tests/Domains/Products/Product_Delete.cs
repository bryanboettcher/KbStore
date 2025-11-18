namespace KbStore.Catalog.Tests.Domains.Products;

using Abstractions.Contracts;
using Catalog.Domains.Products;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Shouldly;


[TestFixture]
public class Product_Delete : StateMachine_Tests<ProductStateMachine, ProductEntity>
{
    protected IRequestClient<DeleteProductRequest> Client = null!;
    protected Response<DeleteProductResponse> Response = null!;

    protected override void Arrange()
    {
        Client = CreateRequestClient<DeleteProductRequest>();

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
        Response = await Client.GetResponse<DeleteProductResponse>(new
        {
            ProductId = ExistingId,
            Timestamp = Later
        });
    }

    public class When_deleting_enabled_product : Product_Delete
    {
        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();

            var sagaId = Response.Message.ProductId;
            SagaHarness.Sagas.Contains(sagaId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(ExistingId);
                o.CurrentState.ShouldBe(ProductStates.Discontinued);
                o.UpdatedOn.ShouldBe(Later);
            });

            (await Harness.Published.Any<ProductDiscontinued>()).ShouldBeTrue();
        });
    }

    public class When_deleting_discontinued_product : Product_Delete
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
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();

            var sagaId = Response.Message.ProductId;
            (await SagaHarness.NotExists(sagaId)).ShouldNotBe(ExistingId);

            (await Harness.Published.Any<ProductDeleted>()).ShouldBeTrue();
        });
    }
}