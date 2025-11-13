namespace KbStore.Storefront.Tests.Domains.SellableItems;

using KbStore.Storefront.Abstractions.Contracts;
using KbStore.Storefront.Domains.SellableItems;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class SellableItem_Publish : StateMachine_Tests<SellableItemStateMachine, SellableItemEntity>
{
    protected IRequestClient<PublishSellableItemRequest> Client = null!;
    protected Response<SellableItemResponse> Response = null!;

    protected override void Arrange()
    {

        Harness.AddOrUpdateSagaInstance<SellableItemEntity>(ExistingId, entity =>
        {
            entity.CurrentState = SellableItemStates.Draft;
            entity.SKU = "DRAFT-SKU";
            entity.Name = "Draft Item";
            entity.BasePrice = 50.00m;
            entity.ItemType = "simple-product";
            entity.Payload = new Dictionary<string, object?>();
            entity.IsAvailable = true;
            entity.CreatedAt = Now;
        });

        Client = Harness.Bus.CreateRequestClient<PublishSellableItemRequest>();
    }

    protected override async Task Act()
    {
        Response = await Client.GetResponse<SellableItemResponse>(new
        {
            SellableItemId = ExistingId
        });
    }

    public class When_publishing_from_draft : SellableItem_Publish
    {
        [Test]
        public async Task It_should_transition_to_published() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Id.ShouldBe(ExistingId);
            Response.Message.State.ShouldBe("Published");
            Response.Message.UpdatedAt.ShouldNotBeNull();

            var saga = SagaHarness.Sagas.Contains(ExistingId);
            saga.ShouldNotBeNull();
            saga.CurrentState.ShouldBe(SellableItemStates.Published);
            saga.UpdatedAt.ShouldNotBeNull();

            (await Harness.Published.Any<SellableItemPublished>()).ShouldBeTrue();
        });
    }

    public class When_publishing_from_hidden : SellableItem_Publish
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
        public async Task It_should_transition_to_published() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Id.ShouldBe(ExistingId);
            Response.Message.State.ShouldBe("Published");

            var saga = SagaHarness.Sagas.Contains(ExistingId);
            saga.ShouldNotBeNull();
            saga.CurrentState.ShouldBe(SellableItemStates.Published);

            (await Harness.Published.Any<SellableItemPublished>()).ShouldBeTrue();
        });
    }

    public class When_publishing_discontinued_item : SellableItem_Publish
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

            (await Harness.Published.Any<SellableItemPublished>()).ShouldBeFalse();
        });
    }
}
