using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Services;
using MassTransit;
using NUnit.Framework;


namespace KbStore.Catalog.Tests.Services.Inventory;

using Abstractions.Exceptions;
using Shouldly;


public abstract class InventoryCommandService_Create : CommandService_Tests<MassTransitInventoryCommandService>
{
    protected InventoryModel? Result;

    protected int StockQuantity;
    protected string Description = null!;
    protected string PartNumber = null!;

    protected override void Arrange()
    {
        base.Arrange();

        PartNumber = "TEST_PART_123";
        Description = "Test Description";
        StockQuantity = 100;
    }

    protected override async Task Act()
    {
        Result = await Subject.CreateAsync(PartNumber, Description, StockQuantity)
            .ConfigureAwait(false);
    }

    public class When_creating_valid_inventory : InventoryCommandService_Create
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<CreateInventoryRequest>(async context =>
            {
                await context.RespondAsync<CreateInventoryResponse>(new
                {
                    InventoryId = ExistingId,
                    PartNumber = context.Message.PartNumber,
                    Description = context.Message.Description,
                    StockQuantity = context.Message.StockQuantity,
                    Status = InventoryStatus.Available,
                    CreatedOn = Now,
                    UpdatedOn = Now
                });
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Result.ShouldNotBeNull();
            Result.ShouldSatisfyAllConditions(x =>
            {
                x.InventoryId.ShouldBe(ExistingId);
                x.PartNumber.ShouldBe("TEST_PART_123");
                x.Description.ShouldBe("Test Description");
                x.StockQuantity.ShouldBe(100);
                x.Status.ShouldBe(InventoryStatus.Available);
                x.CreatedOn.ShouldBe(Now);
                x.UpdatedOn.ShouldBe(Now);
            });

            (await Harness.Consumed.Any<CreateInventoryRequest>()).ShouldBeTrue();
        });
    }

    public class When_creating_inventory_zero_quantity : InventoryCommandService_Create
    {
        protected override void Arrange()
        {
            base.Arrange();
            StockQuantity = 0;
        }

        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<CreateInventoryRequest>(async context =>
            {
                await context.RespondAsync<CreateInventoryResponse>(new
                {
                    InventoryId = ExistingId,
                    PartNumber = context.Message.PartNumber,
                    Description = context.Message.Description,
                    StockQuantity = context.Message.StockQuantity,
                    Status = InventoryStatus.Available,
                    CreatedOn = Now,
                    UpdatedOn = Now
                });
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Result.ShouldNotBeNull();
            Result.StockQuantity.ShouldBe(0);

            (await Harness.Consumed.Any<CreateInventoryRequest>()).ShouldBeTrue();
        });
    }

    public class When_creating_inventory_null_part_number : InventoryCommandService_Create
    {
        protected override void Arrange()
        {
            base.Arrange();
            PartNumber = null!;
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryValidationException>();
            LastException.Message.ShouldContain("PartNumber must have a value");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<CreateInventoryRequest>()).ShouldBeFalse();
        });
    }

    public class When_creating_inventory_empty_part_number : InventoryCommandService_Create
    {
        protected override void Arrange()
        {
            base.Arrange();
            PartNumber = "";
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryValidationException>();
            LastException.Message.ShouldContain("PartNumber must have a value");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<CreateInventoryRequest>()).ShouldBeFalse();
        });
    }

    public class When_creating_inventory_whitespace_part_number : InventoryCommandService_Create
    {
        protected override void Arrange()
        {
            base.Arrange();
            PartNumber = "   ";
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryValidationException>();
            LastException.Message.ShouldContain("PartNumber must have a value");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<CreateInventoryRequest>()).ShouldBeFalse();
        });
    }

    public class When_creating_inventory_null_description : InventoryCommandService_Create
    {
        protected override void Arrange()
        {
            base.Arrange();
            Description = null!;
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryValidationException>();
            LastException.Message.ShouldContain("Description must have a value");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<CreateInventoryRequest>()).ShouldBeFalse();
        });
    }

    public class When_creating_inventory_empty_description : InventoryCommandService_Create
    {
        protected override void Arrange()
        {
            base.Arrange();
            Description = "";
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryValidationException>();
            LastException.Message.ShouldContain("Description must have a value");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<CreateInventoryRequest>()).ShouldBeFalse();
        });
    }

    public class When_creating_inventory_negative_quantity : InventoryCommandService_Create
    {
        protected override void Arrange()
        {
            base.Arrange();
            StockQuantity = -1;
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryValidationException>();
            LastException.Message.ShouldContain("StockQuantity must be a positive value");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<CreateInventoryRequest>()).ShouldBeFalse();
        });
    }

    public class When_creating_inventory_duplicate_part_number : InventoryCommandService_Create
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<CreateInventoryRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw InventoryConflictException.DuplicatePartNumber("TEST_PART_123");
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryConflictException>();
            LastException.Message.ShouldContain("part number");
            LastException.Message.ShouldContain("TEST_PART_123");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<CreateInventoryRequest>()).ShouldBeTrue();
        });
    }

    public class When_creating_inventory_generic_fault : InventoryCommandService_Create
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<CreateInventoryRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw new GenericInventoryException("Unknown inventory operation failed");
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<GenericInventoryException>();
            LastException.Message.ShouldContain("Unknown inventory operation failed");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<CreateInventoryRequest>()).ShouldBeTrue();
        });
    }

    public class When_creating_inventory_timeout : InventoryCommandService_Create
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);
            conf.AddHandler<CreateInventoryRequest>(async context =>
            {
                await Task.Delay(1000, context.CancellationToken);
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<RequestTimeoutException>();

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<CreateInventoryRequest>()).ShouldBeTrue();
        });
    }
}