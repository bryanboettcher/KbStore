namespace KbStore.ApiService.Tests.Api.Endpoints.Catalog.Product;

using ApiService.Endpoints.Catalog;
using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Abstractions.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shouldly;


public abstract class UpdateQuantity_Tests : ProductEndpoints_Tests
{
    protected static readonly Guid TestId = Guid.NewGuid();
    protected static readonly int ValidQuantity = 3;

    public class When_updating_quantity_successfully : UpdateQuantity_Tests
    {
        protected override void Arrange()
        {
            MockOf<IProductCommandService>()
                .UpdateQuantityAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(new TestProductModel
                {
                    ProductId = TestId,
                    Sku = "TEST_SKU_123",
                    Name = "Test Product",
                    Quantity = ValidQuantity,
                    IsStocked = true,
                    IsEnabled = true,
                    IsAvailable = true
                });
        }

        protected override async Task Act()
            => Output = await Execute(ProductEndpoints.UpdateQuantity, TestId, ValidQuantity);

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Output.ShouldNotBeNull();
            Output.ShouldBeOfType<Ok<ProductModel>>();
        });
    }

    public class When_service_throws_exception : UpdateQuantity_Tests
    {
        protected override void Arrange()
        {
            MockOf<IProductCommandService>()
                .UpdateQuantityAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Throws(new TestProductException("Test error"));
        }

        protected override async Task Act()
            => Output = await Execute(ProductEndpoints.UpdateQuantity, TestId, ValidQuantity);

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<TestProductException>();
            LastException.Message.ShouldBe("Test error");
        });
    }
}