namespace KbStore.Storefront.Tests.Domains.SellableItems;

using KbStore.Storefront.Abstractions.Contracts;
using KbStore.Storefront.Domains.SellableItems;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class SellableItem_Create : StateMachine_Tests<SellableItemStateMachine, SellableItemEntity>
{
    protected IRequestClient<CreateSellableItemRequest> Client = null!;
    protected Response<CreateSellableItemResponse> Response = null!;

    protected override void Arrange()
    {
        Client = CreateRequestClient<CreateSellableItemRequest>();
    }

    protected override async Task Act()
    {
        Response = await Client.GetResponse<CreateSellableItemResponse>(new
        {
            Sku = "TEST-SKU-001",
            Name = "Test Product",
            Description = "Test product description",
            BasePrice = 99.99m,
            ItemType = "simple-product",
            Payload = new Dictionary<string, object?>
            {
                { "color", "blue" },
                { "size", "medium" }
            },
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    public class When_creating_sellable_item : SellableItem_Create
    {
        [Test]
        public async Task It_should_create_successfully() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Sku.ShouldBe("TEST-SKU-001");
            Response.Message.Name.ShouldBe("Test Product");
            Response.Message.Description.ShouldBe("Test product description");
            Response.Message.BasePrice.ShouldBe(99.99m);
            Response.Message.ItemType.ShouldBe("simple-product");
            Response.Message.IsAvailable.ShouldBeTrue();
            Response.Message.Payload.ShouldNotBeNull();
            Response.Message.Payload["color"].ShouldBe("blue");
            Response.Message.Payload["size"].ShouldBe("medium");

            var sagaId = Response.Message.SellableItemId;
            var saga = SagaHarness.Sagas.Contains(sagaId);
            saga.ShouldNotBeNull();
            saga.CorrelationId.ShouldBe(sagaId);
            saga.CurrentState.ShouldBe(SellableItemStates.Draft);
            saga.Sku.ShouldBe("TEST-SKU-001");
            saga.Name.ShouldBe("Test Product");
            saga.BasePrice.ShouldBe(99.99m);
            saga.IsAvailable.ShouldBeTrue();
            saga.CreatedOn.ShouldNotBe(default);
            saga.UpdatedOn.ShouldBe(saga.CreatedOn);

            (await Harness.Published.Any<SellableItemCreated>()).ShouldBeTrue();
        });
    }

    public class When_creating_without_optional_fields : SellableItem_Create
    {
        protected override async Task Act()
        {
            Response = await Client.GetResponse<CreateSellableItemResponse>(new
            {
                Sku = "MIN-SKU",
                Name = "Minimal Product",
                Description = (string?)null,
                BasePrice = 10.00m,
                ItemType = "simple",
                Payload = new Dictionary<string, object?>(),
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        [Test]
        public async Task It_should_create_successfully() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Sku.ShouldBe("MIN-SKU");
            Response.Message.Name.ShouldBe("Minimal Product");
            Response.Message.Description.ShouldBeNull();
            Response.Message.BasePrice.ShouldBe(10.00m);

            var sagaId = Response.Message.SellableItemId;
            var saga = SagaHarness.Sagas.Contains(sagaId);
            saga.ShouldNotBeNull();
            saga.CurrentState.ShouldBe(SellableItemStates.Draft);
            saga.Description.ShouldBeNull();

            (await Harness.Published.Any<SellableItemCreated>()).ShouldBeTrue();
        });
    }
}
