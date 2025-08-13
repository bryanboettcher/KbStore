namespace KbStore.Catalog.Tests.Products;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using NUnit.Framework;
using Services;
using Shouldly;


[Category("Products")]
[Category("Integration")]
public abstract class UpdateStockThreshold_Tests : Catalog_Tests<MassTransitProductCommandService>
{
    protected ProductModel? Result;

    protected override void Arrange()
    {
        TestStockThreshold = 25;
    }

    protected override async Task Act()
    {
        await CreateExistingProduct();

        Now = Later;
        Result ??= await Subject.UpdateStockThresholdAsync(TestId, TestStockThreshold);
    }

    public class When_updating_successfully : UpdateStockThreshold_Tests
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
        public void It_should_have_correct_threshold()
            => Result!.StockThreshold.ShouldBe(25);

        [Test]
        public void It_should_have_updated_on_timestamp()
            => Result!.UpdatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));

        [Test]
        public void It_should_have_unchanged_created_on_timestamp()
            => Result!.CreatedOn.ShouldBe(InitialCreatedOn);

        [Test]
        public async Task It_should_publish_product_stock_threshold_updated_event()
            => (await Harness.Published.Any<ProductStockThresholdUpdated>()).ShouldBeTrue();
    }

    public class When_threshold_is_negative : UpdateStockThreshold_Tests
    {
        protected override void Arrange()
        {
            base.Arrange();
            TestStockThreshold = -5;
        }

        [Test]
        public void It_should_throw_validation_exception()
            => LastException.ShouldBeOfType<ProductValidationException>();

        [Test]
        public void It_should_have_correct_error_message()
            => LastException!.Message.ShouldContain("threshold");

        [Test]
        public async Task It_should_not_publish_product_stock_threshold_updated_event()
            => (await Harness.Published.Any<ProductStockThresholdUpdated>()).ShouldBeFalse();
    }

    public class When_threshold_causes_availability_change : UpdateStockThreshold_Tests
    {
        protected override void Arrange()
        {
            base.Arrange();
            TestInventoryId = Guid.NewGuid();
            TestStockThreshold = 100; // Higher than current stock
        }

        protected override async Task Act()
        {
            await CreateExistingProduct(inventoryItemId: TestInventoryId, stockThreshold: 10);

            // Simulate current stock of 50
            await PublishInventoryEvent<InventoryQuantityChanged>(new
            {
                InventoryId = TestInventoryId,
                StockQuantity = 50,
                Timestamp = Now
            });

            Now = Later;
            Result ??= await Subject.UpdateStockThresholdAsync(TestId, TestStockThreshold);
        }

        [Test]
        public void It_should_affect_availability()
            => Result!.IsAvailable.ShouldBeFalse();

        [Test]
        public async Task It_should_publish_availability_changed_event()
            => (await Harness.Published.Any<ProductAvailabilityChanged>()).ShouldBeTrue();
    }
}