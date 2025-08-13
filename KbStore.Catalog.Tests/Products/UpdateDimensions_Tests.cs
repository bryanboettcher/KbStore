namespace KbStore.Catalog.Tests.Products;

using Abstractions.Contracts;
using NUnit.Framework;
using Services;
using Shouldly;


[Category("Products")]
[Category("Integration")]
public abstract class UpdateDimensions_Tests : Catalog_Tests<MassTransitProductCommandService>
{
    protected ProductModel? Result;

    protected override void Arrange()
    {
        TestDimensions = new ProductDimensions { Width = 20m, Height = 10m, Length = 30m, Weight = 5m };
    }

    protected override async Task Act()
    {
        await CreateExistingProduct();

        Now = Later;
        Result ??= await Subject.UpdateDimensionsAsync(TestId, TestDimensions);
    }

    public class When_updating_successfully : UpdateDimensions_Tests
    {
        [Test]
        public void It_should_not_throw()
            => LastException.ShouldBeNull();

        [Test]
        public void It_should_return_something()
            => Result.ShouldNotBeNull();

        [Test]
        public void It_should_be_successful()
            => Result!.ProductId.ShouldBe(TestId);

        [Test]
        public void It_should_have_correct_dimensions()
        {
            Result!.Dimensions.ShouldNotBeNull();
            Result!.Dimensions!.Value.Width.ShouldBe(20m);
            Result!.Dimensions!.Value.Height.ShouldBe(10m);
            Result!.Dimensions!.Value.Length.ShouldBe(30m);
            Result!.Dimensions!.Value.Weight.ShouldBe(5m);
        }

        [Test]
        public void It_should_have_updated_on_timestamp()
            => Result!.UpdatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));

        [Test]
        public void It_should_have_unchanged_created_on_timestamp()
            => Result!.CreatedOn.ShouldBe(InitialCreatedOn);

        [Test]
        public async Task It_should_publish_product_dimensions_updated_event()
            => (await Harness.Published.Any<ProductDimensionsUpdated>()).ShouldBeTrue();
    }

    public class When_setting_null_dimensions : UpdateDimensions_Tests
    {
        protected override void Arrange()
        {
            base.Arrange();
            TestDimensions = null;
        }

        [Test]
        public void It_should_not_throw()
            => LastException.ShouldBeNull();

        [Test]
        public void It_should_accept_null_dimensions()
            => Result!.Dimensions.ShouldBeNull();

        [Test]
        public async Task It_should_publish_product_dimensions_updated_event()
            => (await Harness.Published.Any<ProductDimensionsUpdated>()).ShouldBeTrue();
    }
}