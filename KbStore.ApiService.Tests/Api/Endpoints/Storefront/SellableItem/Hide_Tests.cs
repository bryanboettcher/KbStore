namespace KbStore.ApiService.Tests.Api.Endpoints.Storefront.SellableItem;

using KbStore.ApiService.Endpoints.Storefront;
using KbStore.Storefront.Abstractions.Contracts;
using KbStore.Storefront.Abstractions.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shouldly;


public abstract class Hide_Tests : SellableItemEndpoints_Tests
{
    protected static readonly Guid TestId = Guid.NewGuid();

    public class When_hiding_successfully : Hide_Tests
    {
        protected override void Arrange()
        {
            MockOf<ISellableItemCommandService>()
                .HideAsync(TestId, Arg.Any<CancellationToken>())
                .Returns(new TestSellableItemModel
                {
                    SellableItemId = TestId,
                    Sku = "TEST_SKU_123",
                    Name = "Test Item",
                    BasePrice = 19.99m,
                    ItemType = "Physical",
                    IsAvailable = false,
                    Version = 2
                });
        }

        protected override async Task Act()
            => Output = await Execute(SellableItemEndpoints.Hide, TestId);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<SellableItemModel>>();
    }

    public class When_service_throws_exception : Hide_Tests
    {
        protected override void Arrange()
        {
            MockOf<ISellableItemCommandService>()
                .HideAsync(TestId, Arg.Any<CancellationToken>())
                .Throws(new TestSellableItemException("Test error"));
        }

        protected override async Task Act()
            => Output = await Execute(SellableItemEndpoints.Hide, TestId);

        [Test]
        public void It_should_throw() => LastException.ShouldBeOfType<TestSellableItemException>();

        [Test]
        public void It_should_have_correct_message() => LastException!.Message.ShouldBe("Test error");
    }
}
