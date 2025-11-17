namespace KbStore.Storefront.Tests.Services.SellableItems;

using Abstractions.Contracts;
using KbStore.Storefront.Domains.SellableItems;
using KbStore.Storefront.Services;
using NUnit.Framework;
using Shouldly;


public abstract class SellableItemQueryService_GetPublished : QueryService_Tests<SellableItemQueryService>
{
    protected List<SellableItemModel> Result = null!;

    protected override void Arrange()
    {
        base.Arrange();

        Result = new List<SellableItemModel>();
    }

    protected override async Task Act()
    {
        Result = await Subject.GetPublishedAsync();
    }

    public class When_getting_published_empty : SellableItemQueryService_GetPublished
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

    public class When_getting_published_with_items : SellableItemQueryService_GetPublished
    {
        protected override void Arrange()
        {
            base.Arrange();

            var publishedEntity1 = new SellableItemEntity
            {
                CorrelationId = ExistingId,
                ProductId = null,
                Sku = "TEST_SKU_1",
                Name = "Published Item 1",
                Description = "First published item",
                BasePrice = 19.99m,
                ItemType = "Standard",
                Payload = new Dictionary<string, object?> { { "key1", "value1" } },
                IsAvailable = true,
                CurrentState = 2, // Published state
                Version = 1,
                CreatedOn = Now,
                UpdatedOn = Now
            };

            var publishedEntity2 = new SellableItemEntity
            {
                CorrelationId = Guid.NewGuid(),
                ProductId = null,
                Sku = "TEST_SKU_2",
                Name = "Published Item 2",
                Description = "Second published item",
                BasePrice = 29.99m,
                ItemType = "Premium",
                Payload = new Dictionary<string, object?> { { "key2", "value2" } },
                IsAvailable = true,
                CurrentState = 2, // Published state
                Version = 1,
                CreatedOn = Now,
                UpdatedOn = Now
            };

            SetupFindReturnsItems(publishedEntity1, publishedEntity2);
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Result.ShouldNotBeNull();
            Result.Count.ShouldBe(2);
            Result.ShouldAllBe(x => x.Name.Contains("Published Item"));
        });
    }
}
