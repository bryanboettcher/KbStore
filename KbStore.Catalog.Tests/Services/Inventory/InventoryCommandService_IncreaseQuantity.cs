namespace KbStore.Catalog.Tests.Services.Inventory;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using Catalog.Services;
using MassTransit;
using NUnit.Framework;
using Shouldly;


public abstract class InventoryCommandService_IncreaseQuantity : CommandService_Tests<MassTransitInventoryCommandService>
{
    protected InventoryModel? Result;

    protected Guid InventoryId;
    protected int Quantity;

    protected override void Arrange()
    {
        base.Arrange();

        InventoryId = ExistingId;
        Quantity = 25;
    }

    protected override async Task Act()
    {
        Result = await Subject.IncreaseQuantityAsync(InventoryId, Quantity)
            .ConfigureAwait(false);
    }

    public class When_increasing_quantity_successfully : InventoryCommandService_IncreaseQuantity
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<IncreaseInventoryQuantityRequest>(async context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                await context.RespondAsync<UpdateInventoryResponse>(new
                {
                    InventoryId = context.Message.InventoryId,
                    PartNumber = "TEST_PART_123",
                    Description = "Test Description",
                    StockQuantity = 125, // Original 100 + 25
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
                x.StockQuantity.ShouldBe(125);
                x.Status.ShouldBe(InventoryStatus.Available);
                x.CreatedOn.ShouldBe(Now);
                x.UpdatedOn.ShouldBe(Now);
            });

            (await Harness.Consumed.Any<IncreaseInventoryQuantityRequest>()).ShouldBeTrue();
        });
    }

    public class When_increasing_quantity_empty_inventory_id : InventoryCommandService_IncreaseQuantity
    {
        protected override void Arrange()
        {
            base.Arrange();
            InventoryId = Guid.Empty;
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<ArgumentException>();
            LastException.Message.ShouldContain("InventoryId must be set");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<IncreaseInventoryQuantityRequest>()).ShouldBeFalse();
        });
    }

    public class When_increasing_quantity_zero_quantity : InventoryCommandService_IncreaseQuantity
    {
        protected override void Arrange()
        {
            base.Arrange();
            Quantity = 0;
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryValidationException>();
            LastException.Message.ShouldContain("Quantity can only be increased by a positive whole number");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<IncreaseInventoryQuantityRequest>()).ShouldBeFalse();
        });
    }

    public class When_increasing_quantity_negative_quantity : InventoryCommandService_IncreaseQuantity
    {
        protected override void Arrange()
        {
            base.Arrange();
            Quantity = -10;
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryValidationException>();
            LastException.Message.ShouldContain("Quantity can only be increased by a positive whole number");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<IncreaseInventoryQuantityRequest>()).ShouldBeFalse();
        });
    }

    public class When_increasing_quantity_nonexistent_item : InventoryCommandService_IncreaseQuantity
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<IncreaseInventoryQuantityRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw new InventoryNotFoundException(context.Message.InventoryId);
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryNotFoundException>();
            LastException.Message.ShouldContain("was not found");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<IncreaseInventoryQuantityRequest>()).ShouldBeTrue();
        });
    }

    public class When_increasing_quantity_held_item : InventoryCommandService_IncreaseQuantity
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<IncreaseInventoryQuantityRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw InventoryStateException.CannotModifyHeldItem(context.Message.InventoryId, "IncreaseQuantity");
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryStateException>();
            LastException.Message.ShouldContain("Held");
            LastException.Message.ShouldContain("IncreaseQuantity");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<IncreaseInventoryQuantityRequest>()).ShouldBeTrue();
        });
    }

    public class When_increasing_quantity_discontinued_item : InventoryCommandService_IncreaseQuantity
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<IncreaseInventoryQuantityRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw InventoryStateException.CannotModifyDiscontinuedItem(context.Message.InventoryId, "IncreaseQuantity");
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryStateException>();
            LastException.Message.ShouldContain("Discontinued");
            LastException.Message.ShouldContain("IncreaseQuantity");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<IncreaseInventoryQuantityRequest>()).ShouldBeTrue();
        });
    }

    public class When_increasing_quantity_generic_fault : InventoryCommandService_IncreaseQuantity
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<IncreaseInventoryQuantityRequest>(context =>
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

            (await Harness.Consumed.Any<IncreaseInventoryQuantityRequest>()).ShouldBeTrue();
        });
    }

    public class When_increasing_quantity_timeout : InventoryCommandService_IncreaseQuantity
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<IncreaseInventoryQuantityRequest>(async context =>
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

            (await Harness.Consumed.Any<IncreaseInventoryQuantityRequest>()).ShouldBeTrue();
        });
    }
}