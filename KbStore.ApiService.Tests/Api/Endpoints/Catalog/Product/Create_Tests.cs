namespace KbStore.ApiService.Tests.Api.Endpoints.Catalog.Product;

using KbStore.ApiService.Endpoints.Catalog;
using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Abstractions.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shouldly;


public abstract class Create_Tests : ProductEndpoints_Tests
{
    protected static readonly CreateProductPayload ValidPayload = new()
    {
        Sku = "TEST_SKU_123",
        Name = "Test Product",
        Dimensions = new ProductDimensions { Width = 10, Height = 5, Length = 15, Weight = 2.5m },
        InventoryItemId = Guid.NewGuid(),
        StockThreshold = 10,
        LeadTime = TimeSpan.FromDays(7)
    };

    protected static readonly CreateProductPayload InvalidPayload = new()
    {
        Sku = "", // Invalid
        Name = "Test Product",
        StockThreshold = 10,
        LeadTime = TimeSpan.FromDays(7)
    };

    public class When_creating_successfully : Create_Tests
    {
        protected override void Arrange()
        {
            MockOf<IProductCommandService>()
                .CreateAsync(
                    Arg.Any<string>(),
                    Arg.Any<string?>(),
                    Arg.Any<ProductDimensions?>(),
                    Arg.Any<Guid?>(),
                    Arg.Any<int?>(),
                    Arg.Any<TimeSpan?>(),
                    Arg.Any<CancellationToken>())
                .Returns(new TestProductModel
                {
                    ProductId = Guid.NewGuid(),
                    Sku = ValidPayload.Sku!,
                    Name = ValidPayload.Name,
                    Dimensions = ValidPayload.Dimensions,
                    InventoryId = ValidPayload.InventoryItemId,
                    StockThreshold = ValidPayload.StockThreshold,
                    LeadTime = ValidPayload.LeadTime,
                    IsStocked = true,
                    IsEnabled = true,
                    IsAvailable = true
                });
        }
        protected override async Task Act()
            => Output = await Execute(ProductEndpoints.Create, ValidPayload);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<ProductModel>>();
    }

    public class When_payload_is_invalid : Create_Tests
    {
        protected override void Arrange()
        {
        }
        protected override async Task Act()
            => Output = await Execute(ProductEndpoints.Create, InvalidPayload);

        [Test]
        public void It_should_return_bad_request() => Output.ShouldBeOfType<BadRequest>();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_not_call_service()
        {
            MockOf<IProductCommandService>()
                .DidNotReceive()
                .CreateAsync(
                    Arg.Any<string>(),
                    Arg.Any<string?>(),
                    Arg.Any<ProductDimensions?>(),
                    Arg.Any<Guid?>(),
                    Arg.Any<int?>(),
                    Arg.Any<TimeSpan?>(),
                    Arg.Any<CancellationToken>());
        }
    }

    public class When_service_throws_exception : Create_Tests
    {
        protected override void Arrange()
        {
            MockOf<IProductCommandService>()
                .CreateAsync(
                    Arg.Any<string>(),
                    Arg.Any<string?>(),
                    Arg.Any<ProductDimensions?>(),
                    Arg.Any<Guid?>(),
                    Arg.Any<int?>(),
                    Arg.Any<TimeSpan?>(),
                    Arg.Any<CancellationToken>())
            .Throws(new TestProductException("Test error"));
        }

        protected override async Task Act()
            => Output = await Execute(ProductEndpoints.Create, ValidPayload);

        [Test]
        public void It_should_throw() => LastException.ShouldBeOfType<TestProductException>();

        [Test]
        public void It_should_have_correct_message() => LastException!.Message.ShouldBe("Test error");
    }
}