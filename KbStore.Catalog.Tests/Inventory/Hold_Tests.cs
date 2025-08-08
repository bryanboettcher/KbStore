namespace KbStore.Catalog.Tests.Inventory;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using KbStore.Catalog.Services;
using NUnit.Framework;
using Shouldly;


public abstract class Hold_Tests : InventoryService_Tests<MassTransitInventoryCommandService>
{
    protected InventoryModel? Result = null!;

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
        public void It_should_not_throw()
            => LastException.ShouldBeNull();

        [Test]
        public void It_should_return_something()
            => Result.ShouldNotBeNull();

        [Test]
        public void It_should_be_successful()
            => Result!.InventoryId.ShouldBe(TestId);

        [Test]
        public void It_should_have_correct_status()
            => Result!.Status.ShouldBe(InventoryStatus.Held);

        [Test]
        public void It_should_have_updated_on_timestamp()
            => Result!.UpdatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));

        [Test]
        public void It_should_have_unchanged_created_on_timestamp()
            => Result!.CreatedOn.ShouldBe(InitialCreatedOn);

        [Test]
        public async Task It_should_publish_inventory_held_event()
            => (await Harness!.Published.Any<InventoryHeld>()).ShouldBeTrue();
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
        public void It_should_throw_state_exception()
            => LastException.ShouldBeOfType<InventoryStateException>();

        [Test]
        public void It_should_have_correct_error_message()
            => LastException!.Message.ShouldContain("Cannot perform 'Hold'");

        [Test]
        public void It_should_not_publish_inventory_held_event()
            => Harness!.Published.Select<InventoryHeld>().Count().ShouldBe(1);
    }
}