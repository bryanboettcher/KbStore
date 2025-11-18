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
            entity.Sku = "DRAFT-SKU";
            entity.Name = "Draft Item";
            entity.BasePrice = 50.00m;
            entity.ItemType = "simple-product";
            entity.Payload = new Dictionary<string, object?>();
            entity.IsAvailable = true;
            entity.CreatedOn = Now;
            entity.UpdatedOn = Now;
        });

        Client = CreateRequestClient<DeleteSellableItemRequest>();
    }

    protected override async Task Act()
    {
        await Client.GetResponse<DeleteSellableItemResponse>(new
        {
            SellableItemId = ExistingId,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    public class When_deleting_draft_item : SellableItem_Delete
    {
        [Test]
        public async Task It_should_finalize_saga() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            (await SagaHarness.NotExists(ExistingId)).ShouldNotBe(ExistingId);

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
                entity.Sku = "PUB-SKU";
                entity.Name = "Published Item";
                entity.BasePrice = 75.00m;
                entity.ItemType = "simple-product";
                entity.Payload = new Dictionary<string, object?>();
                entity.IsAvailable = true;
                entity.CreatedOn = Now;
                entity.UpdatedOn = Now;
            });
        }

        [Test]
        public async Task It_should_not_delete() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();

            var saga = SagaHarness.Sagas.Contains(ExistingId);
            saga.ShouldNotBeNull();
            saga.CurrentState.ShouldBe(SellableItemStates.Published);

            (await Harness.Published.Any<SellableItemDeleted>()).ShouldBeFalse();
        });
    }
}
