namespace KbStore.Catalog.Tests.Inventory;

using Abstractions.Contracts;
using Abstractions.Services;
using KbStore.Abstractions;
using NUnit.Framework;
using Services;
using Shouldly;


[Ignore("Not implemented yet")]
public abstract class GetAll_Tests : InventoryService_Tests<DbContextInventoryQueryService>
{
    protected PaginatedResponse<InventoryModel>? Result;
    protected PaginatedQuery? Query;

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