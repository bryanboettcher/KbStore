namespace KbStore.ApiService.Tests.Api.Endpoints.Catalog.Inventory;

using ApiService.Endpoints.Catalog;
using KbStore.ApiService.Tests.Api.Endpoints.Catalog;
using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Abstractions.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shouldly;


public abstract class Release_Tests : InventoryEndpoints_Tests
{
    protected static readonly Guid TestId = Guid.NewGuid();

    public class When_releasing_successfully : Release_Tests
    {
        protected override void Arrange()
        {
            MockOf<IInventoryCommandService>()
                .ReleaseAsync(TestId, Arg.Any<CancellationToken>())
                .Returns(new TestInventoryModel
                {
                    InventoryId = TestId,
                    PartNumber = "TEST_PART",
                    Description = "Test Description",
                    StockQuantity = 100,
                    Status = InventoryStatus.Held
                });
        }
        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.Release, TestId);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<InventoryModel>>();
    }

    public class When_service_throws_exception : Release_Tests
    {
        protected override void Arrange()
        {
            MockOf<IInventoryCommandService>()
                .ReleaseAsync(TestId, Arg.Any<CancellationToken>())
                .Throws(new TestInventoryException("Test error"));
        }

        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.Release, TestId);

        [Test]
        public void It_should_throw() => LastException.ShouldBeOfType<TestInventoryException>();

        [Test]
        public void It_should_have_correct_message() => LastException!.Message.ShouldBe("Test error");
    }
}