namespace KbStore.Storefront.Tests.Services.SellableItems;

using Abstractions.Contracts;
using KbStore.Storefront.Domains.SellableItems;
using KbStore.Storefront.Services;
using NUnit.Framework;
using Shouldly;


public abstract class SellableItemQueryService_GetBySku : QueryService_Tests<SellableItemQueryService>
{
    protected SellableItemModel? Result;

    protected string Sku = null!;

    protected override void Arrange()
    {
        base.Arrange();

        Sku = "TEST_SKU";
    }

    protected override async Task Act()
    {
        Result = await Subject.GetBySkuAsync(Sku);
    }

    public class When_getting_by_sku_found : SellableItemQueryService_GetBySku
    {
        protected override void Arrange()
        {
            base.Arrange();

            var entity = new SellableItemEntity
            {
                CorrelationId = ExistingId,
                ProductId = null,
                Sku = "TEST_SKU",
                Name = "Test Item",
                Description = "A test item",
                BasePrice = 19.99m,
                ItemType = "Standard",
                Payload = new Dictionary<string, object?> { { "key1", "value1" } },
                IsAvailable = true,
                CurrentState = 2,
                Version = 1,
                CreatedOn = Now,
                UpdatedOn = Now
            };

            SetupFindReturnsItems(entity);
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Result.ShouldNotBeNull();
            Result.ShouldSatisfyAllConditions(x =>
            {
                x.SellableItemId.ShouldBe(ExistingId);
                x.Sku.ShouldBe("TEST_SKU");
                x.Name.ShouldBe("Test Item");
                x.Description.ShouldBe("A test item");
                x.BasePrice.ShouldBe(19.99m);
                x.ItemType.ShouldBe("Standard");
                x.IsAvailable.ShouldBeTrue();
                x.Version.ShouldBe(1);
            });
        });
    }

    public class When_getting_by_sku_not_found : SellableItemQueryService_GetBySku
    {
        protected override void Arrange()
        {
            base.Arrange();
            SetupFindReturnsEmpty();
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Result.ShouldBeNull();
        });
    }
}
