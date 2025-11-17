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
    protected Response<DiscontinueSellableItemResponse> Response = null!;

    protected override void Arrange()
    {
        Harness.AddOrUpdateSagaInstance<SellableItemEntity>(ExistingId, entity =>
        {
            entity.CurrentState = SellableItemStates.Published;
            entity.Sku = "PUB-SKU";
            entity.Name = "Published Item";
            entity.BasePrice = 50.00m;
            entity.ItemType = "simple-product";
            entity.Payload = new Dictionary<string, object?>();
            entity.IsAvailable = true;
            entity.CreatedOn = Now;
            entity.UpdatedOn = Now;
        });

        Client = Harness.Bus.CreateRequestClient<DiscontinueSellableItemRequest>();
    }

    protected override async Task Act()
    {
        Response = await Client.GetResponse<DiscontinueSellableItemResponse>(new
        {
            SellableItemId = ExistingId,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    public class When_discontinuing_published_item : SellableItem_Discontinue
    {
        [Test]
        public async Task It_should_transition_to_discontinued() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.SellableItemId.ShouldBe(ExistingId);

            var saga = SagaHarness.Sagas.Contains(ExistingId);
            saga.ShouldNotBeNull();
            saga.CurrentState.ShouldBe(SellableItemStates.Discontinued);

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
                entity.Sku = "HIDDEN-SKU";
                entity.Name = "Hidden Item";
                entity.BasePrice = 75.00m;
                entity.ItemType = "simple-product";
                entity.Payload = new Dictionary<string, object?>();
                entity.IsAvailable = true;
                entity.CreatedOn = Now;
                entity.UpdatedOn = Now;
            });
        }

        [Test]
        public async Task It_should_transition_to_discontinued() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.SellableItemId.ShouldBe(ExistingId);

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
                entity.Sku = "DISC-SKU";
                entity.Name = "Discontinued Item";
                entity.BasePrice = 100.00m;
                entity.ItemType = "simple-product";
                entity.Payload = new Dictionary<string, object?>();
                entity.IsAvailable = false;
                entity.CreatedOn = Now;
                entity.UpdatedOn = Now;
            });
        }

        [Test]
        public async Task It_should_throw_state_exception() => await Assert.MultipleAsync(() =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<RequestFaultException>();

            var saga = SagaHarness.Sagas.Contains(ExistingId);
            saga.ShouldNotBeNull();
            saga.CurrentState.ShouldBe(SellableItemStates.Discontinued);
            return Task.CompletedTask;
        });
    }
}
