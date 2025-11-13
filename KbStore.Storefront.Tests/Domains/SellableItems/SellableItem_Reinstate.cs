namespace KbStore.Storefront.Tests.Domains.SellableItems;

using KbStore.Storefront.Abstractions.Contracts;
using KbStore.Storefront.Domains.SellableItems;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class SellableItem_Reinstate : StateMachine_Tests<SellableItemStateMachine, SellableItemEntity>
{
    protected IRequestClient<ReinstateSellableItemRequest> Client = null!;
    protected Response<SellableItemResponse> Response = null!;

    protected override void Arrange()
    {

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

        Client = Harness.Bus.CreateRequestClient<ReinstateSellableItemRequest>();
    }

    protected override async Task Act()
    {
        Response = await Client.GetResponse<SellableItemResponse>(new
        {
            SellableItemId = ExistingId
        });
    }

    public class When_reinstating_discontinued_item : SellableItem_Reinstate
    {
        [Test]
        public async Task It_should_transition_to_draft() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Id.ShouldBe(ExistingId);
            Response.Message.State.ShouldBe("Draft");
            Response.Message.UpdatedAt.ShouldNotBeNull();

            var saga = SagaHarness.Sagas.Contains(ExistingId);
            saga.ShouldNotBeNull();
            saga.CurrentState.ShouldBe(SellableItemStates.Draft);
            saga.UpdatedAt.ShouldNotBeNull();

            // Reinstate is internal operation - no event published
            (await Harness.Published.Any<SellableItemCreated>()).ShouldBeFalse();
            (await Harness.Published.Any<SellableItemPublished>()).ShouldBeFalse();
        });
    }
}
