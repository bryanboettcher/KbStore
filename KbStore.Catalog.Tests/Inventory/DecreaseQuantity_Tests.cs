namespace KbStore.Catalog.Tests.Inventory;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using KbStore.Catalog.Services;
using NUnit.Framework;
using Shouldly;


public abstract class DecreaseQuantity_Tests : InventoryService_Tests<MassTransitInventoryCommandService>
{
    protected InventoryModel? Result = null!;

    protected override void Arrange()
    {
        TestQuantity = 10;
    }

    protected override async Task Act()
    {
        await CreateExistingInventory(quantity:50);

        Now = Later;
        Result ??= await Subject.DecreaseQuantityAsync(TestId, TestQuantity);
    }

    public class When_decreasing_successfully : DecreaseQuantity_Tests
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
            => Result!.StockQuantity.ShouldBe(40); // Assuming initial quantity was 50

        [Test]
        public void It_should_have_updated_on_timestamp()
            => Result!.UpdatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));

        [Test]
        public void It_should_have_unchanged_created_on_timestamp()
            => Result!.CreatedOn.ShouldBe(InitialCreatedOn);

        [Test]
        public async Task It_should_publish_inventory_quantity_decreased_event()
            => (await Harness!.Published.Any<InventoryQuantityDecreased>()).ShouldBeTrue();
    }

    public class When_quantity_is_invalid : DecreaseQuantity_Tests
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
        public async Task It_should_not_publish_inventory_quantity_decreased_event()
            => (await Harness!.Published.Any<InventoryQuantityDecreased>()).ShouldBeFalse();
    }
}