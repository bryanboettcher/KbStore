namespace KbStore.Storefront.Tests.Domains.SellableItems;

using KbStore.Storefront.Abstractions.Contracts;
using KbStore.Storefront.Domains.SellableItems;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class SellableItem_UpdateName : StateMachine_Tests<SellableItemStateMachine, SellableItemEntity>
{
    protected IRequestClient<UpdateSellableItemNameRequest> Client = null!;
    protected Response<UpdateSellableItemResponse> Response = null!;

    protected override void Arrange()
    {
        Harness.AddOrUpdateSagaInstance<SellableItemEntity>(ExistingId, entity =>
        {
            entity.CurrentState = SellableItemStates.Draft;
            entity.Sku = "DRAFT-SKU";
            entity.Name = "Original Name";
            entity.BasePrice = 50.00m;
            entity.ItemType = "simple-product";
            entity.Payload = new Dictionary<string, object?>();
            entity.IsAvailable = true;
            entity.CreatedOn = Now;
            entity.UpdatedOn = Now;
        });

        Client = Harness.Bus.CreateRequestClient<UpdateSellableItemNameRequest>();
    }

    protected override async Task Act()
    {
        Response = await Client.GetResponse<UpdateSellableItemResponse>(new
        {
            SellableItemId = ExistingId,
            Name = "Updated Name",
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    public class When_updating_name_in_draft : SellableItem_UpdateName
    {
        [Test]
        public async Task It_should_update_successfully() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.SellableItemId.ShouldBe(ExistingId);
            Response.Message.Name.ShouldBe("Updated Name");

            var saga = SagaHarness.Sagas.Contains(ExistingId);
            saga.ShouldNotBeNull();
            saga.Name.ShouldBe("Updated Name");
            saga.CurrentState.ShouldBe(SellableItemStates.Draft);

            (await Harness.Published.Any<SellableItemCreated>()).ShouldBeFalse();
        });
    }

    public class When_updating_name_in_published : SellableItem_UpdateName
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
        public async Task It_should_update_successfully() => await Assert.MultipleAsync(() =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Name.ShouldBe("Updated Name");

            var saga = SagaHarness.Sagas.Contains(ExistingId);
            saga.ShouldNotBeNull();
            saga.Name.ShouldBe("Updated Name");
            saga.CurrentState.ShouldBe(SellableItemStates.Published);
            return Task.CompletedTask;
        });
    }

    public class When_updating_name_in_hidden : SellableItem_UpdateName
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddOrUpdateSagaInstance<SellableItemEntity>(ExistingId, entity =>
            {
                entity.CurrentState = SellableItemStates.Hidden;
                entity.Sku = "HIDDEN-SKU";
                entity.Name = "Hidden Item";
                entity.BasePrice = 60.00m;
                entity.ItemType = "simple-product";
                entity.Payload = new Dictionary<string, object?>();
                entity.IsAvailable = true;
                entity.CreatedOn = Now;
                entity.UpdatedOn = Now;
            });
        }

        [Test]
        public async Task It_should_update_successfully() => await Assert.MultipleAsync(() =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Name.ShouldBe("Updated Name");

            var saga = SagaHarness.Sagas.Contains(ExistingId);
            saga.ShouldNotBeNull();
            saga.Name.ShouldBe("Updated Name");
            saga.CurrentState.ShouldBe(SellableItemStates.Hidden);
            return Task.CompletedTask;
        });
    }

    public class When_updating_name_in_discontinued : SellableItem_UpdateName
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
            saga.Name.ShouldBe("Discontinued Item");
            saga.CurrentState.ShouldBe(SellableItemStates.Discontinued);
            return Task.CompletedTask;
        });
    }
}
