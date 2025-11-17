namespace KbStore.ApiService.Tests.Api.Endpoints.Storefront.SellableItem;

using KbStore.ApiService.Endpoints.Storefront;
using KbStore.Storefront.Abstractions.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shouldly;


public abstract class Delete_Tests : SellableItemEndpoints_Tests
{
    protected static readonly Guid TestId = Guid.NewGuid();

    public class When_deleting_successfully : Delete_Tests
    {
        protected override void Arrange()
        {
            MockOf<ISellableItemCommandService>()
                .DeleteAsync(TestId, Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
        }

        protected override async Task Act()
            => Output = await Execute(SellableItemEndpoints.Delete, TestId);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<NoContent>();
    }

    public class When_service_throws_exception : Delete_Tests
    {
        protected override void Arrange()
        {
            MockOf<ISellableItemCommandService>()
                .DeleteAsync(TestId, Arg.Any<CancellationToken>())
                .Throws(new TestSellableItemException("Test error"));
        }

        protected override async Task Act()
            => Output = await Execute(SellableItemEndpoints.Delete, TestId);

        [Test]
        public void It_should_throw() => LastException.ShouldBeOfType<TestSellableItemException>();

        [Test]
        public void It_should_have_correct_message() => LastException!.Message.ShouldBe("Test error");
    }
}
