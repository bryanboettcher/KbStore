namespace KbStore.Storefront.Tests.Services.SellableItems;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using KbStore.Storefront.Services;
using MassTransit;
using NUnit.Framework;
using Shouldly;


public abstract class SellableItemCommandService_Delete : CommandService_Tests<SellableItemCommandService>
{
    protected Guid SellableItemId;

    protected override void Arrange()
    {
        base.Arrange();

        SellableItemId = ExistingId;
    }

    protected override async Task Act()
    {
        await Subject.DeleteAsync(SellableItemId);
    }

    public class When_deleting_valid : SellableItemCommandService_Delete
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<DeleteSellableItemRequest>(async context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                await context.RespondAsync<DeleteSellableItemResponse>(new
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

            (await Harness.Consumed.Any<DeleteSellableItemRequest>()).ShouldBeTrue();
        });
    }

    public class When_deleting_empty_id : SellableItemCommandService_Delete
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

            (await Harness.Consumed.Any<DeleteSellableItemRequest>()).ShouldBeFalse();
        });
    }

    public class When_deleting_not_found : SellableItemCommandService_Delete
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<DeleteSellableItemRequest>(context =>
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

            (await Harness.Consumed.Any<DeleteSellableItemRequest>()).ShouldBeTrue();
        });
    }

    public class When_deleting_generic_fault : SellableItemCommandService_Delete
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<DeleteSellableItemRequest>(context =>
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

            (await Harness.Consumed.Any<DeleteSellableItemRequest>()).ShouldBeTrue();
        });
    }

    public class When_deleting_timeout : SellableItemCommandService_Delete
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<DeleteSellableItemRequest>(async context =>
            {
                await Task.Delay(1000, context.CancellationToken);
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<RequestTimeoutException>();

            (await Harness.Consumed.Any<DeleteSellableItemRequest>()).ShouldBeTrue();
        });
    }
}
