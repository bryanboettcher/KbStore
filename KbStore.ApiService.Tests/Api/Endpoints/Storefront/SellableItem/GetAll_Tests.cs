namespace KbStore.ApiService.Tests.Api.Endpoints.Storefront.SellableItem;

using KbStore.ApiService.Endpoints.Storefront;
using KbStore.Storefront.Abstractions.Contracts;
using KbStore.Storefront.Abstractions.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shouldly;


public abstract class GetAll_Tests : SellableItemEndpoints_Tests
{
    public class When_getting_all_successfully : GetAll_Tests
    {
        protected override void Arrange()
        {
            MockOf<ISellableItemQueryService>()
                .GetAllAsync(Arg.Any<CancellationToken>())
                .Returns(new List<SellableItemModel>
                {
                    new TestSellableItemModel
                    {
                        SellableItemId = Guid.NewGuid(),
                        Sku = "SKU-001",
                        Name = "Item 1",
                        BasePrice = 9.99m,
                        ItemType = "Physical",
                        IsAvailable = true,
                        Version = 1
                    },
                    new TestSellableItemModel
                    {
                        SellableItemId = Guid.NewGuid(),
                        Sku = "SKU-002",
                        Name = "Item 2",
                        BasePrice = 19.99m,
                        ItemType = "Digital",
                        IsAvailable = true,
                        Version = 1
                    }
                });
        }

        protected override async Task Act()
            => Output = await Execute(SellableItemEndpoints.GetAll);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<List<SellableItemModel>>>();
    }

    public class When_getting_all_returns_empty_list : GetAll_Tests
    {
        protected override void Arrange()
        {
            MockOf<ISellableItemQueryService>()
                .GetAllAsync(Arg.Any<CancellationToken>())
                .Returns(new List<SellableItemModel>());
        }

        protected override async Task Act()
            => Output = await Execute(SellableItemEndpoints.GetAll);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<List<SellableItemModel>>>();
    }

    public class When_service_throws_exception : GetAll_Tests
    {
        protected override void Arrange()
        {
            MockOf<ISellableItemQueryService>()
                .GetAllAsync(Arg.Any<CancellationToken>())
                .Throws(new TestSellableItemException("Test error"));
        }

        protected override async Task Act()
            => Output = await Execute(SellableItemEndpoints.GetAll);

        [Test]
        public void It_should_throw() => LastException.ShouldBeOfType<TestSellableItemException>();

        [Test]
        public void It_should_have_correct_message() => LastException!.Message.ShouldBe("Test error");
    }
}
