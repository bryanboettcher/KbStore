namespace KbStore.ApiService.Tests.Api.Endpoints.Catalog;

using ApiService.Endpoints.Catalog;
using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Abstractions.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shouldly;


public abstract class UpdateDescription_Tests : InventoryEndpoints_Tests
{
    protected static readonly Guid TestId = Guid.NewGuid();
    protected static readonly string ValidDescription = "Updated Description";
    protected static readonly string? InvalidDescription = null;

    public class When_updating_successfully : UpdateDescription_Tests
    {
        protected override void Arrange()
        {
            MockOf<IInventoryCommandService>()
                .UpdateDescriptionAsync(TestId, ValidDescription, Arg.Any<CancellationToken>())
                .Returns(new TestInventoryModel
                {
                    InventoryId = TestId,
                    PartNumber = "TEST_PART",
                    Description = ValidDescription,
                    StockQuantity = 100,
                    Status = InventoryStatus.Available
                });
        }
        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.UpdateDescription, TestId, ValidDescription);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<InventoryModel>>();
    }

    public class When_description_is_invalid : UpdateDescription_Tests
    {
        protected override void Arrange()
        {
        }

        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.UpdateDescription, TestId, InvalidDescription);

        [Test]
        public void It_should_return_bad_request() => Output.ShouldBeOfType<BadRequest<string>>();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_not_call_service()
        {
            MockOf<IInventoryCommandService>()
                .DidNotReceive()
                .UpdateDescriptionAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
    }

    public class When_service_throws_exception : UpdateDescription_Tests
    {
        protected override void Arrange()
        {
            MockOf<IInventoryCommandService>()
                .UpdateDescriptionAsync(TestId, ValidDescription, Arg.Any<CancellationToken>())
                .Throws(new TestInventoryException("Test error"));
        }

        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.UpdateDescription, TestId, ValidDescription);

        [Test]
        public void It_should_throw() => LastException.ShouldBeOfType<TestInventoryException>();

        [Test]
        public void It_should_have_correct_message() => LastException?.Message.ShouldBe("Test error");
    }
}