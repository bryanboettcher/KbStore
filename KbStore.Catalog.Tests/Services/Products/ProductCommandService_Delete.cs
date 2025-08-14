namespace KbStore.Catalog.Tests.Services.Products;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using Catalog.Services;
using MassTransit;
using NUnit.Framework;
using Shouldly;


public abstract class ProductCommandService_Delete : CommandService_Tests<MassTransitProductCommandService>
{
    protected ProductModel? Result;

    protected Guid ProductId;

    protected override void Arrange()
    {
        base.Arrange();

        ProductId = ExistingId;
    }

    protected override async Task Act()
    {
        Result = await Subject.DeleteAsync(ProductId);
    }

    public class When_deleting_successfully : ProductCommandService_Delete
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<DeleteProductRequest>(async context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                await context.RespondAsync<DeleteProductResponse>(new
                {
                    ProductId = context.Message.ProductId,
                    Sku = "TEST_SKU_123",
                    Name = "Test Product",
                    Dimensions = (ProductDimensions?)null,
                    InventoryId = (Guid?)null,
                    StockThreshold = (int?)null,
                    LeadTime = (TimeSpan?)null,
                    IsStocked = false,
                    IsEnabled = false,
                    IsAvailable = false,
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
                x.IsEnabled.ShouldBeFalse();
                x.IsAvailable.ShouldBeFalse();
                x.UpdatedOn.ShouldBe(Later);
            });

            (await Harness.Consumed.Any<DeleteProductRequest>()).ShouldBeTrue();
        });
    }

    public class When_deleting_empty_product_id : ProductCommandService_Delete
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

            (await Harness.Consumed.Any<DeleteProductRequest>()).ShouldBeFalse();
        });
    }

    public class When_deleting_nonexistent_product : ProductCommandService_Delete
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<DeleteProductRequest>(context =>
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

            (await Harness.Consumed.Any<DeleteProductRequest>()).ShouldBeTrue();
        });
    }

    public class When_deleting_generic_fault : ProductCommandService_Delete
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<DeleteProductRequest>(context =>
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

            (await Harness.Consumed.Any<DeleteProductRequest>()).ShouldBeTrue();
        });
    }

    public class When_deleting_timeout : ProductCommandService_Delete
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<DeleteProductRequest>(async context =>
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

            (await Harness.Consumed.Any<DeleteProductRequest>()).ShouldBeTrue();
        });
    }
}