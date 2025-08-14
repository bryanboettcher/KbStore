namespace KbStore.Catalog.Tests.Services.Products;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using Catalog.Services;
using MassTransit;
using NUnit.Framework;
using Shouldly;


public abstract class ProductCommandService_UpdateDimensions : CommandService_Tests<MassTransitProductCommandService>
{
    protected ProductModel? Result;

    protected Guid ProductId;
    protected ProductDimensions? Dimensions;

    protected override void Arrange()
    {
        base.Arrange();

        ProductId = ExistingId;
        Dimensions = new ProductDimensions { Width = 20, Height = 10, Length = 30, Weight = 5.0m };
    }

    protected override async Task Act()
    {
        Result = await Subject.UpdateDimensionsAsync(ProductId, Dimensions);
    }

    public class When_updating_dimensions_successfully : ProductCommandService_UpdateDimensions
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductDimensionsRequest>(async context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                await context.RespondAsync<UpdateProductResponse>(new
                {
                    ProductId = context.Message.ProductId,
                    Sku = "TEST_SKU_123",
                    Name = "Test Product",
                    Dimensions = context.Message.Dimensions,
                    InventoryId = (Guid?)null,
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
                x.Dimensions.ShouldNotBeNull();
                x.Dimensions!.Value.Width.ShouldBe(20);
                x.Dimensions!.Value.Height.ShouldBe(10);
                x.Dimensions!.Value.Length.ShouldBe(30);
                x.Dimensions!.Value.Weight.ShouldBe(5.0m);
                x.UpdatedOn.ShouldBe(Later);
            });

            (await Harness.Consumed.Any<UpdateProductDimensionsRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_dimensions_to_null : ProductCommandService_UpdateDimensions
    {
        protected override void Arrange()
        {
            base.Arrange();
            Dimensions = null;
        }

        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductDimensionsRequest>(async context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                await context.RespondAsync<UpdateProductResponse>(new
                {
                    ProductId = context.Message.ProductId,
                    Sku = "TEST_SKU_123",
                    Name = "Test Product",
                    Dimensions = (ProductDimensions?)null,
                    InventoryId = (Guid?)null,
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
                x.Dimensions.ShouldBeNull();
                x.UpdatedOn.ShouldBe(Later);
            });

            (await Harness.Consumed.Any<UpdateProductDimensionsRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_dimensions_empty_product_id : ProductCommandService_UpdateDimensions
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

            (await Harness.Consumed.Any<UpdateProductDimensionsRequest>()).ShouldBeFalse();
        });
    }

    public class When_updating_dimensions_nonexistent_product : ProductCommandService_UpdateDimensions
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductDimensionsRequest>(context =>
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

            (await Harness.Consumed.Any<UpdateProductDimensionsRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_dimensions_discontinued_product : ProductCommandService_UpdateDimensions
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductDimensionsRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw ProductStateException.CannotModifyDiscontinuedProduct(context.Message.ProductId, "UpdateDimensions");
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<ProductStateException>();
            LastException.Message.ShouldContain("Discontinued");
            LastException.Message.ShouldContain("UpdateDimensions");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<UpdateProductDimensionsRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_dimensions_generic_fault : ProductCommandService_UpdateDimensions
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductDimensionsRequest>(context =>
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

            (await Harness.Consumed.Any<UpdateProductDimensionsRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_dimensions_timeout : ProductCommandService_UpdateDimensions
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductDimensionsRequest>(async context =>
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

            (await Harness.Consumed.Any<UpdateProductDimensionsRequest>()).ShouldBeTrue();
        });
    }
}