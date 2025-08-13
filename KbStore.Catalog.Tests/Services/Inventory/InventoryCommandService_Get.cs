namespace KbStore.Catalog.Tests.Services.Inventory;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using Catalog.Services;
using MassTransit;
using NUnit.Framework;
using Shouldly;


public abstract class InventoryCommandService_Get : CommandService_Tests<MassTransitInventoryCommandService>
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
        Result = await Subject.GetAsync(InventoryId);
    }

    public class When_getting_successfully : InventoryCommandService_Get
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<InventoryStatusRequest>(async context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                await context.RespondAsync<InventoryStatusResponse>(new
                {
                    InventoryId = context.Message.InventoryId,
                    PartNumber = "TEST_PART_123",
                    Description = "Test Description",
                    StockQuantity = 100,
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

            (await Harness.Consumed.Any<InventoryStatusRequest>()).ShouldBeTrue();
        });
    }

    public class When_getting_empty_inventory_id : InventoryCommandService_Get
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

            (await Harness.Consumed.Any<InventoryStatusRequest>()).ShouldBeFalse();
        });
    }

    public class When_getting_nonexistent_item : InventoryCommandService_Get
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<InventoryStatusRequest>(context =>
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

            (await Harness.Consumed.Any<InventoryStatusRequest>()).ShouldBeTrue();
        });
    }

    public class When_getting_generic_fault : InventoryCommandService_Get
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<InventoryStatusRequest>(context =>
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

            (await Harness.Consumed.Any<InventoryStatusRequest>()).ShouldBeTrue();
        });
    }

    public class When_getting_timeout : InventoryCommandService_Get
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<InventoryStatusRequest>(async context =>
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

            (await Harness.Consumed.Any<InventoryStatusRequest>()).ShouldBeTrue();
        });
    }
}