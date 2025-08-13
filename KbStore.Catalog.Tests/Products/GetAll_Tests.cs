namespace KbStore.Catalog.Tests.Products;

using Abstractions.Contracts;
using Abstractions.Services;
using KbStore.Abstractions;
using KbStore.Catalog.Tests;
using NUnit.Framework;
using Services;
using Shouldly;


[Category("Products")]
[Category("Integration")]
[Ignore("Not implemented yet")]
public abstract class GetAll_Tests : ProductService_Tests<DbContextProductQueryService>
{
    protected PaginatedResponse<ProductModel>? Result = null;
    protected ProductPaginatedQuery? Query = null;

    protected override void Arrange()
    {
        Query = new();
    }

    protected override async Task Act()
    {
        Result ??= await Subject.SearchAsync(Query!);
    }

    public class When_getting_all_successfully : GetAll_Tests
    {
        [Test]
        public void It_should_not_throw()
            => LastException.ShouldBeNull();

        [Test]
        public void It_should_return_something()
            => Result.ShouldNotBeNull();

        [Test]
        public async Task It_should_be_successful()
            => (await Result!.Results.ToListAsync()).ShouldNotBeEmpty();
    }
}