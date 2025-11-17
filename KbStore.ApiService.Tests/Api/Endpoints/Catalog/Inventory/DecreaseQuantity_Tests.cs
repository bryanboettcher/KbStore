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


public abstract class DecreaseQuantity_Tests : InventoryEndpoints_Tests
{
    protected static readonly Guid TestId = Guid.NewGuid();
    protected static readonly int ValidQuantity = 5;
    protected static readonly int InvalidQuantity = -1;

    public class When_decreasing_successfully : DecreaseQuantity_Tests
    {
        protected override void Arrange()
        {
            MockOf<IInventoryCommandService>()
                .DecreaseQuantityAsync(TestId, ValidQuantity, Arg.Any<CancellationToken>())
                .Returns(new TestInventoryModel
                {
                    InventoryId = TestId,
                    PartNumber = "TEST_PART",
                    Description = "Test Description",
                    StockQuantity = 95,
                    Status = InventoryStatus.Available
                });
        }
        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.DecreaseQuantity, TestId, ValidQuantity);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<InventoryModel>>();
    }

    public class When_quantity_is_invalid : DecreaseQuantity_Tests
    {
        protected override void Arrange()
        {
        }
        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.DecreaseQuantity, TestId, InvalidQuantity);

        [Test]
        public void It_should_return_bad_request() => Output.ShouldBeOfType<BadRequest<string>>();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_not_call_service()
        {
            MockOf<IInventoryCommandService>()
                .DidNotReceive()
                .DecreaseQuantityAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        }
    }

    public class When_service_throws_exception : DecreaseQuantity_Tests
    {
        protected override void Arrange()
        {
            MockOf<IInventoryCommandService>()
                .DecreaseQuantityAsync(TestId, ValidQuantity, Arg.Any<CancellationToken>())
                .Throws(new TestInventoryException("Test error"));
        }

        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.DecreaseQuantity, TestId, ValidQuantity);

        [Test]
        public void It_should_throw() => LastException.ShouldBeOfType<TestInventoryException>();

        [Test]
        public void It_should_have_correct_message() => LastException!.Message.ShouldBe("Test error");
    }
}