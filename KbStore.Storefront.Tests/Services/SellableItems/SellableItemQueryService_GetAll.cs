namespace KbStore.Storefront.Tests.Services.SellableItems;

using Abstractions.Contracts;
using KbStore.Storefront.Domains.SellableItems;
using KbStore.Storefront.Services;
using NUnit.Framework;
using Shouldly;


public abstract class SellableItemQueryService_GetAll : QueryService_Tests<SellableItemQueryService>
{
    protected List<SellableItemModel> Result = null!;

    protected override void Arrange()
    {
        base.Arrange();

        Result = new List<SellableItemModel>();
    }

    protected override async Task Act()
    {
        Result = await Subject.GetAllAsync();
    }

    public class When_getting_all_empty : SellableItemQueryService_GetAll
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

            Result.ShouldNotBeNull();
            Result.Count.ShouldBe(0);
        });
    }

    public class When_getting_all_with_items : SellableItemQueryService_GetAll
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
                Description = "First test item",
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
                Description = "Second test item",
                BasePrice = 29.99m,
                ItemType = "Premium",
                Payload = new Dictionary<string, object?> { { "key2", "value2" } },
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
            Result.ShouldSatisfyAllConditions(
                x => x[0].Sku.ShouldBe("TEST_SKU_1"),
                x => x[1].Sku.ShouldBe("TEST_SKU_2")
            );
        });
    }
}
