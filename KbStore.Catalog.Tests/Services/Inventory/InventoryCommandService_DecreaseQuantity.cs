namespace KbStore.Catalog.Tests.Services.Inventory;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using Catalog.Services;
using MassTransit;
using NUnit.Framework;
using Shouldly;


public abstract class InventoryCommandService_DecreaseQuantity : CommandService_Tests<MassTransitInventoryCommandService>
{
    protected InventoryModel? Result;

    protected Guid InventoryId;
    protected int Quantity;

    protected override void Arrange()
    {
        base.Arrange();

        InventoryId = ExistingId;
        Quantity = 15;
    }

    protected override async Task Act()
    {
        Result = await Subject.DecreaseQuantityAsync(InventoryId, Quantity);
    }

    public class When_decreasing_quantity_successfully : InventoryCommandService_DecreaseQuantity
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<DecreaseInventoryQuantityRequest>(async context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                await context.RespondAsync<UpdateInventoryResponse>(new
                {
                    InventoryId = context.Message.InventoryId,
                    PartNumber = "TEST_PART_123",
                    Description = "Test Description",
                    StockQuantity = 85, // Original 100 - 15
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
                x.StockQuantity.ShouldBe(85);
                x.Status.ShouldBe(InventoryStatus.Available);
                x.CreatedOn.ShouldBe(Now);
                x.UpdatedOn.ShouldBe(Now);
            });

            (await Harness.Consumed.Any<DecreaseInventoryQuantityRequest>()).ShouldBeTrue();
        });
    }

    public class When_decreasing_quantity_empty_inventory_id : InventoryCommandService_DecreaseQuantity
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

            (await Harness.Consumed.Any<DecreaseInventoryQuantityRequest>()).ShouldBeFalse();
        });
    }

    public class When_decreasing_quantity_zero_quantity : InventoryCommandService_DecreaseQuantity
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
            LastException.Message.ShouldContain("Quantity can only be decreased by a positive whole number");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<DecreaseInventoryQuantityRequest>()).ShouldBeFalse();
        });
    }

    public class When_decreasing_quantity_negative_quantity : InventoryCommandService_DecreaseQuantity
    {
        protected override void Arrange()
        {
            base.Arrange();
            Quantity = -5;
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryValidationException>();
            LastException.Message.ShouldContain("Quantity can only be decreased by a positive whole number");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<DecreaseInventoryQuantityRequest>()).ShouldBeFalse();
        });
    }

    public class When_decreasing_quantity_nonexistent_item : InventoryCommandService_DecreaseQuantity
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<DecreaseInventoryQuantityRequest>(context =>
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

            (await Harness.Consumed.Any<DecreaseInventoryQuantityRequest>()).ShouldBeTrue();
        });
    }

    public class When_decreasing_quantity_insufficient_stock : InventoryCommandService_DecreaseQuantity
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<DecreaseInventoryQuantityRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw InventoryValidationException.InsufficientStock(context.Message.Amount, 10, context.Message.InventoryId);
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryValidationException>();
            LastException.Message.ShouldContain("Cannot decrease quantity");
            LastException.Message.ShouldContain("Current stock");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<DecreaseInventoryQuantityRequest>()).ShouldBeTrue();
        });
    }

    public class When_decreasing_quantity_held_item : InventoryCommandService_DecreaseQuantity
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<DecreaseInventoryQuantityRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw InventoryStateException.CannotModifyHeldItem(context.Message.InventoryId, "DecreaseQuantity");
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryStateException>();
            LastException.Message.ShouldContain("Held");
            LastException.Message.ShouldContain("DecreaseQuantity");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<DecreaseInventoryQuantityRequest>()).ShouldBeTrue();
        });
    }

    public class When_decreasing_quantity_discontinued_item : InventoryCommandService_DecreaseQuantity
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<DecreaseInventoryQuantityRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw InventoryStateException.CannotModifyDiscontinuedItem(context.Message.InventoryId, "DecreaseQuantity");
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryStateException>();
            LastException.Message.ShouldContain("Discontinued");
            LastException.Message.ShouldContain("DecreaseQuantity");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<DecreaseInventoryQuantityRequest>()).ShouldBeTrue();
        });
    }

    public class When_decreasing_quantity_generic_fault : InventoryCommandService_DecreaseQuantity
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<DecreaseInventoryQuantityRequest>(context =>
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

            (await Harness.Consumed.Any<DecreaseInventoryQuantityRequest>()).ShouldBeTrue();
        });
    }

    public class When_decreasing_quantity_timeout : InventoryCommandService_DecreaseQuantity
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<DecreaseInventoryQuantityRequest>(async context =>
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

            (await Harness.Consumed.Any<DecreaseInventoryQuantityRequest>()).ShouldBeTrue();
        });
    }
}