namespace KbStore.Catalog.Tests.Products;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using KbStore.Catalog.Tests;
using NUnit.Framework;
using Services;
using Shouldly;


[Category("Products")]
[Category("Integration")]
public abstract class CreateAsync_Tests : ProductService_Tests<MassTransitProductCommandService>
{
    protected ProductModel? Result = null!;

    protected override void Arrange()
    {
        TestSku = "TEST_SKU_123";
        TestName = "Test Product";
        TestDimensions = new ProductDimensions { Width = 10m, Height = 5m, Length = 15m, Weight = 2.5m };
        TestStockThreshold = 10;
        TestLeadTime = TimeSpan.FromDays(7);
    }

    protected override async Task Act()
    {
        Result ??= await Subject.CreateAsync(TestSku!, TestName, TestDimensions, TestInventoryItemId, TestStockThreshold, TestLeadTime);
    }

    public class When_creating_valid_product_should_succeed : CreateAsync_Tests
    {
        [Test]
        public void It_should_not_throw()
            => LastException.ShouldBeNull();

        [Test]
        public void It_should_return_valid_result()
            => Result.ShouldNotBeNull();

        [Test]
        public void It_should_have_correct_sku()
            => Result!.Sku.ShouldBe("TEST_SKU_123");

        [Test]
        public void It_should_set_the_name()
            => Result!.Name.ShouldBe("Test Product");

        [Test]
        public void It_should_have_enabled_status()
            => Result!.IsEnabled.ShouldBeTrue();

        [Test]
        public void It_should_be_available()
            => Result!.IsAvailable.ShouldBeTrue();

        [Test]
        public void It_should_set_CreatedOn()
            => Result!.CreatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));

        [Test]
        public void It_should_set_UpdatedOn()
            => Result!.UpdatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));

        [Test]
        public async Task It_should_publish_product_created_event()
            => (await Harness!.Published.Any<ProductCreated>()).ShouldBeTrue();
    }

    public class When_creating_with_empty_sku_should_fail : CreateAsync_Tests
    {
        protected override void Arrange()
        {
            base.Arrange();
            TestSku = "";
        }

        [Test]
        public void It_should_throw_validation_exception()
            => LastException.ShouldBeOfType<ProductValidationException>();

        [Test]
        public void It_should_have_correct_error_message()
            => LastException!.Message.ShouldContain("SKU");

        [Test]
        public async Task It_should_not_publish_product_created_event()
            => (await Harness!.Published.Any<ProductCreated>()).ShouldBeFalse();
    }

    public class When_creating_with_null_sku_should_fail : CreateAsync_Tests
    {
        protected override void Arrange()
        {
            base.Arrange();
            TestSku = null!;
        }

        [Test]
        public void It_should_throw_validation_exception()
            => LastException.ShouldBeOfType<ProductValidationException>();

        [Test]
        public void It_should_have_correct_error_message()
            => LastException!.Message.ShouldContain("SKU");

        [Test]
        public async Task It_should_not_publish_product_created_event()
            => (await Harness!.Published.Any<ProductCreated>()).ShouldBeFalse();
    }

    public class When_creating_with_negative_stock_threshold_should_fail : CreateAsync_Tests
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
        public async Task It_should_not_publish_product_created_event()
            => (await Harness!.Published.Any<ProductCreated>()).ShouldBeFalse();
    }

    public class When_creating_with_negative_lead_time_should_fail : CreateAsync_Tests
    {
        protected override void Arrange()
        {
            base.Arrange();
            TestLeadTime = TimeSpan.FromDays(-1);
        }

        [Test]
        public void It_should_throw_validation_exception()
            => LastException.ShouldBeOfType<ProductValidationException>();

        [Test]
        public void It_should_have_correct_error_message()
            => LastException!.Message.ShouldContain("Lead time");

        [Test]
        public async Task It_should_not_publish_product_created_event()
            => (await Harness!.Published.Any<ProductCreated>()).ShouldBeFalse();
    }

    public class When_creating_duplicate_sku : CreateAsync_Tests
    {
        protected override async Task Act()
        {
            // create our initial
            await Subject.CreateAsync(TestSku!, TestName, TestDimensions, TestInventoryItemId, TestStockThreshold, TestLeadTime);

            // create the duplicate
            await base.Act();
        }

        [Test]
        public void It_should_throw_conflict_exception()
            => LastException.ShouldBeOfType<ProductConflictException>();

        [Test]
        public void It_should_mention_duplicate_sku()
            => LastException!.Message.ShouldContain("TEST_SKU_123");

        [Test]
        public void It_should_not_publish_additional_product_created_event()
            => Harness!.Published.Select<ProductCreated>().Count().ShouldBe(1);
    }

    public class When_linked_inventory_is_discontinued : CreateAsync_Tests
    {
        protected override void Arrange()
        {
            base.Arrange();
            TestInventoryItemId = Guid.NewGuid();
        }

        protected override async Task Act()
        {
            await base.Act();

            // Simulate inventory discontinued after creation
            await PublishInventoryEvent<InventoryDiscontinued>(new
            {
                InventoryId = TestInventoryItemId,
                StockQuantity = 0,
                Status = InventoryStatus.Discontinued,
                Timestamp = Later
            });

            // Get updated state
            Result = await Subject.GetAsync(TestId);
        }

        [Test]
        public void It_should_discontinue_product()
            => Result!.IsEnabled.ShouldBeFalse();

        [Test]
        public async Task It_should_publish_product_discontinued_event()
            => (await Harness!.Published.Any<ProductDiscontinued>()).ShouldBeTrue();
    }
}