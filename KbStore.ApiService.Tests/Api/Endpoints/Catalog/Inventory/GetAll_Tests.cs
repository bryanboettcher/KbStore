namespace KbStore.ApiService.Tests.Api.Endpoints.Catalog.Inventory;

using Abstractions;
using ApiService.Endpoints.Catalog;
using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Abstractions.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shouldly;


public abstract class GetAll_Tests : InventoryEndpoints_Tests
{
    protected static readonly PaginatedQuery ValidQuery = new() { Page = 0, Size = 25 };

    public class When_getting_all_successfully : GetAll_Tests
    {
        protected override void Arrange()
        {
            MockOf<IInventoryQueryService>()
                .SearchAsync(Arg.Any<PaginatedQuery>(), Arg.Any<CancellationToken>())
                .Returns(new PaginatedResponse<InventoryModel>
                {
                    TotalItems = 1,
                    Page = 0,
                    Size = 25,
                    Results = Enumerable.Empty<InventoryModel>().ToAsyncEnumerable()
                });
        }
        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.GetAll, ValidQuery);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<PaginatedResponse<InventoryModel>>>();
    }

    public class When_service_throws_exception : GetAll_Tests
    {
        protected override void Arrange()
        {
            MockOf<IInventoryQueryService>()
                .SearchAsync(Arg.Any<PaginatedQuery>(), Arg.Any<CancellationToken>())
                .Throws(new TestInventoryException("Test error"));
        }

        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.GetAll, ValidQuery);

        [Test]
        public void It_should_throw() => LastException.ShouldBeOfType<TestInventoryException>();

        [Test]
        public void It_should_have_correct_message() => LastException!.Message.ShouldBe("Test error");
    }
}