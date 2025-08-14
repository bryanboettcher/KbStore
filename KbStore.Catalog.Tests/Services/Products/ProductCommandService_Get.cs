using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Services;
using MassTransit;
using NUnit.Framework;


namespace KbStore.Catalog.Tests.Services.Products;

using Abstractions.Exceptions;
using Shouldly;


public abstract class ProductCommandService_Get : CommandService_Tests<MassTransitProductCommandService>
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
        Result = await Subject.GetAsync(ProductId);
    }

    public class When_getting_successfully : ProductCommandService_Get
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<ProductStatusRequest>(async context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                await context.RespondAsync<ProductStatusResponse>(new
                {
                    ProductId = context.Message.ProductId,
                    Sku = "TEST_SKU_123",
                    Name = "Test Product",
                    Dimensions = new ProductDimensions { Width = 10, Height = 5, Length = 15, Weight = 2.5m },
                    InventoryId = LinkedId,
                    StockThreshold = 10,
                    LeadTime = TimeSpan.FromDays(7),
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
                x.InventoryId.ShouldBe(LinkedId);
                x.StockThreshold.ShouldBe(10);
                x.LeadTime.ShouldBe(TimeSpan.FromDays(7));
                x.IsStocked.ShouldBeTrue();
                x.IsEnabled.ShouldBeTrue();
                x.IsAvailable.ShouldBeTrue();
                x.CreatedOn.ShouldBe(Now);
                x.UpdatedOn.ShouldBe(Now);
            });

            (await Harness.Consumed.Any<ProductStatusRequest>()).ShouldBeTrue();
        });
    }

    public class When_getting_empty_product_id : ProductCommandService_Get
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

            (await Harness.Consumed.Any<ProductStatusRequest>()).ShouldBeFalse();
        });
    }

    public class When_getting_nonexistent_product : ProductCommandService_Get
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<ProductStatusRequest>(context =>
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

            (await Harness.Consumed.Any<ProductStatusRequest>()).ShouldBeTrue();
        });
    }

    public class When_getting_generic_fault : ProductCommandService_Get
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<ProductStatusRequest>(context =>
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

            (await Harness.Consumed.Any<ProductStatusRequest>()).ShouldBeTrue();
        });
    }

    public class When_getting_timeout : ProductCommandService_Get
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<ProductStatusRequest>(async context =>
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

            (await Harness.Consumed.Any<ProductStatusRequest>()).ShouldBeTrue();
        });
    }
}