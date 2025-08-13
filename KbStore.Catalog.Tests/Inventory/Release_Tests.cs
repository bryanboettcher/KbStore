namespace KbStore.Catalog.Tests.Inventory;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using NUnit.Framework;
using Services;
using Shouldly;


public abstract class Release_Tests : InventoryService_Tests<MassTransitInventoryCommandService>
{
    protected InventoryModel? Result;

    protected override void Arrange() { }

    protected override async Task Act()
    {
        // create and hold an item if we don't have one yet
        if (TestId == Guid.Empty)
        {
            await CreateExistingInventory();
            await Subject.HoldAsync(TestId);
        }

        Now = Later;
        Result ??= await Subject.ReleaseAsync(TestId);
    }

    public class When_releasing_successfully : Release_Tests
    {
        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Result.ShouldNotBeNull();
            Result.InventoryId.ShouldBe(TestId);
            Result.Status.ShouldBe(InventoryStatus.Available);
            Result.UpdatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));
            Result.CreatedOn.ShouldBe(InitialCreatedOn);

            Harness.ShouldNotBeNull();
            (await Harness.Published.Any<InventoryReleased>()).ShouldBeTrue();
        });
    }

    public class When_item_cannot_be_released : Release_Tests
    {
        protected override async Task Act()
        {
            await CreateExistingInventory();
            await Subject.ReleaseAsync(TestId);

            await base.Act();
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryStateException>();
            LastException.Message.ShouldContain("Cannot perform 'Release'");

            Harness.ShouldNotBeNull();
            (await Harness.Published.Any<InventoryReleased>()).ShouldBeFalse();
        });
    }
}