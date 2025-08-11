namespace KbStore.ApiService.Tests.Api.Endpoints.Catalog.Product;

using KbStore.Abstractions;
using KbStore.ApiService.Endpoints.Catalog;
using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Abstractions.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shouldly;


public abstract class GetAll_Tests : ProductEndpoints_Tests
{
    protected static readonly ProductPaginatedQuery ValidQuery = new() { Page = 0, Size = 25 };

    public class When_getting_all_successfully : GetAll_Tests
    {
        protected override void Arrange()
        {
            MockOf<IProductQueryService>()
                .SearchAsync(Arg.Any<ProductPaginatedQuery>(), Arg.Any<CancellationToken>())
                .Returns(new PaginatedResponse<ProductModel>
                {
                    TotalItems = 1,
                    Page = 0,
                    Size = 25,
                    Results = Enumerable.Empty<ProductModel>().ToAsyncEnumerable()
                });
        }
        protected override async Task Act()
            => Output = await Execute(ProductEndpoints.GetAll, ValidQuery);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();
        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<PaginatedResponse<ProductModel>>>();
    }

    public class When_service_throws_exception : GetAll_Tests
    {
        protected override void Arrange()
        {
            MockOf<IProductQueryService>()
                .SearchAsync(Arg.Any<ProductPaginatedQuery>(), Arg.Any<CancellationToken>())
            .Throws(new TestProductException("Test error"));
        }

        protected override async Task Act()
            => Output = await Execute(ProductEndpoints.GetAll, ValidQuery);

        [Test]
        public void It_should_throw() => LastException.ShouldBeOfType<TestProductException>();

        [Test]
        public void It_should_have_correct_message() => LastException!.Message.ShouldBe("Test error");
    }
}



#pragma warning disable CS8618