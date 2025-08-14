namespace KbStore.ApiService.Tests.Api.Endpoints.Catalog.Product;

using ApiService.Endpoints.Catalog;
using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Abstractions.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shouldly;


public abstract class UpdateStockThreshold_Tests : ProductEndpoints_Tests
{
    protected static readonly Guid TestId = Guid.NewGuid();
    protected static readonly int? ValidThreshold = 25;

    public class When_updating_stock_threshold_successfully : UpdateStockThreshold_Tests
    {
        protected override void Arrange()
        {
            MockOf<IProductCommandService>()
                .UpdateStockThresholdAsync(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
                .Returns(new TestProductModel
                {
                    ProductId = TestId,
                    Sku = "TEST_SKU_123",
                    Name = "Test Product",
                    StockThreshold = ValidThreshold,
                    IsStocked = true,
                    IsEnabled = true,
                    IsAvailable = true
                });
        }

        protected override async Task Act()
            => Output = await Execute(ProductEndpoints.UpdateStockThreshold, TestId, ValidThreshold);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<ProductModel>>();
    }

    public class When_service_throws_exception : UpdateStockThreshold_Tests
    {
        protected override void Arrange()
        {
            MockOf<IProductCommandService>()
                .UpdateStockThresholdAsync(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
                .Throws(new TestProductException("Test error"));
        }

        protected override async Task Act()
            => Output = await Execute(ProductEndpoints.UpdateStockThreshold, TestId, ValidThreshold);

        [Test]
        public void It_should_throw() => LastException.ShouldBeOfType<TestProductException>();

        [Test]
        public void It_should_have_correct_message() => LastException!.Message.ShouldBe("Test error");
    }
}