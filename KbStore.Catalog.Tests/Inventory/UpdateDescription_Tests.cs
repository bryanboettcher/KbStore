namespace KbStore.Catalog.Tests.Inventory;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using NUnit.Framework;
using Services;
using Shouldly;


public abstract class UpdateDescription_Tests : InventoryService_Tests<MassTransitInventoryCommandService>
{
    protected InventoryModel? Result;

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
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Result.ShouldNotBeNull();
            Result.InventoryId.ShouldBe(TestId);
            Result.Description.ShouldBe("Updated Description");
            Result.UpdatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));
            Result.CreatedOn.ShouldBe(InitialCreatedOn);

            Harness.ShouldNotBeNull();
            (await Harness.Published.Any<InventoryDescriptionUpdated>()).ShouldBeTrue();
        });
    }

    public class When_description_is_invalid : UpdateDescription_Tests
    {
        protected override void Arrange()
        {
            base.Arrange();

            TestDescription = ""; // Invalid empty description
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryValidationException>();
            LastException.Message.ShouldContain("Description");

            Harness.ShouldNotBeNull();
            (await Harness.Published.Any<InventoryDescriptionUpdated>()).ShouldBeFalse();
        });
    }
}