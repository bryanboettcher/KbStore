namespace KbStore.Catalog.Tests.Services.Inventory;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using Catalog.Services;
using MassTransit;
using NUnit.Framework;
using Shouldly;


public abstract class InventoryCommandService_UpdateDescription : CommandService_Tests<MassTransitInventoryCommandService>
{
    protected InventoryModel? Result;

    protected Guid InventoryId;
    protected string Description = null!;

    protected override void Arrange()
    {
        base.Arrange();

        InventoryId = ExistingId;
        Description = "Updated Test Description";
    }

    protected override async Task Act()
    {
        Result = await Subject.UpdateDescriptionAsync(InventoryId, Description);
    }

    public class When_updating_description_successfully : InventoryCommandService_UpdateDescription
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateInventoryDescriptionRequest>(async context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                await context.RespondAsync<UpdateInventoryResponse>(new
                {
                    InventoryId = context.Message.InventoryId,
                    PartNumber = "TEST_PART_123",
                    Description = context.Message.Description,
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
                x.Description.ShouldBe("Updated Test Description");
                x.StockQuantity.ShouldBe(100);
                x.Status.ShouldBe(InventoryStatus.Available);
                x.CreatedOn.ShouldBe(Now);
                x.UpdatedOn.ShouldBe(Now);
            });

            (await Harness.Consumed.Any<UpdateInventoryDescriptionRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_description_empty_inventory_id : InventoryCommandService_UpdateDescription
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

            (await Harness.Consumed.Any<UpdateInventoryDescriptionRequest>()).ShouldBeFalse();
        });
    }

    public class When_updating_description_null_description : InventoryCommandService_UpdateDescription
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
            LastException.Message.ShouldContain("Description cannot be empty");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<UpdateInventoryDescriptionRequest>()).ShouldBeFalse();
        });
    }

    public class When_updating_description_empty_description : InventoryCommandService_UpdateDescription
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
            LastException.Message.ShouldContain("Description cannot be empty");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<UpdateInventoryDescriptionRequest>()).ShouldBeFalse();
        });
    }

    public class When_updating_description_nonexistent_item : InventoryCommandService_UpdateDescription
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateInventoryDescriptionRequest>(context =>
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

            (await Harness.Consumed.Any<UpdateInventoryDescriptionRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_description_discontinued_item : InventoryCommandService_UpdateDescription
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateInventoryDescriptionRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw InventoryStateException.CannotModifyDiscontinuedItem(context.Message.InventoryId, "UpdateDescription");
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryStateException>();
            LastException.Message.ShouldContain("Discontinued");
            LastException.Message.ShouldContain("UpdateDescription");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<UpdateInventoryDescriptionRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_description_generic_fault : InventoryCommandService_UpdateDescription
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateInventoryDescriptionRequest>(context =>
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

            (await Harness.Consumed.Any<UpdateInventoryDescriptionRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_description_timeout : InventoryCommandService_UpdateDescription
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateInventoryDescriptionRequest>(async context =>
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

            (await Harness.Consumed.Any<UpdateInventoryDescriptionRequest>()).ShouldBeTrue();
        });
    }
}