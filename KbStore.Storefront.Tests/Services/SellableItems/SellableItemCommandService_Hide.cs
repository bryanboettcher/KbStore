namespace KbStore.Storefront.Tests.Services.SellableItems;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using KbStore.Storefront.Services;
using MassTransit;
using NUnit.Framework;
using Shouldly;


public abstract class SellableItemCommandService_Hide : CommandService_Tests<SellableItemCommandService>
{
    protected SellableItemModel? Result;

    protected Guid SellableItemId;

    protected override void Arrange()
    {
        base.Arrange();

        SellableItemId = ExistingId;
    }

    protected override async Task Act()
    {
        Result = await Subject.HideAsync(SellableItemId);
    }

    public class When_hiding_valid : SellableItemCommandService_Hide
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<HideSellableItemRequest>(async context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                await context.RespondAsync<HideSellableItemResponse>(new
                {
                    SellableItemId = ExistingId,
                    ProductId = (Guid?)null,
                    Sku = "TEST_SKU",
                    Name = "Test Item",
                    Description = "A test item",
                    BasePrice = 19.99m,
                    ItemType = "Standard",
                    Payload = new Dictionary<string, object?> { { "key1", "value1" } },
                    IsAvailable = false,
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
                x.IsAvailable.ShouldBeFalse();
                x.Version.ShouldBe(2);
                x.UpdatedOn.ShouldBe(Later);
            });

            (await Harness.Consumed.Any<HideSellableItemRequest>()).ShouldBeTrue();
        });
    }

    public class When_hiding_empty_id : SellableItemCommandService_Hide
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

            (await Harness.Consumed.Any<HideSellableItemRequest>()).ShouldBeFalse();
        });
    }

    public class When_hiding_not_found : SellableItemCommandService_Hide
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<HideSellableItemRequest>(context =>
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

            (await Harness.Consumed.Any<HideSellableItemRequest>()).ShouldBeTrue();
        });
    }

    public class When_hiding_in_discontinued_state : SellableItemCommandService_Hide
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<HideSellableItemRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw new SellableItemStateException("Discontinued", "hide");
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<SellableItemStateException>();
            LastException.Message.ShouldContain("Discontinued");
            LastException.Message.ShouldContain("hide");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<HideSellableItemRequest>()).ShouldBeTrue();
        });
    }

    public class When_hiding_generic_fault : SellableItemCommandService_Hide
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<HideSellableItemRequest>(context =>
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

            (await Harness.Consumed.Any<HideSellableItemRequest>()).ShouldBeTrue();
        });
    }

    public class When_hiding_timeout : SellableItemCommandService_Hide
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<HideSellableItemRequest>(async context =>
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

            (await Harness.Consumed.Any<HideSellableItemRequest>()).ShouldBeTrue();
        });
    }
}
