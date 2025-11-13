namespace KbStore.Storefront.Tests.Domains.SellableItems;

using KbStore.Storefront.Abstractions.Contracts;
using KbStore.Storefront.Domains.SellableItems;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class SellableItem_UpdatePrice : StateMachine_Tests<SellableItemStateMachine, SellableItemEntity>
{
    protected IRequestClient<UpdateSellableItemPriceRequest> Client = null!;
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

        Client = Harness.Bus.CreateRequestClient<UpdateSellableItemPriceRequest>();
    }

    protected override async Task Act()
    {
        Response = await Client.GetResponse<SellableItemResponse>(new
        {
            SellableItemId = ExistingId,
            BasePrice = 75.99m
        });
    }

    public class When_updating_price_in_draft : SellableItem_UpdatePrice
    {
        [Test]
        public async Task It_should_update_and_publish_event() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Id.ShouldBe(ExistingId);
            Response.Message.BasePrice.ShouldBe(75.99m);
            Response.Message.State.ShouldBe("Draft");
            Response.Message.UpdatedAt.ShouldNotBeNull();

            var saga = SagaHarness.Sagas.Contains(ExistingId);
            saga.ShouldNotBeNull();
            saga.BasePrice.ShouldBe(75.99m);
            saga.UpdatedAt.ShouldNotBeNull();

            (await Harness.Published.Any<SellableItemPriceChanged>()).ShouldBeTrue();
        });
    }

    public class When_updating_price_in_published : SellableItem_UpdatePrice
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddOrUpdateSagaInstance<SellableItemEntity>(ExistingId, entity =>
            {
                entity.CurrentState = SellableItemStates.Published;
                entity.SKU = "PUB-SKU";
                entity.Name = "Published Item";
                entity.BasePrice = 100.00m;
                entity.ItemType = "simple-product";
                entity.Payload = new Dictionary<string, object?>();
                entity.IsAvailable = true;
                entity.CreatedAt = Now;
            });
        }

        protected override async Task Act()
        {
            Response = await Client.GetResponse<SellableItemResponse>(new
            {
                SellableItemId = ExistingId,
                BasePrice = 89.99m
            });
        }

        [Test]
        public async Task It_should_update_and_publish_event() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.BasePrice.ShouldBe(89.99m);
            Response.Message.State.ShouldBe("Published");

            var saga = SagaHarness.Sagas.Contains(ExistingId);
            saga.ShouldNotBeNull();
            saga.BasePrice.ShouldBe(89.99m);

            (await Harness.Published.Any<SellableItemPriceChanged>()).ShouldBeTrue();
        });
    }

    public class When_updating_price_in_hidden : SellableItem_UpdatePrice
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddOrUpdateSagaInstance<SellableItemEntity>(ExistingId, entity =>
            {
                entity.CurrentState = SellableItemStates.Hidden;
                entity.SKU = "HIDDEN-SKU";
                entity.Name = "Hidden Item";
                entity.BasePrice = 60.00m;
                entity.ItemType = "simple-product";
                entity.Payload = new Dictionary<string, object?>();
                entity.IsAvailable = true;
                entity.CreatedAt = Now;
            });
        }

        [Test]
        public async Task It_should_update_and_publish_event() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.BasePrice.ShouldBe(75.99m);
            Response.Message.State.ShouldBe("Hidden");

            var saga = SagaHarness.Sagas.Contains(ExistingId);
            saga.ShouldNotBeNull();
            saga.BasePrice.ShouldBe(75.99m);

            (await Harness.Published.Any<SellableItemPriceChanged>()).ShouldBeTrue();
        });
    }

    public class When_updating_price_in_discontinued : SellableItem_UpdatePrice
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
            });
        }

        [Test]
        public async Task It_should_throw_state_exception() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<RequestFaultException>();

            var saga = SagaHarness.Sagas.Contains(ExistingId);
            saga.ShouldNotBeNull();
            saga.BasePrice.ShouldBe(100.00m);
            saga.CurrentState.ShouldBe(SellableItemStates.Discontinued);

            (await Harness.Published.Any<SellableItemPriceChanged>()).ShouldBeFalse();
        });
    }
}
