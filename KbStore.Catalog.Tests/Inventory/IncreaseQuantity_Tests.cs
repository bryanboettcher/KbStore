namespace KbStore.Catalog.Tests.Inventory;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using NUnit.Framework;
using Services;
using Shouldly;


public abstract class IncreaseQuantity_Tests : InventoryService_Tests<MassTransitInventoryCommandService>
{
    protected InventoryModel? Result;

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
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Result.ShouldNotBeNull();
            Result.InventoryId.ShouldBe(TestId);
            Result.StockQuantity.ShouldBe(60); // Assuming initial quantity was 50
            Result.UpdatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));
            Result.CreatedOn.ShouldBe(Now);

            Harness.ShouldNotBeNull();
            (await Harness.Published.Any<InventoryQuantityIncreased>()).ShouldBeTrue();
        });
    }

    public class When_quantity_is_invalid : IncreaseQuantity_Tests
    {
        protected override void Arrange()
        {
            base.Arrange();

            TestQuantity = -5; // Invalid negative quantity
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryValidationException>();
            LastException.Message.ShouldContain("Quantity");

            Harness.ShouldNotBeNull();
            (await Harness.Published.Any<InventoryQuantityIncreased>()).ShouldBeFalse();
        });
    }
}