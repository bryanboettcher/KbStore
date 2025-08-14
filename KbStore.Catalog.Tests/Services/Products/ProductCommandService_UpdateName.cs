namespace KbStore.Catalog.Tests.Services.Products;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using Catalog.Services;
using MassTransit;
using NUnit.Framework;
using Shouldly;


public abstract class ProductCommandService_UpdateName : CommandService_Tests<MassTransitProductCommandService>
{
    protected ProductModel? Result;

    protected Guid ProductId;
    protected string Name = null!;

    protected override void Arrange()
    {
        base.Arrange();

        ProductId = ExistingId;
        Name = "Updated Product Name";
    }

    protected override async Task Act()
    {
        Result = await Subject.UpdateNameAsync(ProductId, Name);
    }

    public class When_updating_name_successfully : ProductCommandService_UpdateName
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductNameRequest>(async context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                await context.RespondAsync<UpdateProductResponse>(new
                {
                    ProductId = context.Message.ProductId,
                    Sku = "TEST_SKU_123",
                    Name = context.Message.Name,
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
                x.Name.ShouldBe("Updated Product Name");
                x.UpdatedOn.ShouldBe(Later);
            });

            (await Harness.Consumed.Any<UpdateProductNameRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_name_empty_product_id : ProductCommandService_UpdateName
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

            (await Harness.Consumed.Any<UpdateProductNameRequest>()).ShouldBeFalse();
        });
    }

    public class When_updating_name_null_name : ProductCommandService_UpdateName
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
            LastException.Message.ShouldContain("Name");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<UpdateProductNameRequest>()).ShouldBeFalse();
        });
    }

    public class When_updating_name_empty_name : ProductCommandService_UpdateName
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
            LastException.Message.ShouldContain("Name");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<UpdateProductNameRequest>()).ShouldBeFalse();
        });
    }

    public class When_updating_name_nonexistent_product : ProductCommandService_UpdateName
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductNameRequest>(context =>
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

            (await Harness.Consumed.Any<UpdateProductNameRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_name_discontinued_product : ProductCommandService_UpdateName
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductNameRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw ProductStateException.CannotModifyDiscontinuedProduct(context.Message.ProductId, "UpdateName");
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<ProductStateException>();
            LastException.Message.ShouldContain("Discontinued");
            LastException.Message.ShouldContain("UpdateName");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<UpdateProductNameRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_name_generic_fault : ProductCommandService_UpdateName
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductNameRequest>(context =>
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

            (await Harness.Consumed.Any<UpdateProductNameRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_name_timeout : ProductCommandService_UpdateName
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductNameRequest>(async context =>
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

            (await Harness.Consumed.Any<UpdateProductNameRequest>()).ShouldBeTrue();
        });
    }
}