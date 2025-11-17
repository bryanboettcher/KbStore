namespace KbStore.ApiService.Tests.Api.Endpoints.Storefront.SellableItem;

using KbStore.ApiService.Endpoints.Storefront;
using KbStore.Storefront.Abstractions.Contracts;
using KbStore.Storefront.Abstractions.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shouldly;


public abstract class Create_Tests : SellableItemEndpoints_Tests
{
    protected static readonly CreateSellableItemPayload ValidPayload = new()
    {
        Sku = "TEST_SKU_123",
        Name = "Test Item",
        Description = "Test description",
        BasePrice = 19.99m,
        ItemType = "Physical",
        Payload = new Dictionary<string, object?> { { "weight", 2.5 }, { "dimensions", "10x5x3" } },
        ProductId = Guid.NewGuid()
    };

    protected static readonly CreateSellableItemPayload InvalidPayload = new()
    {
        Sku = "", // Invalid
        Name = "Test Item",
        BasePrice = 19.99m,
        ItemType = "Physical"
    };

    public class When_creating_successfully : Create_Tests
    {
        protected override void Arrange()
        {
            MockOf<ISellableItemCommandService>()
                .CreateAsync(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string?>(),
                    Arg.Any<decimal>(),
                    Arg.Any<string>(),
                    Arg.Any<IReadOnlyDictionary<string, object?>>(),
                    Arg.Any<Guid?>(),
                    Arg.Any<CancellationToken>())
                .Returns(new TestSellableItemModel
                {
                    SellableItemId = Guid.NewGuid(),
                    ProductId = ValidPayload.ProductId,
                    Sku = ValidPayload.Sku!,
                    Name = ValidPayload.Name!,
                    Description = ValidPayload.Description,
                    BasePrice = ValidPayload.BasePrice,
                    ItemType = ValidPayload.ItemType!,
                    Payload = ValidPayload.Payload ?? new Dictionary<string, object?>(),
                    IsAvailable = true,
                    Version = 1
                });
        }

        protected override async Task Act()
            => Output = await Execute(SellableItemEndpoints.Create, ValidPayload);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<SellableItemModel>>();
    }

    public class When_payload_is_invalid : Create_Tests
    {
        protected override void Arrange()
        {
        }

        protected override async Task Act()
            => Output = await Execute(SellableItemEndpoints.Create, InvalidPayload);

        [Test]
        public void It_should_return_bad_request() => Output.ShouldBeOfType<BadRequest>();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_not_call_service()
        {
            MockOf<ISellableItemCommandService>()
                .DidNotReceive()
                .CreateAsync(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string?>(),
                    Arg.Any<decimal>(),
                    Arg.Any<string>(),
                    Arg.Any<IReadOnlyDictionary<string, object?>>(),
                    Arg.Any<Guid?>(),
                    Arg.Any<CancellationToken>());
        }
    }

    public class When_service_throws_exception : Create_Tests
    {
        protected override void Arrange()
        {
            MockOf<ISellableItemCommandService>()
                .CreateAsync(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string?>(),
                    Arg.Any<decimal>(),
                    Arg.Any<string>(),
                    Arg.Any<IReadOnlyDictionary<string, object?>>(),
                    Arg.Any<Guid?>(),
                    Arg.Any<CancellationToken>())
                .Throws(new TestSellableItemException("Test error"));
        }

        protected override async Task Act()
            => Output = await Execute(SellableItemEndpoints.Create, ValidPayload);

        [Test]
        public void It_should_throw() => LastException.ShouldBeOfType<TestSellableItemException>();

        [Test]
        public void It_should_have_correct_message() => LastException!.Message.ShouldBe("Test error");
    }
}
