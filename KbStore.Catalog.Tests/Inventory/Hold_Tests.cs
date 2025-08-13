namespace KbStore.Catalog.Tests.Inventory;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using NUnit.Framework;
using Services;
using Shouldly;


public abstract class Hold_Tests : InventoryService_Tests<MassTransitInventoryCommandService>
{
    protected InventoryModel? Result;

    protected override void Arrange() { }

    protected override async Task Act()
    {
        await CreateExistingInventory();

        Now = Later;
        Result ??= await Subject.HoldAsync(TestId);
    }

    public class When_holding_successfully : Hold_Tests
    {
        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Result.ShouldNotBeNull();
            Result.InventoryId.ShouldBe(TestId);
            Result.Status.ShouldBe(InventoryStatus.Held);
            Result.UpdatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));
            Result.CreatedOn.ShouldBe(InitialCreatedOn);

            (await Harness.Published.Any<InventoryHeld>()).ShouldBeTrue();
        });
    }

    public class When_item_cannot_be_held : Hold_Tests
    {
        protected override async Task Act()
        {
            // create and hold an item, then run the base code
            // which will create (but no-op) and hold the already-held item
            await CreateExistingInventory();
            await Subject.HoldAsync(TestId);

            await base.Act();
        }

        [Test]
        public void It_should_be_correct() => Assert.Multiple(() =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryStateException>();
            LastException.Message.ShouldContain("Cannot perform 'Hold'");

            Harness.Published.Select<InventoryHeld>().Count().ShouldBe(1);
        });
    }
}