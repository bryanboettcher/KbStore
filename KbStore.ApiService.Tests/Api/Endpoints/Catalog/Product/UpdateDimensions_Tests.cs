namespace KbStore.ApiService.Tests.Api.Endpoints.Catalog.Product;

using ApiService.Endpoints.Catalog;
using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Abstractions.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shouldly;


public abstract class UpdateDimensions_Tests : ProductEndpoints_Tests
{
    protected static readonly Guid TestId = Guid.NewGuid();
    protected static readonly ProductDimensions? ValidDimensions = new()
    {
        Width = 20,
        Height = 10,
        Length = 30,
        Weight = 5.0m
    };

    public class When_updating_dimensions_successfully : UpdateDimensions_Tests
    {
        protected override void Arrange()
        {
            MockOf<IProductCommandService>()
                .UpdateDimensionsAsync(Arg.Any<Guid>(), Arg.Any<ProductDimensions>(), Arg.Any<CancellationToken>())
                .Returns(new TestProductModel
                {
                    ProductId = TestId,
                    Sku = "TEST_SKU_123",
                    Name = "Test Product",
                    Dimensions = ValidDimensions,
                    IsStocked = true,
                    IsEnabled = true,
                    IsAvailable = true
                });
        }

        protected override async Task Act()
            => Output = await Execute(ProductEndpoints.UpdateDimensions, TestId, ValidDimensions);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<ProductModel>>();
    }

    public class When_service_throws_exception : UpdateDimensions_Tests
    {
        protected override void Arrange()
        {
            MockOf<IProductCommandService>()
                .UpdateDimensionsAsync(Arg.Any<Guid>(), Arg.Any<ProductDimensions?>(), Arg.Any<CancellationToken>())
                .Throws(new TestProductException("Test error"));
        }

        protected override async Task Act()
            => Output = await Execute(ProductEndpoints.UpdateDimensions, TestId, ValidDimensions);

        [Test]
        public void It_should_throw() => LastException.ShouldBeOfType<TestProductException>();

        [Test]
        public void It_should_have_correct_message() => LastException!.Message.ShouldBe("Test error");
    }
}