namespace KbStore.Catalog.Tests;

using Abstractions.Contracts;
using Domains.Inventory;
using Services;
using KbStore.Tests;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;
using KbStore.Catalog.Abstractions.Exceptions;

[TestFixture]
public abstract class CatalogDomain_Tests : TestBase
{
    protected ISagaStateMachineTestHarness<InventoryStateMachine, InventoryEntity>? SagaHarness;

    protected override Task OnPostSetup()
    {
        SagaHarness = Harness!.GetSagaStateMachineHarness<InventoryStateMachine, InventoryEntity>();

        return base.OnPostSetup();
    }
}

public abstract class CreateAsync_Tests : CatalogDomain_Tests
{
    protected MassTransitInventoryCommandService Subject = null!;
    protected InventoryModel? Result = null!;

    protected string? TestPartNumber;
    protected string? TestDescription;
    protected int TestQuantity;

    protected override void OnServicesCreating(IServiceCollection services)
    {
        services.AddScoped<MassTransitInventoryCommandService>();
    }

    protected override void OnHarnessCreating(IBusRegistrationConfigurator configurator)
    {
        configurator.AddSagaStateMachine<InventoryStateMachine, InventoryEntity>();
    }

    protected override async Task OnPostSetup()
    {
        Subject = ScopedProvider.ServiceProvider.GetRequiredService<MassTransitInventoryCommandService>();
    }

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
        public void It_should_not_throw() 
            => LastException.ShouldBeNull();

        [Test]
        public void It_should_return_valid_result() 
            => Result.ShouldNotBeNull();

        [Test]
        public void It_should_have_correct_part_number() 
            => Result!.PartNumber.ShouldBe("TEST_PART_123");

        [Test]
        public void It_should_set_the_description() 
            => Result!.Description.ShouldBe("Test Description");

        [Test]
        public void It_should_have_available_status() 
            => Result!.Status.ShouldBe(InventoryStatus.Available);

        [Test]
        public async Task It_should_publish_inventory_created_event()
            => (await Harness!.Published.Any<InventoryCreated>()).ShouldBeTrue();

        [Test]
        public async Task It_should_consume_create_request()
            => (await Harness!.Consumed.Any<CreateInventoryRequest>()).ShouldBeTrue();
    }

    public class When_creating_with_negative_quantity_should_fail : CreateAsync_Tests
    {
        protected override void Arrange()
        {
            base.Arrange();

            TestQuantity = -5;
        }
        
        [Test]
        public void It_should_throw_validation_exception() 
            => LastException.ShouldBeOfType<InventoryValidationException>();

        [Test]
        public void It_should_have_correct_error_message() 
            => LastException?.Message.ShouldContain("StockQuantity");

        [Test]
        public async Task It_should_consume_create_request() 
            => await Harness!.Consumed.Any<CreateInventoryRequest>();

        [Test]
        public async Task It_should_not_publish_inventory_created_event() 
            => (await Harness!.Published.Any<InventoryCreated>()).ShouldBeFalse();
    }

    public class When_creating_with_empty_part_number : CreateAsync_Tests
    {
        protected override void Arrange()
        {
            base.Arrange();

            TestPartNumber = "";
        }

        [Test]
        public void It_should_throw_validation_exception() 
            => LastException.ShouldBeOfType<InventoryValidationException>();

        [Test]
        public void It_should_have_correct_error_message() 
            => LastException?.Message.ShouldContain("PartNumber");

        [Test]
        public async Task It_should_consume_create_request() 
            => await Harness!.Consumed.Any<CreateInventoryRequest>();

        [Test]
        public async Task It_should_not_publish_inventory_created_event() 
            => (await Harness!.Published.Any<InventoryCreated>()).ShouldBeFalse();
    }

    public class When_creating_with_null_description : CreateAsync_Tests
    {
        protected override void Arrange()
        {
            base.Arrange();

            TestDescription = null!;
        }

        [Test]
        public void It_should_throw_validation_exception() 
            => LastException.ShouldBeOfType<InventoryValidationException>();

        [Test]
        public void It_should_have_correct_error_message() 
            => LastException?.Message.ShouldContain("Description");

        [Test]
        public async Task It_should_consume_create_request() 
            => await Harness!.Consumed.Any<CreateInventoryRequest>();

        [Test]
        public async Task It_should_not_publish_inventory_created_event() 
            => (await Harness!.Published.Any<InventoryCreated>()).ShouldBeFalse();
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
        public void It_should_throw_conflict_exception() 
            => LastException.ShouldBeOfType<InventoryConflictException>();

        [Test]
        public void It_should_mention_duplicate_part_number() 
            => LastException?.Message.ShouldContain("TEST_PART_123");

        [Test]
        public async Task It_should_consume_create_request() 
            => await Harness!.Consumed.Any<CreateInventoryRequest>();

        [Test]
        public void It_should_not_publish_additional_inventory_created_event() 
            => (Harness!.Published.Select<InventoryCreated>().Count()).ShouldBe(1);
    }
}