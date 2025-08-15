namespace KbStore.Catalog.Tests.Services.Products;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using Catalog.Services;
using MassTransit;
using NUnit.Framework;
using Shouldly;


public abstract class ProductCommandService_UpdateQuantity : CommandService_Tests<MassTransitProductCommandService>
{
    protected ProductModel? Result;

    protected Guid ProductId;
    protected int Quantity;

    protected override void Arrange()
    {
        base.Arrange();

        ProductId = ExistingId;
        Quantity = 3;
    }

    protected override async Task Act()
    {
        Result = await Subject.UpdateQuantityAsync(ProductId, Quantity);
    }

    public class When_updating_quantity_successfully : ProductCommandService_UpdateQuantity
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductQuantityRequest>(async context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                await context.RespondAsync<UpdateProductResponse>(new
                {
                    ProductId = context.Message.ProductId,
                    Sku = "TEST_SKU_123",
                    Name = "Test Product",
                    Dimensions = (ProductDimensions?)null,
                    InventoryId = (Guid?)null,
                    Quantity = context.Message.Quantity,
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
                x.Quantity.ShouldBe(3);
                x.UpdatedOn.ShouldBe(Later);
            });

            (await Harness.Consumed.Any<UpdateProductQuantityRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_quantity_empty_product_id : ProductCommandService_UpdateQuantity
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

            (await Harness.Consumed.Any<UpdateProductQuantityRequest>()).ShouldBeFalse();
        });
    }

    public class When_updating_quantity_zero : ProductCommandService_UpdateQuantity
    {
        protected override void Arrange()
        {
            base.Arrange();
            Quantity = 0;
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<ProductValidationException>();
            LastException.Message.ShouldContain("Quantity must be a positive value");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<UpdateProductQuantityRequest>()).ShouldBeFalse();
        });
    }

    public class When_updating_quantity_negative : ProductCommandService_UpdateQuantity
    {
        protected override void Arrange()
        {
            base.Arrange();
            Quantity = -1;
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<ProductValidationException>();
            LastException.Message.ShouldContain("Quantity must be a positive value");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<UpdateProductQuantityRequest>()).ShouldBeFalse();
        });
    }

    public class When_updating_quantity_nonexistent_product : ProductCommandService_UpdateQuantity
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductQuantityRequest>(context =>
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

            (await Harness.Consumed.Any<UpdateProductQuantityRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_quantity_discontinued_product : ProductCommandService_UpdateQuantity
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductQuantityRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw ProductStateException.CannotModifyDiscontinuedProduct(context.Message.ProductId, "UpdateQuantity");
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<ProductStateException>();
            LastException.Message.ShouldContain("Discontinued");
            LastException.Message.ShouldContain("UpdateQuantity");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<UpdateProductQuantityRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_quantity_generic_fault : ProductCommandService_UpdateQuantity
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductQuantityRequest>(context =>
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

            (await Harness.Consumed.Any<UpdateProductQuantityRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_quantity_timeout : ProductCommandService_UpdateQuantity
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductQuantityRequest>(async context =>
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

            (await Harness.Consumed.Any<UpdateProductQuantityRequest>()).ShouldBeTrue();
        });
    }
}