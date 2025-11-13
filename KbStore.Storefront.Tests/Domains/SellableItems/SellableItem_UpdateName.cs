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
    protected Response<SellableItemResponse> Response = null!;

    protected override void Arrange()
    {

        Harness.AddOrUpdateSagaInstance<SellableItemEntity>(ExistingId, entity =>
        {
            entity.CurrentState = SellableItemStates.Draft;
            entity.SKU = "DRAFT-SKU";
            entity.Name = "Original Name";
            entity.BasePrice = 50.00m;
            entity.ItemType = "simple-product";
            entity.Payload = new Dictionary<string, object?>();
            entity.IsAvailable = true;
            entity.CreatedAt = Now;
        });

        Client = Harness.Bus.CreateRequestClient<UpdateSellableItemNameRequest>();
    }

    protected override async Task Act()
    {
        Response = await Client.GetResponse<SellableItemResponse>(new
        {
            SellableItemId = ExistingId,
            Name = "Updated Name"
        });
    }

    public class When_updating_name_in_draft : SellableItem_UpdateName
    {
        [Test]
        public async Task It_should_update_successfully() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Id.ShouldBe(ExistingId);
            Response.Message.Name.ShouldBe("Updated Name");
            Response.Message.State.ShouldBe("Draft");
            Response.Message.UpdatedAt.ShouldNotBeNull();

            var saga = SagaHarness.Sagas.Contains(ExistingId);
            saga.ShouldNotBeNull();
            saga.Name.ShouldBe("Updated Name");
            saga.UpdatedAt.ShouldNotBeNull();

            // Name updates don't publish events
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
        public async Task It_should_update_successfully() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Name.ShouldBe("Updated Name");
            Response.Message.State.ShouldBe("Published");

            var saga = SagaHarness.Sagas.Contains(ExistingId);
            saga.ShouldNotBeNull();
            saga.Name.ShouldBe("Updated Name");
            saga.CurrentState.ShouldBe(SellableItemStates.Published);
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
        public async Task It_should_update_successfully() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Name.ShouldBe("Updated Name");
            Response.Message.State.ShouldBe("Hidden");

            var saga = SagaHarness.Sagas.Contains(ExistingId);
            saga.ShouldNotBeNull();
            saga.Name.ShouldBe("Updated Name");
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
            saga.Name.ShouldBe("Discontinued Item");
            saga.CurrentState.ShouldBe(SellableItemStates.Discontinued);
        });
    }
}
