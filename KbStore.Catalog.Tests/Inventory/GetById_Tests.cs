namespace KbStore.Catalog.Tests.Inventory;

using Abstractions.Contracts;
using KbStore.Catalog.Abstractions.Exceptions;
using KbStore.Catalog.Tests;
using NUnit.Framework;
using Shouldly;


public abstract class GetById_Tests : CatalogDomain_Tests
{
    protected override void Arrange() { }

    protected override async Task Act()
    {
        await CreateExistingInventory();
        
        Result ??= await Subject.GetAsync(TestId);
    }

    public class When_getting_successfully : GetById_Tests
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
        public void It_should_have_correct_part_number()
            => Result!.PartNumber.ShouldBe("TEST_PART_123");

        [Test]
        public void It_should_have_correct_description()
            => Result!.Description.ShouldBe("Test Description");

        [Test]
        public void It_should_have_correct_quantity()
            => Result!.StockQuantity.ShouldBe(50);

        [Test]
        public void It_should_have_correct_status()
            => Result!.Status.ShouldBe(InventoryStatus.Available);

        [Test]
        public void It_should_have_valid_created_on_timestamp()
            => Result!.CreatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));

        [Test]
        public void It_should_have_valid_updated_on_timestamp()
            => Result!.UpdatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));

        [Test]
        public async Task It_should_consume_get_request()
            => (await Harness!.Consumed.Any<InventoryStatusRequest>()).ShouldBeTrue();
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
        public void It_should_throw_not_found_exception()
            => LastException.ShouldBeOfType<InventoryNotFoundException>();

        [Test]
        public void It_should_have_correct_error_message()
            => LastException!.Message.ShouldContain("not found");

        [Test]
        public async Task It_should_consume_get_request()
            => (await Harness!.Consumed.Any<InventoryStatusRequest>()).ShouldBeTrue();
    }
}