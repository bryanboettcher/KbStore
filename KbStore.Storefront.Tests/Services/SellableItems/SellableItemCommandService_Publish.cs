namespace KbStore.Storefront.Tests.Services.SellableItems;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using KbStore.Storefront.Services;
using MassTransit;
using NUnit.Framework;
using Shouldly;


public abstract class SellableItemCommandService_Publish : CommandService_Tests<SellableItemCommandService>
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
        Result = await Subject.PublishAsync(SellableItemId);
    }

    public class When_publishing_valid : SellableItemCommandService_Publish
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<PublishSellableItemRequest>(async context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                await context.RespondAsync<PublishSellableItemResponse>(new
                {
                    SellableItemId = ExistingId,
                    ProductId = (Guid?)null,
                    Sku = "TEST_SKU",
                    Name = "Test Item",
                    Description = "A test item",
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
                x.Version.ShouldBe(2);
                x.UpdatedOn.ShouldBe(Later);
            });

            (await Harness.Consumed.Any<PublishSellableItemRequest>()).ShouldBeTrue();
        });
    }

    public class When_publishing_empty_id : SellableItemCommandService_Publish
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

            (await Harness.Consumed.Any<PublishSellableItemRequest>()).ShouldBeFalse();
        });
    }

    public class When_publishing_not_found : SellableItemCommandService_Publish
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<PublishSellableItemRequest>(context =>
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

            (await Harness.Consumed.Any<PublishSellableItemRequest>()).ShouldBeTrue();
        });
    }

    public class When_publishing_invalid_state : SellableItemCommandService_Publish
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<PublishSellableItemRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw new SellableItemStateException("Discontinued", "publish");
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<SellableItemStateException>();
            LastException.Message.ShouldContain("Discontinued");
            LastException.Message.ShouldContain("publish");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<PublishSellableItemRequest>()).ShouldBeTrue();
        });
    }

    public class When_publishing_generic_fault : SellableItemCommandService_Publish
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<PublishSellableItemRequest>(context =>
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

            (await Harness.Consumed.Any<PublishSellableItemRequest>()).ShouldBeTrue();
        });
    }

    public class When_publishing_timeout : SellableItemCommandService_Publish
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<PublishSellableItemRequest>(async context =>
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

            (await Harness.Consumed.Any<PublishSellableItemRequest>()).ShouldBeTrue();
        });
    }
}
