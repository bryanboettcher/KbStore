namespace KbStore.Storefront.Tests.Services.SellableItems;

using Abstractions.Contracts;
using KbStore.Storefront.Domains.SellableItems;
using KbStore.Storefront.Services;
using NUnit.Framework;
using Shouldly;


public abstract class SellableItemQueryService_GetByItemType : QueryService_Tests<SellableItemQueryService>
{
    protected List<SellableItemModel> Result = null!;

    protected string ItemType = null!;

    protected override void Arrange()
    {
        base.Arrange();

        ItemType = "Standard";
        Result = new List<SellableItemModel>();
    }

    protected override async Task Act()
    {
        Result = await Subject.GetByItemTypeAsync(ItemType);
    }

    public class When_getting_by_item_type_found : SellableItemQueryService_GetByItemType
    {
        protected override void Arrange()
        {
            base.Arrange();

            var entity1 = new SellableItemEntity
            {
                CorrelationId = ExistingId,
                ProductId = null,
                Sku = "TEST_SKU_1",
                Name = "Test Item 1",
                Description = "First standard item",
                BasePrice = 19.99m,
                ItemType = "Standard",
                Payload = new Dictionary<string, object?> { { "key1", "value1" } },
                IsAvailable = true,
                CurrentState = 2,
                Version = 1,
                CreatedOn = Now,
                UpdatedOn = Now
            };

            var entity2 = new SellableItemEntity
            {
                CorrelationId = Guid.NewGuid(),
                ProductId = null,
                Sku = "TEST_SKU_2",
                Name = "Test Item 2",
                Description = "Second standard item",
                BasePrice = 29.99m,
                ItemType = "Standard",
                Payload = new Dictionary<string, object?> { { "key2", "value2" } },
                IsAvailable = true,
                CurrentState = 2,
                Version = 1,
                CreatedOn = Now,
                UpdatedOn = Now
            };

            var entity3 = new SellableItemEntity
            {
                CorrelationId = Guid.NewGuid(),
                ProductId = null,
                Sku = "TEST_SKU_3",
                Name = "Test Item 3",
                Description = "Premium item",
                BasePrice = 99.99m,
                ItemType = "Premium",
                Payload = new Dictionary<string, object?> { { "key3", "value3" } },
                IsAvailable = true,
                CurrentState = 2,
                Version = 1,
                CreatedOn = Now,
                UpdatedOn = Now
            };

            SetupFindReturnsItems(entity1, entity2);
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Result.ShouldNotBeNull();
            Result.Count.ShouldBe(2);
            Result.ShouldAllBe(x => x.ItemType == "Standard");
        });
    }

    public class When_getting_by_item_type_not_found : SellableItemQueryService_GetByItemType
    {
        protected override void Arrange()
        {
            base.Arrange();
            ItemType = "NonExistent";
            SetupFindReturnsEmpty();
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Result.ShouldNotBeNull();
            Result.Count.ShouldBe(0);
        });
    }
}
