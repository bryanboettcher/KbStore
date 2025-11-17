namespace KbStore.Storefront.Tests.Services.SellableItems;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using KbStore.Storefront.Services;
using MassTransit;
using NUnit.Framework;
using Shouldly;


public abstract class SellableItemCommandService_UpdatePayload : CommandService_Tests<SellableItemCommandService>
{
    protected SellableItemModel? Result;

    protected Guid SellableItemId;
    protected IReadOnlyDictionary<string, object?> Payload = null!;

    protected override void Arrange()
    {
        base.Arrange();

        SellableItemId = ExistingId;
        Payload = new Dictionary<string, object?> { { "updatedKey", "updatedValue" } };
    }

    protected override async Task Act()
    {
        Result = await Subject.UpdatePayloadAsync(SellableItemId, Payload);
    }

    public class When_updating_payload_valid : SellableItemCommandService_UpdatePayload
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateSellableItemPayloadRequest>(async context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                await context.RespondAsync<UpdateSellableItemResponse>(new
                {
                    SellableItemId = ExistingId,
                    ProductId = (Guid?)null,
                    Sku = "TEST_SKU",
                    Name = "Test Item",
                    Description = "A test item",
                    BasePrice = 19.99m,
                    ItemType = "Standard",
                    Payload = context.Message.Payload,
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
                x.Payload.ShouldContainKey("updatedKey");
                x.Payload["updatedKey"].ShouldBe("updatedValue");
                x.Version.ShouldBe(2);
                x.UpdatedOn.ShouldBe(Later);
            });

            (await Harness.Consumed.Any<UpdateSellableItemPayloadRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_payload_empty_id : SellableItemCommandService_UpdatePayload
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

            (await Harness.Consumed.Any<UpdateSellableItemPayloadRequest>()).ShouldBeFalse();
        });
    }

    public class When_updating_payload_not_found : SellableItemCommandService_UpdatePayload
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateSellableItemPayloadRequest>(context =>
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

            (await Harness.Consumed.Any<UpdateSellableItemPayloadRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_payload_in_discontinued_state : SellableItemCommandService_UpdatePayload
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateSellableItemPayloadRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw new SellableItemStateException("Discontinued", "update payload");
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<SellableItemStateException>();
            LastException.Message.ShouldContain("Discontinued");
            LastException.Message.ShouldContain("update payload");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<UpdateSellableItemPayloadRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_payload_generic_fault : SellableItemCommandService_UpdatePayload
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateSellableItemPayloadRequest>(context =>
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

            (await Harness.Consumed.Any<UpdateSellableItemPayloadRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_payload_timeout : SellableItemCommandService_UpdatePayload
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateSellableItemPayloadRequest>(async context =>
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

            (await Harness.Consumed.Any<UpdateSellableItemPayloadRequest>()).ShouldBeTrue();
        });
    }
}
