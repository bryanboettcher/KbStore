namespace KbStore.Catalog.Tests.Services.Products;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using Catalog.Services;
using MassTransit;
using NUnit.Framework;
using Shouldly;


public abstract class ProductCommandService_UpdateLeadTime : CommandService_Tests<MassTransitProductCommandService>
{
    protected ProductModel? Result;

    protected Guid ProductId;
    protected TimeSpan? LeadTime;

    protected override void Arrange()
    {
        base.Arrange();

        ProductId = ExistingId;
        LeadTime = TimeSpan.FromDays(14);
    }

    protected override async Task Act()
    {
        Result = await Subject.UpdateLeadTimeAsync(ProductId, LeadTime);
    }

    public class When_updating_lead_time_successfully : ProductCommandService_UpdateLeadTime
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductLeadTimeRequest>(async context =>
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
                    LeadTime = context.Message.LeadTime,
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
                x.LeadTime.ShouldBe(TimeSpan.FromDays(14));
                x.UpdatedOn.ShouldBe(Later);
            });

            (await Harness.Consumed.Any<UpdateProductLeadTimeRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_lead_time_to_null : ProductCommandService_UpdateLeadTime
    {
        protected override void Arrange()
        {
            base.Arrange();
            LeadTime = null;
        }

        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductLeadTimeRequest>(async context =>
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
                x.LeadTime.ShouldBeNull();
                x.UpdatedOn.ShouldBe(Later);
            });

            (await Harness.Consumed.Any<UpdateProductLeadTimeRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_lead_time_zero : ProductCommandService_UpdateLeadTime
    {
        protected override void Arrange()
        {
            base.Arrange();
            LeadTime = TimeSpan.Zero;
        }

        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductLeadTimeRequest>(async context =>
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
                    LeadTime = context.Message.LeadTime,
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
            Result.LeadTime.ShouldBe(TimeSpan.Zero);

            (await Harness.Consumed.Any<UpdateProductLeadTimeRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_lead_time_negative : ProductCommandService_UpdateLeadTime
    {
        protected override void Arrange()
        {
            base.Arrange();
            LeadTime = TimeSpan.FromDays(-1);
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<ProductValidationException>();
            LastException.Message.ShouldContain("Lead time");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<UpdateProductLeadTimeRequest>()).ShouldBeFalse();
        });
    }

    public class When_updating_lead_time_empty_product_id : ProductCommandService_UpdateLeadTime
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

            (await Harness.Consumed.Any<UpdateProductLeadTimeRequest>()).ShouldBeFalse();
        });
    }

    public class When_updating_lead_time_nonexistent_product : ProductCommandService_UpdateLeadTime
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductLeadTimeRequest>(context =>
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

            (await Harness.Consumed.Any<UpdateProductLeadTimeRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_lead_time_discontinued_product : ProductCommandService_UpdateLeadTime
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductLeadTimeRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                throw ProductStateException.CannotModifyDiscontinuedProduct(context.Message.ProductId, "UpdateLeadTime");
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<ProductStateException>();
            LastException.Message.ShouldContain("Discontinued");
            LastException.Message.ShouldContain("UpdateLeadTime");

            Result.ShouldBeNull();

            (await Harness.Consumed.Any<UpdateProductLeadTimeRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_lead_time_generic_fault : ProductCommandService_UpdateLeadTime
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductLeadTimeRequest>(context =>
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

            (await Harness.Consumed.Any<UpdateProductLeadTimeRequest>()).ShouldBeTrue();
        });
    }

    public class When_updating_lead_time_timeout : ProductCommandService_UpdateLeadTime
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<UpdateProductLeadTimeRequest>(async context =>
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

            (await Harness.Consumed.Any<UpdateProductLeadTimeRequest>()).ShouldBeTrue();
        });
    }
}