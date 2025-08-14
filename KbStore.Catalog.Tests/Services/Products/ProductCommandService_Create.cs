namespace KbStore.Catalog.Tests.Services.Products;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using Catalog.Services;
using MassTransit;
using NUnit.Framework;
using Shouldly;


public abstract class ProductCommandService_Create : CommandService_Tests<MassTransitProductCommandService>
{
    protected ProductModel? Result;

    protected string Sku = null!;
    protected string Name = null!;
    protected ProductDimensions? Dimensions;
    protected Guid? InventoryId;
    protected int? StockThreshold;
    protected TimeSpan? LeadTime;

    protected override void Arrange()
    {
        base.Arrange();

        Sku = "TEST_SKU_123";
        Name = "Test Product";
        Dimensions = new ProductDimensions { Width = 10, Height = 5, Length = 15, Weight = 2.5m };
        InventoryId = null;
        StockThreshold = null;
        LeadTime = null;
    }

    protected override async Task Act()
    {
        Result = await Subject.CreateAsync(Sku, Name, Dimensions, InventoryId, StockThreshold, LeadTime);
    }

    public class When_creating_valid_product_without_inventory : ProductCommandService_Create
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<CreateProductRequest>(async context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                await context.RespondAsync<CreateProductResponse>(new
                {
                    ProductId = ExistingId,
                    context.Message.Sku,
                    context.Message.Name,
                    context.Message.Dimensions,
                    InventoryId = (Guid?)null,
                    context.Message.StockThreshold,
                    context.Message.LeadTime,
                    IsStocked = true,
                    IsEnabled = true,
                    IsAvailable = true,
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
                x.ProductId.ShouldBe(ExistingId);
                x.Sku.ShouldBe("TEST_SKU_123");
                x.Name.ShouldBe("Test Product");
                x.Dimensions.ShouldNotBeNull();
                x.Dimensions!.Value.Width.ShouldBe(10);
                x.Dimensions!.Value.Height.ShouldBe(5);
                x.Dimensions!.Value.Length.ShouldBe(15);
                x.Dimensions!.Value.Weight.ShouldBe(2.5m);
                x.InventoryId.ShouldBeNull();
                x.IsStocked.ShouldBeTrue();
                x.IsEnabled.ShouldBeTrue();
                x.IsAvailable.ShouldBeTrue();
                x.CreatedOn.ShouldBe(Now);
                x.UpdatedOn.ShouldBe(Now);
            });

            (await Harness.Consumed.Any<CreateProductRequest>()).ShouldBeTrue();
        });
    }

    public class When_creating_valid_product_with_inventory : ProductCommandService_Create
    {
        protected override void Arrange()
        {
            base.Arrange();
            InventoryId = LinkedId;
            StockThreshold = 10;
            LeadTime = TimeSpan.FromDays(7);
        }

        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<CreateProductRequest>(async context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                await context.RespondAsync<CreateProductResponse>(new
                {
                    ProductId = ExistingId,
                    context.Message.Sku,
                    context.Message.Name,
                    context.Message.Dimensions,
                    context.Message.InventoryId,
                    context.Message.StockThreshold,
                    context.Message.LeadTime,
                    IsStocked = true,
                    IsEnabled = true,
                    IsAvailable = true,
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
                x.ProductId.ShouldBe(ExistingId);
                x.Sku.ShouldBe("TEST_SKU_123");
                x.Name.ShouldBe("Test Product");
                x.InventoryId.ShouldBe(LinkedId);
                x.StockThreshold.ShouldBe(10);
                x.LeadTime.ShouldBe(TimeSpan.FromDays(7));
                x.IsStocked.ShouldBeTrue();
                x.IsEnabled.ShouldBeTrue();
                x.IsAvailable.ShouldBeTrue();
            });

            (await Harness.Consumed.Any<CreateProductRequest>()).ShouldBeTrue();
        });
    }

    public class When_creating_product_null_sku : ProductCommandService_Create
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
            LastException.ShouldBeOfType<ProductValidationException>();
            LastException.Message.ShouldContain("Sku must have a value");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<CreateProductRequest>()).ShouldBeFalse();
        });
    }

    public class When_creating_product_empty_sku : ProductCommandService_Create
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
            LastException.ShouldBeOfType<ProductValidationException>();
            LastException.Message.ShouldContain("Sku must have a value");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<CreateProductRequest>()).ShouldBeFalse();
        });
    }

    public class When_creating_product_whitespace_sku : ProductCommandService_Create
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
            LastException.ShouldBeOfType<ProductValidationException>();
            LastException.Message.ShouldContain("Sku must have a value");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<CreateProductRequest>()).ShouldBeFalse();
        });
    }

    public class When_creating_product_null_name : ProductCommandService_Create
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
            LastException.ShouldBeOfType<ProductValidationException>();
            LastException.Message.ShouldContain("Name must have a value");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<CreateProductRequest>()).ShouldBeFalse();
        });
    }

    public class When_creating_product_empty_name : ProductCommandService_Create
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
            LastException.ShouldBeOfType<ProductValidationException>();
            LastException.Message.ShouldContain("Name must have a value");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<CreateProductRequest>()).ShouldBeFalse();
        });
    }

    public class When_creating_product_invalid_leadtime : ProductCommandService_Create
    {
        protected override void Arrange()
        {
            base.Arrange();
            LeadTime = TimeSpan.FromSeconds(-1);
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<ProductValidationException>();
            LastException.Message.ShouldContain("lead");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<CreateProductRequest>()).ShouldBeFalse();
        });
    }

    public class When_creating_product_duplicate_sku : ProductCommandService_Create
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<CreateProductRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw ProductConflictException.DuplicateSku("TEST_SKU_123");
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<ProductConflictException>();
            LastException.Message.ShouldContain("sku");
            LastException.Message.ShouldContain("TEST_SKU_123");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<CreateProductRequest>()).ShouldBeTrue();
        });
    }

    public class When_creating_product_generic_fault : ProductCommandService_Create
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<CreateProductRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw new GenericProductException("Unknown product operation failed");
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<GenericProductException>();
            LastException.Message.ShouldContain("Unknown product operation failed");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<CreateProductRequest>()).ShouldBeTrue();
        });
    }

    public class When_creating_product_timeout : ProductCommandService_Create
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<CreateProductRequest>(async context =>
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

            (await Harness.Consumed.Any<CreateProductRequest>()).ShouldBeTrue();
        });
    }
}