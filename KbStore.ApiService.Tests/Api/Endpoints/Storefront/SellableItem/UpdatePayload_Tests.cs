namespace KbStore.ApiService.Tests.Api.Endpoints.Storefront.SellableItem;

using KbStore.ApiService.Endpoints.Storefront;
using KbStore.Storefront.Abstractions.Contracts;
using KbStore.Storefront.Abstractions.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shouldly;


public abstract class UpdatePayload_Tests : SellableItemEndpoints_Tests
{
    protected static readonly Guid TestId = Guid.NewGuid();
    protected static readonly UpdatePayloadPayload ValidPayload = new()
    {
        Payload = new Dictionary<string, object?> { { "color", "red" }, { "size", "large" } }
    };
    protected static readonly UpdatePayloadPayload InvalidPayload = new() { Payload = null };

    public class When_updating_successfully : UpdatePayload_Tests
    {
        protected override void Arrange()
        {
            MockOf<ISellableItemCommandService>()
                .UpdatePayloadAsync(
                    Arg.Any<Guid>(),
                    Arg.Any<IReadOnlyDictionary<string, object?>>(),
                    Arg.Any<CancellationToken>())
                .Returns(new TestSellableItemModel
                {
                    SellableItemId = TestId,
                    Sku = "TEST_SKU_123",
                    Name = "Test Item",
                    BasePrice = 19.99m,
                    ItemType = "Physical",
                    Payload = ValidPayload.Payload!,
                    IsAvailable = true,
                    Version = 2
                });
        }

        protected override async Task Act()
            => Output = await Execute(SellableItemEndpoints.UpdatePayload, TestId, ValidPayload);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<SellableItemModel>>();
    }

    public class When_payload_is_null : UpdatePayload_Tests
    {
        protected override void Arrange()
        {
        }

        protected override async Task Act()
            => Output = await Execute(SellableItemEndpoints.UpdatePayload, TestId, InvalidPayload);

        [Test]
        public void It_should_return_bad_request() => Output.ShouldBeOfType<BadRequest<string>>();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_not_call_service()
        {
            MockOf<ISellableItemCommandService>()
                .DidNotReceive()
                .UpdatePayloadAsync(
                    Arg.Any<Guid>(),
                    Arg.Any<IReadOnlyDictionary<string, object?>>(),
                    Arg.Any<CancellationToken>());
        }
    }

    public class When_updating_to_empty_payload : UpdatePayload_Tests
    {
        private static readonly UpdatePayloadPayload EmptyPayload = new()
        {
            Payload = new Dictionary<string, object?>()
        };

        protected override void Arrange()
        {
            MockOf<ISellableItemCommandService>()
                .UpdatePayloadAsync(
                    Arg.Any<Guid>(),
                    Arg.Any<IReadOnlyDictionary<string, object?>>(),
                    Arg.Any<CancellationToken>())
                .Returns(new TestSellableItemModel
                {
                    SellableItemId = TestId,
                    Sku = "TEST_SKU_123",
                    Name = "Test Item",
                    BasePrice = 19.99m,
                    ItemType = "Physical",
                    Payload = new Dictionary<string, object?>(),
                    IsAvailable = true,
                    Version = 2
                });
        }

        protected override async Task Act()
            => Output = await Execute(SellableItemEndpoints.UpdatePayload, TestId, EmptyPayload);

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<SellableItemModel>>();
    }

    public class When_service_throws_exception : UpdatePayload_Tests
    {
        protected override void Arrange()
        {
            MockOf<ISellableItemCommandService>()
                .UpdatePayloadAsync(
                    Arg.Any<Guid>(),
                    Arg.Any<IReadOnlyDictionary<string, object?>>(),
                    Arg.Any<CancellationToken>())
                .Throws(new TestSellableItemException("Test error"));
        }

        protected override async Task Act()
            => Output = await Execute(SellableItemEndpoints.UpdatePayload, TestId, ValidPayload);

        [Test]
        public void It_should_throw() => LastException.ShouldBeOfType<TestSellableItemException>();

        [Test]
        public void It_should_have_correct_message() => LastException!.Message.ShouldBe("Test error");
    }
}
