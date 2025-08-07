using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Abstractions.Exceptions;
using NUnit.Framework;
using Shouldly;

namespace KbStore.Catalog.Tests.Inventory;

public abstract class IncreaseQuantity_Tests : CatalogDomain_Tests
{
    protected override void Arrange()
    {
        TestQuantity = 10;
    }

    protected override async Task Act()
    {
        await CreateExistingInventory(quantity: 50);

        Result ??= await Subject.IncreaseQuantityAsync(TestId, TestQuantity);
    }

    public class When_increasing_successfully : IncreaseQuantity_Tests
    {
        [Test]
        public void It_should_not_throw()
            => LastException.ShouldBeNull();

        [Test]
        public void It_should_return_something()
            => Result.ShouldNotBeNull();

        [Test]
        public void It_should_be_successful()
            => Result!.InventoryId.ShouldBe(TestId);

        [Test]
        public void It_should_have_correct_quantity()
            => Result!.StockQuantity.ShouldBe(60); // Assuming initial quantity was 50

        [Test]
        public void It_should_have_updated_on_timestamp()
            => Result!.UpdatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));

        [Test]
        public void It_should_have_unchanged_created_on_timestamp()
            => Result!.CreatedOn.ShouldBe(Now);

        [Test]
        public async Task It_should_publish_inventory_quantity_increased_event()
            => (await Harness!.Published.Any<InventoryQuantityIncreased>()).ShouldBeTrue();
    }

    public class When_quantity_is_invalid : IncreaseQuantity_Tests
    {
        protected override void Arrange()
        {
            base.Arrange();

            TestQuantity = -5; // Invalid negative quantity
        }

        [Test]
        public void It_should_throw_validation_exception()
            => LastException.ShouldBeOfType<InventoryValidationException>();

        [Test]
        public void It_should_have_correct_error_message()
            => LastException!.Message.ShouldContain("Quantity");

        [Test]
        public async Task It_should_not_publish_inventory_quantity_increased_event()
            => (await Harness!.Published.Any<InventoryQuantityIncreased>()).ShouldBeFalse();
    }
}