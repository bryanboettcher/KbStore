namespace KbStore.Catalog.Tests.Domains.Products;

using Abstractions.Contracts;
using Catalog.Domains.Products;
using MassTransit.Testing;
using NUnit.Framework;
using Shouldly;


[TestFixture]
public abstract class Product_InventoryEvents : StateMachine_Tests<ProductStateMachine, ProductEntity>
{
    protected override void Arrange()
    {
        // Create product linked to inventory
        Harness.AddOrUpdateSagaInstance<ProductEntity>(ExistingId, entity =>
        {
            entity.CurrentState = ProductStates.Enabled;
            entity.Sku = "TEST_SKU_123";
            entity.Name = "Test Product";
            entity.InventoryId = LinkedId;
            entity.StockQuantity = 50;
            entity.StockThreshold = 10;
            entity.IsStocked = true;
            entity.CreatedOn = Now;
            entity.UpdatedOn = Now;
        });
    }

    public class When_inventory_quantity_decreased_below_threshold : Product_InventoryEvents
    {
        protected override async Task Act()
        {
            await Harness.Bus.Publish<InventoryQuantityChanged>(new
            {
                InventoryId = LinkedId,
                PartNumber = "TEST_PART",
                Status = InventoryStatus.Available,
                StockQuantity = 5,  // Below threshold of 10
                UpdatedOn = Later
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            (await SagaHarness.Consumed.Any<InventoryQuantityChanged>()).ShouldBeTrue();
            SagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CurrentState.ShouldBe(ProductStates.Enabled);
                o.StockQuantity.ShouldBe(5);
                o.IsStocked.ShouldBeFalse();
            });

            (await Harness.Published.Any<ProductAvailabilityChanged>()).ShouldBeTrue();
        });
    }

    public class When_inventory_quantity_increased_above_threshold : Product_InventoryEvents
    {
        protected override void Arrange()
        {
            base.Arrange();

            // Start with product below threshold
            Harness.AddOrUpdateSagaInstance<ProductEntity>(ExistingId, entity =>
            {
                entity.CurrentState = ProductStates.Enabled;
                entity.Sku = "TEST_SKU_123";
                entity.Name = "Test Product";
                entity.InventoryId = LinkedId;
                entity.StockQuantity = 5;  // Below threshold
                entity.StockThreshold = 10;
                entity.IsStocked = false;
                entity.CreatedOn = Now;
                entity.UpdatedOn = Now;
            });
        }

        protected override async Task Act()
        {
            await Harness.Bus.Publish<InventoryQuantityChanged>(new
            {
                InventoryId = LinkedId,
                PartNumber = "TEST_PART",
                Status = InventoryStatus.Available,
                StockQuantity = 25,  // Above threshold of 10
                UpdatedOn = Later
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            (await SagaHarness.Consumed.Any<InventoryQuantityChanged>()).ShouldBeTrue();
            SagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CurrentState.ShouldBe(ProductStates.Enabled);
                o.StockQuantity.ShouldBe(25);
                o.IsStocked.ShouldBeTrue();
            });

            (await Harness.Published.Any<ProductAvailabilityChanged>()).ShouldBeTrue();
        });
    }

    public class When_inventory_held : Product_InventoryEvents
    {
        protected override async Task Act()
        {
            await Harness.Bus.Publish<InventoryHeld>(new
            {
                InventoryId = LinkedId,
                PartNumber = "TEST_PART",
                Status = InventoryStatus.Held,
                StockQuantity = 50,
                UpdatedOn = Later
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            (await SagaHarness.Consumed.Any<InventoryHeld>()).ShouldBeTrue();
            SagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CurrentState.ShouldBe(ProductStates.Enabled);
                o.StockQuantity.ShouldBe(50);
                o.IsStocked.ShouldBeFalse();  // Held inventory = not stocked
            });

            (await Harness.Published.Any<ProductAvailabilityChanged>()).ShouldBeTrue();
        });
    }

    public class When_inventory_released : Product_InventoryEvents
    {
        protected override void Arrange()
        {
            base.Arrange();

            // Start with held inventory (not stocked)
            Harness.AddOrUpdateSagaInstance<ProductEntity>(ExistingId, entity =>
            {
                entity.CurrentState = ProductStates.Enabled;
                entity.Sku = "TEST_SKU_123";
                entity.Name = "Test Product";
                entity.InventoryId = LinkedId;
                entity.StockQuantity = 50;
                entity.StockThreshold = 10;
                entity.IsStocked = false;  // Was held
                entity.CreatedOn = Now;
                entity.UpdatedOn = Now;
            });
        }

        protected override async Task Act()
        {
            await Harness.Bus.Publish<InventoryReleased>(new
            {
                InventoryId = LinkedId,
                PartNumber = "TEST_PART",
                Status = InventoryStatus.Available,
                StockQuantity = 50,
                UpdatedOn = Later
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            (await SagaHarness.Consumed.Any<InventoryReleased>()).ShouldBeTrue();
            SagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CurrentState.ShouldBe(ProductStates.Enabled);
                o.StockQuantity.ShouldBe(50);
                o.IsStocked.ShouldBeTrue();  // Released and above threshold
            });

            (await Harness.Published.Any<ProductAvailabilityChanged>()).ShouldBeTrue();
        });
    }

    public class When_inventory_discontinued : Product_InventoryEvents
    {
        protected override async Task Act()
        {
            await Harness.Bus.Publish<InventoryDiscontinued>(new
            {
                InventoryId = LinkedId,
                PartNumber = "TEST_PART",
                Status = InventoryStatus.Discontinued,
                StockQuantity = 0,
                UpdatedOn = Later
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            (await SagaHarness.Exists(ExistingId, s => s.Discontinued)).ShouldNotBeNull();
            SagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CurrentState.ShouldBe(ProductStates.Discontinued);
                o.StockQuantity.ShouldBe(0);
                o.IsStocked.ShouldBeFalse();
            });

            (await Harness.Published.Any<ProductAvailabilityChanged>()).ShouldBeTrue();
        });
    }

    public class When_inventory_deleted : Product_InventoryEvents
    {
        protected override async Task Act()
        {
            await Harness.Bus.Publish<InventoryDeleted>(new
            {
                InventoryId = LinkedId,
                Status = InventoryStatus.Invalid,
                PartNumber = "TEST_PART",
                UpdatedOn = Later
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            // Product should be finalized (removed)
            (await SagaHarness.NotExists(ExistingId)).ShouldNotBe(ExistingId);

            (await Harness.Published.Any<ProductAvailabilityChanged>()).ShouldBeTrue();
        });
    }
}