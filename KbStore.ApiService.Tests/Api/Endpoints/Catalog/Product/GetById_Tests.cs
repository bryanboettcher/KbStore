namespace KbStore.ApiService.Tests.Api.Endpoints.Catalog.Product;

using KbStore.ApiService.Endpoints.Catalog;
using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Abstractions.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shouldly;


public abstract class GetById_Tests : ProductEndpoints_Tests
{
    protected static readonly Guid TestId = Guid.NewGuid();

    public class When_getting_successfully : GetById_Tests
    {
        protected override void Arrange()
        {
            MockOf<IProductCommandService>()
                .GetAsync(TestId, Arg.Any<CancellationToken>())
                .Returns(new TestProductModel
                {
                    ProductId = TestId,
                    Sku = "TEST_SKU_123",
                    Name = "Test Product",
                    Dimensions = new ProductDimensions { Width = 10, Height = 5, Length = 15, Weight = 2.5m },
                    InventoryId = Guid.NewGuid(),
                    StockThreshold = 10,
                    LeadTime = TimeSpan.FromDays(7),
                    IsStocked = true,
                    IsEnabled = true,
                    IsAvailable = true
                });
        }
        protected override async Task Act()
            => Output = await Execute(ProductEndpoints.GetById, TestId);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<ProductModel>>();
    }

    public class When_service_throws_exception : GetById_Tests
    {
        protected override void Arrange()
        {
            MockOf<IProductCommandService>()
                .GetAsync(TestId, Arg.Any<CancellationToken>())
            .Throws(new TestProductException("Test error"));
        }

        protected override async Task Act()
            => Output = await Execute(ProductEndpoints.GetById, TestId);

        [Test]
        public void It_should_throw() => LastException.ShouldBeOfType<TestProductException>();

        [Test]
        public void It_should_have_correct_message() => LastException!.Message.ShouldBe("Test error");
    }
}