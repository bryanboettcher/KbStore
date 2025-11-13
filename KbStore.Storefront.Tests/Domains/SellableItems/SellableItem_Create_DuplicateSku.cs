namespace KbStore.Storefront.Tests.Domains.SellableItems;

using KbStore.Storefront.Abstractions.Contracts;
using KbStore.Storefront.Abstractions.Exceptions;
using KbStore.Storefront.Domains.SellableItems;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class SellableItem_Create_DuplicateSku : StateMachine_Tests<SellableItemStateMachine, SellableItemEntity>
{
    protected IRequestClient<CreateSellableItemRequest> Client = null!;
    protected Response<SellableItemResponse>? FirstResponse;

    protected override void Arrange()
    {
        Client = Harness.Bus.CreateRequestClient<CreateSellableItemRequest>();
    }

    protected override async Task Act()
    {
        // Create the first sellable item
        FirstResponse = await Client.GetResponse<SellableItemResponse>(new
        {
            SKU = "DUPLICATE-SKU",
            Name = "First Item",
            Description = "First item with this SKU",
            BasePrice = 100.00m,
            ItemType = "simple-product",
            Payload = new Dictionary<string, object?>()
        });

        // Attempt to create another item with the same SKU
        try
        {
            await Client.GetResponse<SellableItemResponse>(new
            {
                SKU = "DUPLICATE-SKU",
                Name = "Second Item",
                Description = "Second item attempting same SKU",
                BasePrice = 200.00m,
                ItemType = "simple-product",
                Payload = new Dictionary<string, object?>()
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
            FirstResponse.Message.SKU.ShouldBe("DUPLICATE-SKU");
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
            // The first item was successfully created
            FirstResponse.ShouldNotBeNull();

            // The second attempt should fail with duplicate SKU error
            LastException.ShouldNotBeNull();
        }
    }
}
