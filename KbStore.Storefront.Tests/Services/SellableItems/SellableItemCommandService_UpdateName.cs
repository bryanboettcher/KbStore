namespace KbStore.Storefront.Tests.Services.SellableItems;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using KbStore.Storefront.Services;
using MassTransit;
using NUnit.Framework;
using Shouldly;


public abstract class SellableItemCommandService_UpdateName : CommandService_Tests<SellableItemCommandService>
{
    protected SellableItemModel? Result;

    protected Guid SellableItemId;
    protected string Name = null!;

    protected override void Arrange()
    {
        base.Arrange();

        SellableItemId = ExistingId;
        Name = "Updated Name";
    }

    protected override async Task Act()
    {
        Result = await Subject.UpdateNameAsync(SellableItemId, Name);
    }

    public class When_updating_name_valid : SellableItemCommandService_UpdateName
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateSellableItemNameRequest>(async context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                await context.RespondAsync<UpdateSellableItemResponse>(new
                {
                    SellableItemId = ExistingId,
                    ProductId = (Guid?)null,
                    Sku = "TEST_SKU",
                    Name = context.Message.Name,
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
                x.Name.ShouldBe("Updated Name");
                x.Version.ShouldBe(2);
                x.UpdatedOn.ShouldBe(Later);
            });

            (await Harness.Consumed.Any<UpdateSellableItemNameRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_name_empty_id : SellableItemCommandService_UpdateName
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

            (await Harness.Consumed.Any<UpdateSellableItemNameRequest>()).ShouldBeFalse();
        });
    }

    public class When_updating_name_null_name : SellableItemCommandService_UpdateName
    {
        protected override void Arrange()
        {
            base.Arrange();
            Name = null!;
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<SellableItemValidationException>();
            LastException.Message.ShouldContain("Name must have a value");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<UpdateSellableItemNameRequest>()).ShouldBeFalse();
        });
    }

    public class When_updating_name_empty_name : SellableItemCommandService_UpdateName
    {
        protected override void Arrange()
        {
            base.Arrange();
            Name = "";
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<SellableItemValidationException>();
            LastException.Message.ShouldContain("Name must have a value");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<UpdateSellableItemNameRequest>()).ShouldBeFalse();
        });
    }

    public class When_updating_name_not_found : SellableItemCommandService_UpdateName
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateSellableItemNameRequest>(context =>
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

            (await Harness.Consumed.Any<UpdateSellableItemNameRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_name_in_discontinued_state : SellableItemCommandService_UpdateName
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateSellableItemNameRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw new SellableItemStateException("Discontinued", "update name");
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<SellableItemStateException>();
            LastException.Message.ShouldContain("Discontinued");
            LastException.Message.ShouldContain("update name");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<UpdateSellableItemNameRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_name_generic_fault : SellableItemCommandService_UpdateName
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateSellableItemNameRequest>(context =>
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

            (await Harness.Consumed.Any<UpdateSellableItemNameRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_name_timeout : SellableItemCommandService_UpdateName
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateSellableItemNameRequest>(async context =>
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

            (await Harness.Consumed.Any<UpdateSellableItemNameRequest>()).ShouldBeTrue();
        });
    }
}
