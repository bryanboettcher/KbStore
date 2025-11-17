namespace KbStore.ApiService.Tests.Api.Endpoints.Storefront.SellableItem;

using KbStore.ApiService.Endpoints.Storefront;
using KbStore.Storefront.Abstractions.Contracts;
using KbStore.Storefront.Abstractions.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shouldly;


public abstract class GetById_Tests : SellableItemEndpoints_Tests
{
    protected static readonly Guid TestId = Guid.NewGuid();

    public class When_getting_successfully : GetById_Tests
    {
        protected override void Arrange()
        {
            MockOf<ISellableItemQueryService>()
                .GetByIdAsync(TestId, Arg.Any<CancellationToken>())
                .Returns(new TestSellableItemModel
                {
                    SellableItemId = TestId,
                    ProductId = Guid.NewGuid(),
                    Sku = "TEST_SKU_123",
                    Name = "Test Item",
                    Description = "Test description",
                    BasePrice = 19.99m,
                    ItemType = "Physical",
                    Payload = new Dictionary<string, object?> { { "weight", 2.5 } },
                    IsAvailable = true,
                    Version = 1
                });
        }

        protected override async Task Act()
            => Output = await Execute(SellableItemEndpoints.GetById, TestId);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<SellableItemModel>>();
    }

    public class When_not_found : GetById_Tests
    {
        protected override void Arrange()
        {
            MockOf<ISellableItemQueryService>()
                .GetByIdAsync(TestId, Arg.Any<CancellationToken>())
                .Returns((SellableItemModel?)null);
        }

        protected override async Task Act()
            => Output = await Execute(SellableItemEndpoints.GetById, TestId);

        [Test]
        public void It_should_return_not_found() => Output.ShouldBeOfType<NotFound>();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();
    }

    public class When_service_throws_exception : GetById_Tests
    {
        protected override void Arrange()
        {
            MockOf<ISellableItemQueryService>()
                .GetByIdAsync(TestId, Arg.Any<CancellationToken>())
                .Throws(new TestSellableItemException("Test error"));
        }

        protected override async Task Act()
            => Output = await Execute(SellableItemEndpoints.GetById, TestId);

        [Test]
        public void It_should_throw() => LastException.ShouldBeOfType<TestSellableItemException>();

        [Test]
        public void It_should_have_correct_message() => LastException!.Message.ShouldBe("Test error");
    }
}
