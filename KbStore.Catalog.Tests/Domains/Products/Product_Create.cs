namespace KbStore.Catalog.Tests.Domains.Products;

using Abstractions.Contracts;
using Catalog.Domains.Products;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Shouldly;


[TestFixture]
public class Product_Create : StateMachine_Tests<ProductStateMachine, ProductEntity>
{
    protected IRequestClient<CreateProductRequest> Client = null!;
    protected Response<CreateProductResponse> Response = null!;

    protected override void Arrange()
    {
        Client = CreateRequestClient<CreateProductRequest>();
    }

    protected override async Task Act()
    {
        Response = await Client.GetResponse<CreateProductResponse>(new
        {
            Sku = "TEST_SKU_123",
            Name = "Test Product",
            Dimensions = new ProductDimensions { Width = 10, Height = 5, Length = 15, Weight = 2.5m },
            InventoryId = (Guid?)null,
            StockThreshold = (int?)null,
            LeadTime = (TimeSpan?)null,
            Timestamp = Now
        });
    }

    public class When_creating_without_inventory_link : Product_Create
    {
        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Sku.ShouldBe("TEST_SKU_123");
            Response.Message.Name.ShouldBe("Test Product");
            Response.Message.InventoryId.ShouldBeNull();
            Response.Message.IsStocked.ShouldBeTrue();
            Response.Message.IsEnabled.ShouldBeTrue();
            Response.Message.IsAvailable.ShouldBeTrue();

            var sagaId = Response.Message.ProductId;
            SagaHarness.Sagas.Contains(sagaId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(sagaId);
                o.CurrentState.ShouldBe(ProductStates.Enabled);
                o.Sku.ShouldBe("TEST_SKU_123");
                o.Name.ShouldBe("Test Product");
                o.InventoryId.ShouldBeNull();
                o.IsStocked.ShouldBeTrue();
                o.Width.ShouldBe(10);
                o.Height.ShouldBe(5);
                o.Depth.ShouldBe(15);
                o.Weight.ShouldBe(2.5m);
            });

            (await Harness.Published.Any<ProductCreated>()).ShouldBeTrue();
        });
    }

    public class When_creating_with_inventory_link_successful : Product_Create
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator configurator)
        {
            base.OnHarnessCreating(configurator);

            configurator.AddHandler<InventoryStatusRequest>(async context =>
            {
                LogContext.Warning?.Log("Faking response");
                await context.RespondAsync<InventoryStatusResponse>(new
                {
                    InventoryId = context.Message.InventoryId,
                    PartNumber = "INV_PART_123",
                    Description = "Inventory Item",
                    StockQuantity = 50,
                    Status = InventoryStatus.Available,
                    CreatedOn = Now,
                    UpdatedOn = Now
                });
            });
        }
        
        protected override async Task Act()
        {
            Response = await Client.GetResponse<CreateProductResponse>(new
            {
                Sku = "TEST_SKU_123",
                Name = "Test Product with Inventory",
                Dimensions = (ProductDimensions?)null,
                InventoryId = LinkedId,
                StockThreshold = 10,
                LeadTime = TimeSpan.FromDays(7),
                Timestamp = Now
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Sku.ShouldBe("TEST_SKU_123");
            Response.Message.InventoryId.ShouldBe(LinkedId);
            Response.Message.StockThreshold.ShouldBe(10);
            Response.Message.IsEnabled.ShouldBeFalse();

            var sagaId = Response.Message.ProductId;
            (await SagaHarness.Exists(sagaId, s => s.Enabled)).ShouldNotBeNull();
            SagaHarness.Sagas.Contains(sagaId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(sagaId);
                o.CurrentState.ShouldBe(ProductStates.Enabled);
                o.Sku.ShouldBe("TEST_SKU_123");
                o.InventoryId.ShouldBe(LinkedId);
                o.StockQuantity.ShouldBe(50);
                o.StockThreshold.ShouldBe(10);
                o.LeadTime.ShouldBe(TimeSpan.FromDays(7));
            });

            (await Harness.Published.Any<ProductCreated>()).ShouldBeTrue();
            (await Harness.Consumed.Any<InventoryStatusRequest>()).ShouldBeTrue();
        });
    }

    public class When_creating_with_inventory_link_faulted : Product_Create
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator configurator)
        {
            base.OnHarnessCreating(configurator);

            configurator.AddHandler<InventoryStatusRequest>(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                throw new Exception("Inventory not found");
            });
        }
        
        protected override async Task Act()
        {
            Response = await Client.GetResponse<CreateProductResponse>(new
            {
                Sku = "TEST_SKU_123",
                Name = "Test Product with Bad Inventory",
                Dimensions = (ProductDimensions?)null,
                InventoryId = LinkedId,
                StockThreshold = 10,
                LeadTime = (TimeSpan?)null,
                Timestamp = Now
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.IsEnabled.ShouldBeFalse();
            Response.Message.IsAvailable.ShouldBeFalse();

            var sagaId = Response.Message.ProductId;
            
            (await SagaHarness.Exists(sagaId, s => s.Disabled)).ShouldNotBeNull();

            SagaHarness.Sagas.Contains(sagaId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(sagaId);
                o.CurrentState.ShouldBe(ProductStates.Disabled);
                o.StockQuantity.ShouldBe(0);
            });

            (await Harness.Published.Any<ProductCreated>()).ShouldBeTrue();
            (await Harness.Consumed.Any<InventoryStatusRequest>()).ShouldBeTrue();
        });
    }

    public class When_creating_with_inventory_link_timeout : Product_Create
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator configurator)
        {
            base.OnHarnessCreating(configurator);

            configurator.AddHandler<InventoryStatusRequest>(async context =>
            {
                await Task.Delay(2000, context.CancellationToken);
            });
        }

        protected override async Task Act()
        {
            Response = await Client.GetResponse<CreateProductResponse>(new
            {
                Sku = "TEST_SKU_123",
                Name = "Test Product with Timeout",
                Dimensions = (ProductDimensions?)null,
                InventoryId = LinkedId,
                StockThreshold = 10,
                LeadTime = (TimeSpan?)null,
                Timestamp = Now
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.IsEnabled.ShouldBeFalse();
            Response.Message.IsAvailable.ShouldBeFalse();

            var sagaId = Response.Message.ProductId;
            (await SagaHarness.Exists(sagaId, s => s.Disabled)).ShouldNotBeNull();
            SagaHarness.Sagas.Contains(sagaId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(sagaId);
                o.CurrentState.ShouldBe(ProductStates.Disabled);
                o.StockQuantity.ShouldBe(0);
            });

            (await Harness.Published.Any<ProductCreated>()).ShouldBeTrue();
            (await Harness.Consumed.Any<InventoryStatusRequest>()).ShouldBeTrue();
        });
    }

    public class When_creating_with_duplicate_sku : Product_Create
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddOrUpdateSagaInstance<ProductEntity>(ExistingId, entity =>
            {
                entity.CurrentState = ProductStates.Enabled;
                entity.Sku = "TEST_SKU_123";
                entity.Name = "Existing Product";
                entity.IsStocked = true;
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<RequestFaultException>();

            SagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(ExistingId);
                o.CurrentState.ShouldBe(ProductStates.Enabled);
                o.Sku.ShouldBe("TEST_SKU_123");
                o.Name.ShouldBe("Existing Product");
            });

            (await Harness.Published.Any<ProductCreated>()).ShouldBeFalse();
        });
    }
}