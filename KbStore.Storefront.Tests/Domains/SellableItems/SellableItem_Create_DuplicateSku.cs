namespace KbStore.Storefront.Tests.Domains.SellableItems;

using KbStore.Storefront.Abstractions.Contracts;
using KbStore.Storefront.Domains.SellableItems;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class SellableItem_Create_DuplicateSku : StateMachine_Tests<SellableItemStateMachine, SellableItemEntity>
{
    protected IRequestClient<CreateSellableItemRequest> Client = null!;
    protected Response<CreateSellableItemResponse>? FirstResponse;

    protected override void Arrange()
    {
        Client = CreateRequestClient<CreateSellableItemRequest>();
    }

    protected override async Task Act()
    {
        FirstResponse = await Client.GetResponse<CreateSellableItemResponse>(new
        {
            Sku = "DUPLICATE-SKU",
            Name = "First Item",
            Description = "First item with this SKU",
            BasePrice = 100.00m,
            ItemType = "simple-product",
            Payload = new Dictionary<string, object?>(),
            Timestamp = DateTimeOffset.UtcNow
        });

        try
        {
            await Client.GetResponse<CreateSellableItemResponse>(new
            {
                Sku = "DUPLICATE-SKU",
                Name = "Second Item",
                Description = "Second item attempting same SKU",
                BasePrice = 200.00m,
                ItemType = "simple-product",
                Payload = new Dictionary<string, object?>(),
                Timestamp = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            LastException = ex;
        }
    }

    public class When_creating_duplicate_sku : SellableItem_Create_DuplicateSku
    {
        [Test]
        public void It_should_create_first_item_successfully()
        {
            FirstResponse.ShouldNotBeNull();
            FirstResponse.Message.Sku.ShouldBe("DUPLICATE-SKU");
            FirstResponse.Message.Name.ShouldBe("First Item");
        }

        [Test]
        public void It_should_reject_duplicate_sku()
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<RequestFaultException>();

            var faultException = (RequestFaultException)LastException;
            faultException.Message.ShouldContain("DUPLICATE-SKU");
        }

        [Test]
        public void It_should_only_have_one_saga_instance()
        {
            FirstResponse.ShouldNotBeNull();
            LastException.ShouldNotBeNull();
        }
    }
}
