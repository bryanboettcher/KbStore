namespace KbStore.Storefront.Tests.Domains.SellableItems;

using KbStore.Storefront.Abstractions.Contracts;
using KbStore.Storefront.Domains.SellableItems;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class SellableItem_Delete : StateMachine_Tests<SellableItemStateMachine, SellableItemEntity>
{
    protected IRequestClient<DeleteSellableItemRequest> Client = null!;

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

        Client = Harness.Bus.CreateRequestClient<DeleteSellableItemRequest>();
    }

    protected override async Task Act()
    {
        // Fire and forget - delete is one-way
        await Client.GetResponse<SellableItemDeleted>(new
        {
            SellableItemId = ExistingId
        });
    }

    public class When_deleting_draft_item : SellableItem_Delete
    {
        [Test]
        public async Task It_should_finalize_saga() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            // Saga should be finalized
            (await SagaHarness.Exists(ExistingId, machine => machine.Final)).ShouldNotBeNull();

            (await Harness.Published.Any<SellableItemDeleted>()).ShouldBeTrue();
        });
    }

    public class When_deleting_published_item : SellableItem_Delete
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddOrUpdateSagaInstance<SellableItemEntity>(ExistingId, entity =>
            {
                entity.CurrentState = SellableItemStates.Published;
                entity.SKU = "PUB-SKU";
                entity.Name = "Published Item";
                entity.BasePrice = 75.00m;
                entity.ItemType = "simple-product";
                entity.Payload = new Dictionary<string, object?>();
                entity.IsAvailable = true;
                entity.CreatedAt = Now;
            });
        }

        [Test]
        public async Task It_should_not_delete() => await Assert.MultipleAsync(async () =>
        {
            // Published items cannot be deleted - only Draft items can be deleted
            // The state machine doesn't define a transition for Delete from Published state
            LastException.ShouldNotBeNull();

            var saga = SagaHarness.Sagas.Contains(ExistingId);
            saga.ShouldNotBeNull();
            saga.CurrentState.ShouldBe(SellableItemStates.Published);

            (await Harness.Published.Any<SellableItemDeleted>()).ShouldBeFalse();
        });
    }
}
