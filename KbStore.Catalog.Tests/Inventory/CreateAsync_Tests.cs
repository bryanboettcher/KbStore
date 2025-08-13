namespace KbStore.Catalog.Tests.Inventory;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using NUnit.Framework;
using Services;
using Shouldly;


public abstract class CreateAsync_Tests : InventoryService_Tests<MassTransitInventoryCommandService>
{
    protected InventoryModel? Result;

    protected override void Arrange()
    {
        TestPartNumber = "TEST_PART_123";
        TestDescription = "Test Description";
        TestQuantity = 50;
    }

    protected override async Task Act()
    {
        Result ??= await Subject.CreateAsync(TestPartNumber!, TestDescription!, TestQuantity);
    }


    public class When_creating_valid_inventory_item_should_succeed : CreateAsync_Tests
    {
        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Result.ShouldNotBeNull();
            Result.PartNumber.ShouldBe("TEST_PART_123");
            Result.Description.ShouldBe("Test Description");
            Result.Status.ShouldBe(InventoryStatus.Available);
            Result.CreatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));
            Result.UpdatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));

            (await Harness.Published.Any<InventoryCreated>()).ShouldBeTrue();
        });
    }

    public class When_creating_with_negative_quantity_should_fail : CreateAsync_Tests
    {
        protected override void Arrange()
        {
            base.Arrange();

            TestQuantity = -5;
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryValidationException>();
            LastException.Message.ShouldContain("StockQuantity");

            (await Harness.Published.Any<InventoryCreated>()).ShouldBeFalse();
        });
    }

    public class When_creating_with_empty_part_number : CreateAsync_Tests
    {
        protected override void Arrange()
        {
            base.Arrange();

            TestPartNumber = "";
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryValidationException>();
            LastException.Message.ShouldContain("PartNumber");

            (await Harness.Published.Any<InventoryCreated>()).ShouldBeFalse();
        });
    }

    public class When_creating_with_null_description : CreateAsync_Tests
    {
        protected override void Arrange()
        {
            base.Arrange();

            TestDescription = null!;
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryValidationException>();
            LastException.Message.ShouldContain("Description");

            (await Harness.Published.Any<InventoryCreated>()).ShouldBeFalse();
        });
    }

    public class When_creating_duplicate_part_number : CreateAsync_Tests
    {
        protected override async Task Act()
        {
            // create our initial
            await Subject.CreateAsync(TestPartNumber!, TestDescription!, TestQuantity);

            // create the duplicate
            await base.Act();
        }

        [Test]
        public void It_should_be_correct() => Assert.Multiple(() =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryConflictException>();
            LastException.Message.ShouldContain("TEST_PART_123");

            Harness.Published.Select<InventoryCreated>().Count().ShouldBe(1);
        });
    }
}