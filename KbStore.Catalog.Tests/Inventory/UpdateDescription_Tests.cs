namespace KbStore.Catalog.Tests.Inventory;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using KbStore.Catalog.Services;
using NUnit.Framework;
using Shouldly;


public abstract class UpdateDescription_Tests : InventoryService_Tests<MassTransitInventoryCommandService>
{
    protected InventoryModel? Result = null!;

    protected override void Arrange()
    {
        TestDescription = "Updated Description";
    }

    protected override async Task Act()
    {
        await CreateExistingInventory();

        Now = Later;
        Result ??= await Subject.UpdateDescriptionAsync(TestId, TestDescription!);
    }

    public class When_updating_successfully : UpdateDescription_Tests
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
        public void It_should_have_correct_description()
            => Result!.Description.ShouldBe("Updated Description");

        [Test]
        public void It_should_have_updated_on_timestamp()
            => Result!.UpdatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));

        [Test]
        public void It_should_have_unchanged_created_on_timestamp()
            => Result!.CreatedOn.ShouldBe(InitialCreatedOn);

        [Test]
        public async Task It_should_publish_inventory_description_updated_event()
            => (await Harness!.Published.Any<InventoryDescriptionUpdated>()).ShouldBeTrue();
    }

    public class When_description_is_invalid : UpdateDescription_Tests
    {
        protected override void Arrange()
        {
            base.Arrange();

            TestDescription = ""; // Invalid empty description
        }

        [Test]
        public void It_should_throw_validation_exception()
            => LastException.ShouldBeOfType<InventoryValidationException>();

        [Test]
        public void It_should_have_correct_error_message()
            => LastException!.Message.ShouldContain("Description");
        
        [Test]
        public async Task It_should_not_publish_inventory_description_updated_event()
            => (await Harness!.Published.Any<InventoryDescriptionUpdated>()).ShouldBeFalse();
    }
}