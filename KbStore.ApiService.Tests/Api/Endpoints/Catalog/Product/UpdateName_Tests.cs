namespace KbStore.ApiService.Tests.Api.Endpoints.Catalog.Product;

using KbStore.ApiService.Endpoints.Catalog;
using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Abstractions.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shouldly;


public abstract class UpdateName_Tests : ProductEndpoints_Tests
{
    protected static readonly Guid TestId = Guid.NewGuid();
    protected static readonly string? ValidName = "Updated Product Name";

    public class When_updating_successfully : UpdateName_Tests
    {
        protected override void Arrange()
        {
            MockOf<IProductCommandService>()
                .UpdateNameAsync(TestId, ValidName, Arg.Any<CancellationToken>())
                .Returns(new TestProductModel
                {
                    ProductId = TestId,
                    Sku = "TEST_SKU_123",
                    Name = ValidName,
                    IsStocked = true,
                    IsEnabled = true,
                    IsAvailable = true
                });
        }
        protected override async Task Act()
            => Output = await Execute(ProductEndpoints.UpdateName, TestId, ValidName);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<ProductModel>>();
    }

    public class When_service_throws_exception : UpdateName_Tests
    {
        protected override void Arrange()
        {
            MockOf<IProductCommandService>()
                .UpdateNameAsync(TestId, ValidName, Arg.Any<CancellationToken>())
            .Throws(new TestProductException("Test error"));
        }

        protected override async Task Act()
            => Output = await Execute(ProductEndpoints.UpdateName, TestId, ValidName);

        [Test]
        public void It_should_throw() => LastException.ShouldBeOfType<TestProductException>();

        [Test]
        public void It_should_have_correct_message() => LastException!.Message.ShouldBe("Test error");
    }
}