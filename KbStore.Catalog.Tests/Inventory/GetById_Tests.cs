namespace KbStore.Catalog.Tests.Inventory;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using NUnit.Framework;
using Services;
using Shouldly;


public abstract class GetById_Tests : InventoryService_Tests<MassTransitInventoryCommandService>
{
    protected InventoryModel? Result;

    protected override void Arrange() { }

    protected override async Task Act()
    {
        await CreateExistingInventory();

        Result ??= await Subject.GetAsync(TestId);
    }

    public class When_getting_successfully : GetById_Tests
    {
        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Result.ShouldNotBeNull();
            Result.InventoryId.ShouldBe(TestId);
            Result.PartNumber.ShouldBe("TEST_PART_123");
            Result.Description.ShouldBe("Test Description");
            Result.StockQuantity.ShouldBe(50);
            Result.Status.ShouldBe(InventoryStatus.Available);
            Result.CreatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));
            Result.UpdatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));
        });
    }

    public class When_instance_is_missing : GetById_Tests
    {
        protected override void Arrange()
        {
            base.Arrange();

            TestId = Guid.NewGuid();
            // TODO: Setup to make service throw InventoryNotFoundException
        }

        [Test]
        public void It_should_be_correct() => Assert.Multiple(() =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryNotFoundException>();
            LastException.Message.ShouldContain("not found");
        });
    }
}