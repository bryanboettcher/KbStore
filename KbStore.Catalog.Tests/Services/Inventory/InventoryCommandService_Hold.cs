namespace KbStore.Catalog.Tests.Services.Inventory;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using Catalog.Services;
using MassTransit;
using NUnit.Framework;
using Shouldly;


public abstract class InventoryCommandService_Hold : CommandService_Tests<MassTransitInventoryCommandService>
{
    protected InventoryModel? Result;

    protected Guid InventoryId;

    protected override void Arrange()
    {
        base.Arrange();

        InventoryId = ExistingId;
    }

    protected override async Task Act()
    {
        Result = await Subject.HoldAsync(InventoryId);
    }

    public class When_holding_successfully : InventoryCommandService_Hold
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<HoldInventoryRequest>(async context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                await context.RespondAsync<HoldInventoryResponse>(new
                {
                    InventoryId = context.Message.InventoryId,
                    PartNumber = "TEST_PART_123",
                    Description = "Test Description",
                    StockQuantity = 100,
                    Status = InventoryStatus.Held,
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
                x.Status.ShouldBe(InventoryStatus.Held);
                x.CreatedOn.ShouldBe(Now);
                x.UpdatedOn.ShouldBe(Now);
            });

            (await Harness.Consumed.Any<HoldInventoryRequest>()).ShouldBeTrue();
        });
    }

    public class When_holding_empty_inventory_id : InventoryCommandService_Hold
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

            (await Harness.Consumed.Any<HoldInventoryRequest>()).ShouldBeFalse();
        });
    }

    public class When_holding_nonexistent_item : InventoryCommandService_Hold
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<HoldInventoryRequest>(context =>
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

            (await Harness.Consumed.Any<HoldInventoryRequest>()).ShouldBeTrue();
        });
    }

    public class When_holding_already_held_item : InventoryCommandService_Hold
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<HoldInventoryRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw InventoryStateException.AlreadyHeld(context.Message.InventoryId);
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryStateException>();
            LastException.Message.ShouldContain("Held");
            LastException.Message.ShouldContain("Hold");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<HoldInventoryRequest>()).ShouldBeTrue();
        });
    }

    public class When_holding_backordered_item : InventoryCommandService_Hold
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<HoldInventoryRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw InventoryStateException.CannotModifyBackorderedItem(context.Message.InventoryId, "Hold");
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryStateException>();
            LastException.Message.ShouldContain("Backordered");
            LastException.Message.ShouldContain("Hold");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<HoldInventoryRequest>()).ShouldBeTrue();
        });
    }

    public class When_holding_discontinued_item : InventoryCommandService_Hold
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<HoldInventoryRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw InventoryStateException.CannotModifyDiscontinuedItem(context.Message.InventoryId, "Hold");
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryStateException>();
            LastException.Message.ShouldContain("Discontinued");
            LastException.Message.ShouldContain("Hold");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<HoldInventoryRequest>()).ShouldBeTrue();
        });
    }

    public class When_holding_generic_fault : InventoryCommandService_Hold
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<HoldInventoryRequest>(context =>
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

            (await Harness.Consumed.Any<HoldInventoryRequest>()).ShouldBeTrue();
        });
    }

    public class When_holding_timeout : InventoryCommandService_Hold
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<HoldInventoryRequest>(async context =>
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

            (await Harness.Consumed.Any<HoldInventoryRequest>()).ShouldBeTrue();
        });
    }
}