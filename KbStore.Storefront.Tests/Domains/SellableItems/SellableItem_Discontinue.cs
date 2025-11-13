namespace KbStore.Storefront.Tests.Domains.SellableItems;

using KbStore.Storefront.Abstractions.Contracts;
using KbStore.Storefront.Domains.SellableItems;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class SellableItem_Discontinue : StateMachine_Tests<SellableItemStateMachine, SellableItemEntity>
{
    protected IRequestClient<DiscontinueSellableItemRequest> Client = null!;
    protected Response<SellableItemResponse> Response = null!;

    protected override void Arrange()
    {

        Harness.AddOrUpdateSagaInstance<SellableItemEntity>(ExistingId, entity =>
        {
            entity.CurrentState = SellableItemStates.Published;
            entity.SKU = "PUB-SKU";
            entity.Name = "Published Item";
            entity.BasePrice = 50.00m;
            entity.ItemType = "simple-product";
            entity.Payload = new Dictionary<string, object?>();
            entity.IsAvailable = true;
            entity.CreatedAt = Now;
        });

        Client = Harness.Bus.CreateRequestClient<DiscontinueSellableItemRequest>();
    }

    protected override async Task Act()
    {
        Response = await Client.GetResponse<SellableItemResponse>(new
        {
            SellableItemId = ExistingId
        });
    }

    public class When_discontinuing_published_item : SellableItem_Discontinue
    {
        [Test]
        public async Task It_should_transition_to_discontinued() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Id.ShouldBe(ExistingId);
            Response.Message.State.ShouldBe("Discontinued");
            Response.Message.UpdatedAt.ShouldNotBeNull();

            var saga = SagaHarness.Sagas.Contains(ExistingId);
            saga.ShouldNotBeNull();
            saga.CurrentState.ShouldBe(SellableItemStates.Discontinued);
            saga.UpdatedAt.ShouldNotBeNull();

            (await Harness.Published.Any<SellableItemDiscontinued>()).ShouldBeTrue();
        });
    }

    public class When_discontinuing_hidden_item : SellableItem_Discontinue
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddOrUpdateSagaInstance<SellableItemEntity>(ExistingId, entity =>
            {
                entity.CurrentState = SellableItemStates.Hidden;
                entity.SKU = "HIDDEN-SKU";
                entity.Name = "Hidden Item";
                entity.BasePrice = 75.00m;
                entity.ItemType = "simple-product";
                entity.Payload = new Dictionary<string, object?>();
                entity.IsAvailable = true;
                entity.CreatedAt = Now;
                entity.UpdatedAt = Now;
            });
        }

        [Test]
        public async Task It_should_transition_to_discontinued() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Id.ShouldBe(ExistingId);
            Response.Message.State.ShouldBe("Discontinued");

            var saga = SagaHarness.Sagas.Contains(ExistingId);
            saga.ShouldNotBeNull();
            saga.CurrentState.ShouldBe(SellableItemStates.Discontinued);

            (await Harness.Published.Any<SellableItemDiscontinued>()).ShouldBeTrue();
        });
    }

    public class When_discontinuing_already_discontinued_item : SellableItem_Discontinue
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddOrUpdateSagaInstance<SellableItemEntity>(ExistingId, entity =>
            {
                entity.CurrentState = SellableItemStates.Discontinued;
                entity.SKU = "DISC-SKU";
                entity.Name = "Discontinued Item";
                entity.BasePrice = 100.00m;
                entity.ItemType = "simple-product";
                entity.Payload = new Dictionary<string, object?>();
                entity.IsAvailable = false;
                entity.CreatedAt = Now;
                entity.UpdatedAt = Now;
            });
        }

        [Test]
        public async Task It_should_throw_state_exception() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<RequestFaultException>();

            var saga = SagaHarness.Sagas.Contains(ExistingId);
            saga.ShouldNotBeNull();
            saga.CurrentState.ShouldBe(SellableItemStates.Discontinued);
        });
    }
}
