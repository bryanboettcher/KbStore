namespace KbStore.ApiService.Tests.Api.Endpoints.Storefront.SellableItem;

using KbStore.ApiService.Endpoints.Storefront;
using KbStore.Storefront.Abstractions.Contracts;
using KbStore.Storefront.Abstractions.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shouldly;


public abstract class UpdatePrice_Tests : SellableItemEndpoints_Tests
{
    protected static readonly Guid TestId = Guid.NewGuid();
    protected static readonly UpdatePricePayload ValidPayload = new() { BasePrice = 29.99m };
    protected static readonly UpdatePricePayload InvalidPayload = new() { BasePrice = -1.00m };

    public class When_updating_successfully : UpdatePrice_Tests
    {
        protected override void Arrange()
        {
            MockOf<ISellableItemCommandService>()
                .UpdatePriceAsync(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>())
                .Returns(new TestSellableItemModel
                {
                    SellableItemId = TestId,
                    Sku = "TEST_SKU_123",
                    Name = "Test Item",
                    BasePrice = ValidPayload.BasePrice,
                    ItemType = "Physical",
                    IsAvailable = true,
                    Version = 2
                });
        }

        protected override async Task Act()
            => Output = await Execute(SellableItemEndpoints.UpdatePrice, TestId, ValidPayload);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<SellableItemModel>>();
    }

    public class When_payload_is_invalid : UpdatePrice_Tests
    {
        protected override void Arrange()
        {
        }

        protected override async Task Act()
            => Output = await Execute(SellableItemEndpoints.UpdatePrice, TestId, InvalidPayload);

        [Test]
        public void It_should_return_bad_request() => Output.ShouldBeOfType<BadRequest<string>>();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_not_call_service()
        {
            MockOf<ISellableItemCommandService>()
                .DidNotReceive()
                .UpdatePriceAsync(
                    Arg.Any<Guid>(),
                    Arg.Any<decimal>(),
                    Arg.Any<CancellationToken>());
        }
    }

    public class When_updating_to_zero : UpdatePrice_Tests
    {
        private static readonly UpdatePricePayload ZeroPricePayload = new() { BasePrice = 0m };

        protected override void Arrange()
        {
            MockOf<ISellableItemCommandService>()
                .UpdatePriceAsync(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>())
                .Returns(new TestSellableItemModel
                {
                    SellableItemId = TestId,
                    Sku = "TEST_SKU_123",
                    Name = "Test Item",
                    BasePrice = 0m,
                    ItemType = "Physical",
                    IsAvailable = true,
                    Version = 2
                });
        }

        protected override async Task Act()
            => Output = await Execute(SellableItemEndpoints.UpdatePrice, TestId, ZeroPricePayload);

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<SellableItemModel>>();
    }

    public class When_service_throws_exception : UpdatePrice_Tests
    {
        protected override void Arrange()
        {
            MockOf<ISellableItemCommandService>()
                .UpdatePriceAsync(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>())
                .Throws(new TestSellableItemException("Test error"));
        }

        protected override async Task Act()
            => Output = await Execute(SellableItemEndpoints.UpdatePrice, TestId, ValidPayload);

        [Test]
        public void It_should_throw() => LastException.ShouldBeOfType<TestSellableItemException>();

        [Test]
        public void It_should_have_correct_message() => LastException!.Message.ShouldBe("Test error");
    }
}
