namespace KbStore.Catalog.Tests.Products;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using NUnit.Framework;
using Services;
using Shouldly;


[Category("Products")]
[Category("Integration")]
public abstract class GetById_Tests : ProductService_Tests<MassTransitProductCommandService>
{
    protected ProductModel? Result;

    protected override void Arrange() { }

    protected override async Task Act()
    {
        await CreateExistingProduct();

        Result ??= await Subject.GetAsync(TestId);
    }

    public class When_getting_successfully : GetById_Tests
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
        public void It_should_have_correct_sku()
            => Result!.Sku.ShouldBe("TEST_SKU_123");

        [Test]
        public void It_should_have_correct_name()
            => Result!.Name.ShouldBe("Test Product");

        [Test]
        public void It_should_have_correct_dimensions()
        {
            Result!.Dimensions.ShouldNotBeNull();
            Result!.Dimensions!.Value.Width.ShouldBe(10m);
            Result!.Dimensions!.Value.Height.ShouldBe(5m);
            Result!.Dimensions!.Value.Length.ShouldBe(15m);
            Result!.Dimensions!.Value.Weight.ShouldBe(2.5m);
        }

        [Test]
        public void It_should_have_correct_stock_threshold()
            => Result!.StockThreshold.ShouldBe(10);

        [Test]
        public void It_should_have_enabled_status()
            => Result!.IsEnabled.ShouldBeTrue();

        [Test]
        public void It_should_be_available()
            => Result!.IsAvailable.ShouldBeTrue();

        [Test]
        public void It_should_have_valid_created_on_timestamp()
            => Result!.CreatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));

        [Test]
        public void It_should_have_valid_updated_on_timestamp()
            => Result!.UpdatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));
    }

    public class When_product_is_missing : GetById_Tests
    {
        protected override void Arrange()
        {
            base.Arrange();
            TestId = Guid.NewGuid();
        }

        protected override async Task Act()
        {
            // Don't create product - use random ID
            Result ??= await Subject.GetAsync(TestId);
        }

        [Test]
        public void It_should_throw_not_found_exception()
            => LastException.ShouldBeOfType<ProductNotFoundException>();

        [Test]
        public void It_should_have_correct_error_message()
            => LastException!.Message.ShouldContain("not found");
    }
}