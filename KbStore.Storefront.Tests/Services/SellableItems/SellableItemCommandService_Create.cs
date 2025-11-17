namespace KbStore.Storefront.Tests.Services.SellableItems;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using KbStore.Storefront.Services;
using MassTransit;
using NUnit.Framework;
using Shouldly;


public abstract class SellableItemCommandService_Create : CommandService_Tests<SellableItemCommandService>
{
    protected SellableItemModel? Result;

    protected string Sku = null!;
    protected string Name = null!;
    protected string? Description;
    protected decimal BasePrice;
    protected string ItemType = null!;
    protected IReadOnlyDictionary<string, object?> Payload = null!;
    protected Guid? ProductId;

    protected override void Arrange()
    {
        base.Arrange();

        Sku = "TEST_SKU_123";
        Name = "Test Sellable Item";
        Description = "A test item";
        BasePrice = 19.99m;
        ItemType = "Standard";
        Payload = new Dictionary<string, object?> { { "key1", "value1" } };
        ProductId = null;
    }

    protected override async Task Act()
    {
        Result = await Subject.CreateAsync(Sku, Name, Description, BasePrice, ItemType, Payload, ProductId);
    }

    public class When_creating_valid_sellable_item : SellableItemCommandService_Create
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<CreateSellableItemRequest>(async context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                await context.RespondAsync<CreateSellableItemResponse>(new
                {
                    SellableItemId = ExistingId,
                    context.Message.ProductId,
                    context.Message.Sku,
                    context.Message.Name,
                    context.Message.Description,
                    context.Message.BasePrice,
                    context.Message.ItemType,
                    context.Message.Payload,
                    IsAvailable = true,
                    Version = 1,
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
                x.SellableItemId.ShouldBe(ExistingId);
                x.Sku.ShouldBe("TEST_SKU_123");
                x.Name.ShouldBe("Test Sellable Item");
                x.Description.ShouldBe("A test item");
                x.BasePrice.ShouldBe(19.99m);
                x.ItemType.ShouldBe("Standard");
                x.IsAvailable.ShouldBeTrue();
                x.Version.ShouldBe(1);
                x.CreatedOn.ShouldBe(Now);
                x.UpdatedOn.ShouldBe(Now);
            });

            (await Harness.Consumed.Any<CreateSellableItemRequest>()).ShouldBeTrue();
        });
    }

    public class When_creating_sellable_item_null_sku : SellableItemCommandService_Create
    {
        protected override void Arrange()
        {
            base.Arrange();
            Sku = null!;
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<SellableItemValidationException>();
            LastException.Message.ShouldContain("SKU must have a value");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<CreateSellableItemRequest>()).ShouldBeFalse();
        });
    }

    public class When_creating_sellable_item_empty_sku : SellableItemCommandService_Create
    {
        protected override void Arrange()
        {
            base.Arrange();
            Sku = "";
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<SellableItemValidationException>();
            LastException.Message.ShouldContain("SKU must have a value");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<CreateSellableItemRequest>()).ShouldBeFalse();
        });
    }

    public class When_creating_sellable_item_whitespace_sku : SellableItemCommandService_Create
    {
        protected override void Arrange()
        {
            base.Arrange();
            Sku = "   ";
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<SellableItemValidationException>();
            LastException.Message.ShouldContain("SKU must have a value");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<CreateSellableItemRequest>()).ShouldBeFalse();
        });
    }

    public class When_creating_sellable_item_null_name : SellableItemCommandService_Create
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

            (await Harness.Consumed.Any<CreateSellableItemRequest>()).ShouldBeFalse();
        });
    }

    public class When_creating_sellable_item_empty_name : SellableItemCommandService_Create
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

            (await Harness.Consumed.Any<CreateSellableItemRequest>()).ShouldBeFalse();
        });
    }

    public class When_creating_sellable_item_negative_price : SellableItemCommandService_Create
    {
        protected override void Arrange()
        {
            base.Arrange();
            BasePrice = -10m;
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<SellableItemValidationException>();
            LastException.Message.ShouldContain("Base price cannot be negative");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<CreateSellableItemRequest>()).ShouldBeFalse();
        });
    }

    public class When_creating_sellable_item_null_item_type : SellableItemCommandService_Create
    {
        protected override void Arrange()
        {
            base.Arrange();
            ItemType = null!;
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<SellableItemValidationException>();
            LastException.Message.ShouldContain("Item type must have a value");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<CreateSellableItemRequest>()).ShouldBeFalse();
        });
    }

    public class When_creating_sellable_item_duplicate_sku : SellableItemCommandService_Create
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<CreateSellableItemRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw SellableItemConflictException.DuplicateSku("TEST_SKU_123");
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<SellableItemConflictException>();
            LastException.Message.ShouldContain("sku");
            LastException.Message.ShouldContain("TEST_SKU_123");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<CreateSellableItemRequest>()).ShouldBeTrue();
        });
    }

    public class When_creating_sellable_item_generic_fault : SellableItemCommandService_Create
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<CreateSellableItemRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw new GenericSellableItemException("Unknown sellable item operation failed");
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<GenericSellableItemException>();
            LastException.Message.ShouldContain("Unknown sellable item operation failed");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<CreateSellableItemRequest>()).ShouldBeTrue();
        });
    }

    public class When_creating_sellable_item_timeout : SellableItemCommandService_Create
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<CreateSellableItemRequest>(async context =>
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

            (await Harness.Consumed.Any<CreateSellableItemRequest>()).ShouldBeTrue();
        });
    }
}
