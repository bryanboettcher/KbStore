namespace KbStore.ApiService.Tests.Api.Endpoints.Storefront.SellableItem;

using KbStore.ApiService.Endpoints.Storefront;
using KbStore.Storefront.Abstractions.Contracts;
using KbStore.Storefront.Abstractions.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shouldly;


public abstract class UpdateName_Tests : SellableItemEndpoints_Tests
{
    protected static readonly Guid TestId = Guid.NewGuid();
    protected static readonly UpdateNamePayload ValidPayload = new() { Name = "Updated Item Name" };
    protected static readonly UpdateNamePayload InvalidPayload = new() { Name = "" };

    public class When_updating_successfully : UpdateName_Tests
    {
        protected override void Arrange()
        {
            MockOf<ISellableItemCommandService>()
                .UpdateNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new TestSellableItemModel
                {
                    SellableItemId = TestId,
                    Sku = "TEST_SKU_123",
                    Name = ValidPayload.Name!,
                    BasePrice = 19.99m,
                    ItemType = "Physical",
                    IsAvailable = true,
                    Version = 2
                });
        }

        protected override async Task Act()
            => Output = await Execute(SellableItemEndpoints.UpdateName, TestId, ValidPayload);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<SellableItemModel>>();
    }

    public class When_payload_is_invalid : UpdateName_Tests
    {
        protected override void Arrange()
        {
        }

        protected override async Task Act()
            => Output = await Execute(SellableItemEndpoints.UpdateName, TestId, InvalidPayload);

        [Test]
        public void It_should_return_bad_request() => Output.ShouldBeOfType<BadRequest<string>>();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_not_call_service()
        {
            MockOf<ISellableItemCommandService>()
                .DidNotReceive()
                .UpdateNameAsync(
                    Arg.Any<Guid>(),
                    Arg.Any<string>(),
                    Arg.Any<CancellationToken>());
        }
    }

    public class When_service_throws_exception : UpdateName_Tests
    {
        protected override void Arrange()
        {
            MockOf<ISellableItemCommandService>()
                .UpdateNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Throws(new TestSellableItemException("Test error"));
        }

        protected override async Task Act()
            => Output = await Execute(SellableItemEndpoints.UpdateName, TestId, ValidPayload);

        [Test]
        public void It_should_throw() => LastException.ShouldBeOfType<TestSellableItemException>();

        [Test]
        public void It_should_have_correct_message() => LastException!.Message.ShouldBe("Test error");
    }
}
