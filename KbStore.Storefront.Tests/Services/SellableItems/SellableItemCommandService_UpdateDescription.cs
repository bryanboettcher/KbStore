namespace KbStore.Storefront.Tests.Services.SellableItems;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using KbStore.Storefront.Services;
using MassTransit;
using NUnit.Framework;
using Shouldly;


public abstract class SellableItemCommandService_UpdateDescription : CommandService_Tests<SellableItemCommandService>
{
    protected SellableItemModel? Result;

    protected Guid SellableItemId;
    protected string? Description;

    protected override void Arrange()
    {
        base.Arrange();

        SellableItemId = ExistingId;
        Description = "Updated Description";
    }

    protected override async Task Act()
    {
        Result = await Subject.UpdateDescriptionAsync(SellableItemId, Description);
    }

    public class When_updating_description_valid : SellableItemCommandService_UpdateDescription
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateSellableItemDescriptionRequest>(async context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                await context.RespondAsync<UpdateSellableItemResponse>(new
                {
                    SellableItemId = ExistingId,
                    ProductId = (Guid?)null,
                    Sku = "TEST_SKU",
                    Name = "Test Item",
                    Description = context.Message.Description,
                    BasePrice = 19.99m,
                    ItemType = "Standard",
                    Payload = new Dictionary<string, object?> { { "key1", "value1" } },
                    IsAvailable = true,
                    Version = 2,
                    CreatedOn = Now,
                    UpdatedOn = Later
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
                x.SellableItemId.ShouldBe(ExistingId);
                x.Description.ShouldBe("Updated Description");
                x.Version.ShouldBe(2);
                x.UpdatedOn.ShouldBe(Later);
            });

            (await Harness.Consumed.Any<UpdateSellableItemDescriptionRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_description_empty_id : SellableItemCommandService_UpdateDescription
    {
        protected override void Arrange()
        {
            base.Arrange();
            SellableItemId = Guid.Empty;
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<ArgumentException>();
            LastException.Message.ShouldContain("SellableItemId must be set");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<UpdateSellableItemDescriptionRequest>()).ShouldBeFalse();
        });
    }

    public class When_updating_description_not_found : SellableItemCommandService_UpdateDescription
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateSellableItemDescriptionRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw new SellableItemNotFoundException(ExistingId);
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<SellableItemNotFoundException>();
            LastException.Message.ShouldContain(ExistingId.ToString());

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<UpdateSellableItemDescriptionRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_description_in_discontinued_state : SellableItemCommandService_UpdateDescription
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateSellableItemDescriptionRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw new SellableItemStateException("Discontinued", "update description");
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<SellableItemStateException>();
            LastException.Message.ShouldContain("Discontinued");
            LastException.Message.ShouldContain("update description");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<UpdateSellableItemDescriptionRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_description_generic_fault : SellableItemCommandService_UpdateDescription
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateSellableItemDescriptionRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw new GenericSellableItemException("Unknown operation failed");
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<GenericSellableItemException>();
            LastException.Message.ShouldContain("Unknown operation failed");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<UpdateSellableItemDescriptionRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_description_timeout : SellableItemCommandService_UpdateDescription
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateSellableItemDescriptionRequest>(async context =>
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

            (await Harness.Consumed.Any<UpdateSellableItemDescriptionRequest>()).ShouldBeTrue();
        });
    }
}
