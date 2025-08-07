namespace KbStore.ApiService.Tests.Api.Endpoints.Catalog;

using ApiService.Endpoints.Catalog;
using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Abstractions.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shouldly;


public abstract class Create_Tests : InventoryEndpoints_Tests
{
    protected static readonly CreateInventoryPayload ValidPayload = new()
    {
        PartNumber = "TEST_PART",
        Description = "Test Description",
        StockQuantity = 50
    };

    protected static readonly CreateInventoryPayload InvalidPayload = new()
    {
        PartNumber = "", // Invalid
        Description = "Test Description",
        StockQuantity = 50
    };

    public class When_creating_successfully : Create_Tests
    {
        protected override void Arrange()
        {
            MockOf<IInventoryCommandService>()
                .CreateAsync(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<int>(),
                    Arg.Any<CancellationToken>())
                .Returns(new TestInventoryModel
                {
                    InventoryId = Guid.NewGuid(),
                    PartNumber = ValidPayload.PartNumber!,
                    Description = ValidPayload.Description!,
                    StockQuantity = ValidPayload.StockQuantity,
                    Status = InventoryStatus.Available
                });
        }
        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.Create, ValidPayload);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<InventoryModel>>();
    }

    public class When_payload_is_invalid : Create_Tests
    {
        protected override void Arrange()
        {
        }
        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.Create, InvalidPayload);

        [Test]
        public void It_should_return_bad_request() => Output.ShouldBeOfType<BadRequest>();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();
    }

    public class When_service_throws_exception : Create_Tests
    {
        protected override void Arrange()
        {
            MockOf<IInventoryCommandService>()
                .CreateAsync(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<int>(),
                    Arg.Any<CancellationToken>())
                .Throws(new TestInventoryException("Test error"));
        }

        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.Create, ValidPayload);

        [Test]
        public void It_should_throw() => LastException.ShouldBeOfType<TestInventoryException>();

        [Test]
        public void It_should_have_correct_message() => LastException!.Message.ShouldBe("Test error");
    }
}