namespace KbStore.Catalog.Tests.Services.Products;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using Catalog.Services;
using MassTransit;
using NUnit.Framework;
using Shouldly;


public abstract class ProductCommandService_UpdateStockThreshold : CommandService_Tests<MassTransitProductCommandService>
{
    protected ProductModel? Result;

    protected Guid ProductId;
    protected int? StockThreshold;

    protected override void Arrange()
    {
        base.Arrange();

        ProductId = ExistingId;
        StockThreshold = 25;
    }

    protected override async Task Act()
    {
        Result = await Subject.UpdateStockThresholdAsync(ProductId, StockThreshold);
    }

    public class When_updating_stock_threshold_successfully : ProductCommandService_UpdateStockThreshold
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductStockThresholdRequest>(async context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                await context.RespondAsync<UpdateProductResponse>(new
                {
                    ProductId = context.Message.ProductId,
                    Sku = "TEST_SKU_123",
                    Name = "Test Product",
                    Dimensions = (ProductDimensions?)null,
                    InventoryId = LinkedId,
                    StockThreshold = context.Message.StockThreshold,
                    LeadTime = (TimeSpan?)null,
                    IsStocked = true,
                    IsEnabled = true,
                    IsAvailable = true,
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
                x.ProductId.ShouldBe(ExistingId);
                x.StockThreshold.ShouldBe(25);
                x.UpdatedOn.ShouldBe(Later);
            });

            (await Harness.Consumed.Any<UpdateProductStockThresholdRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_stock_threshold_to_null : ProductCommandService_UpdateStockThreshold
    {
        protected override void Arrange()
        {
            base.Arrange();
            StockThreshold = null;
        }

        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductStockThresholdRequest>(async context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                await context.RespondAsync<UpdateProductResponse>(new
                {
                    ProductId = context.Message.ProductId,
                    Sku = "TEST_SKU_123",
                    Name = "Test Product",
                    Dimensions = (ProductDimensions?)null,
                    InventoryId = LinkedId,
                    StockThreshold = (int?)null,
                    LeadTime = (TimeSpan?)null,
                    IsStocked = true,
                    IsEnabled = true,
                    IsAvailable = true,
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
                x.ProductId.ShouldBe(ExistingId);
                x.StockThreshold.ShouldBeNull();
                x.UpdatedOn.ShouldBe(Later);
            });

            (await Harness.Consumed.Any<UpdateProductStockThresholdRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_stock_threshold_zero : ProductCommandService_UpdateStockThreshold
    {
        protected override void Arrange()
        {
            base.Arrange();
            StockThreshold = 0;
        }

        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductStockThresholdRequest>(async context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                await context.RespondAsync<UpdateProductResponse>(new
                {
                    ProductId = context.Message.ProductId,
                    Sku = "TEST_SKU_123",
                    Name = "Test Product",
                    Dimensions = (ProductDimensions?)null,
                    InventoryId = LinkedId,
                    StockThreshold = context.Message.StockThreshold,
                    LeadTime = (TimeSpan?)null,
                    IsStocked = true,
                    IsEnabled = true,
                    IsAvailable = true,
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
            Result.StockThreshold.ShouldBe(0);

            (await Harness.Consumed.Any<UpdateProductStockThresholdRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_stock_threshold_negative : ProductCommandService_UpdateStockThreshold
    {
        protected override void Arrange()
        {
            base.Arrange();
            StockThreshold = -5;
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<ProductValidationException>();
            LastException.Message.ShouldContain("threshold");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<UpdateProductStockThresholdRequest>()).ShouldBeFalse();
        });
    }

    public class When_updating_stock_threshold_empty_product_id : ProductCommandService_UpdateStockThreshold
    {
        protected override void Arrange()
        {
            base.Arrange();
            ProductId = Guid.Empty;
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<ArgumentException>();
            LastException.Message.ShouldContain("ProductId must be set");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<UpdateProductStockThresholdRequest>()).ShouldBeFalse();
        });
    }

    public class When_updating_stock_threshold_nonexistent_product : ProductCommandService_UpdateStockThreshold
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductStockThresholdRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw new ProductNotFoundException(context.Message.ProductId);
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<ProductNotFoundException>();
            LastException.Message.ShouldContain("was not found");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<UpdateProductStockThresholdRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_stock_threshold_discontinued_product : ProductCommandService_UpdateStockThreshold
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductStockThresholdRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw ProductStateException.CannotModifyDiscontinuedProduct(context.Message.ProductId, "UpdateStockThreshold");
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<ProductStateException>();
            LastException.Message.ShouldContain("Discontinued");
            LastException.Message.ShouldContain("UpdateStockThreshold");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<UpdateProductStockThresholdRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_stock_threshold_generic_fault : ProductCommandService_UpdateStockThreshold
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductStockThresholdRequest>(context =>
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

            (await Harness.Consumed.Any<UpdateProductStockThresholdRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_stock_threshold_timeout : ProductCommandService_UpdateStockThreshold
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductStockThresholdRequest>(async context =>
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

            (await Harness.Consumed.Any<UpdateProductStockThresholdRequest>()).ShouldBeTrue();
        });
    }
}