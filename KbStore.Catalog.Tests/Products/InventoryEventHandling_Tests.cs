namespace KbStore.Catalog.Tests.Products;

using Abstractions.Contracts;
using KbStore.Catalog.Domains.Inventory;
using MassTransit.Testing;
using NUnit.Framework;
using Services;
using Shouldly;


[Category("Products")]
[Category("Integration")]
public abstract class InventoryEventHandling_Tests : Catalog_Tests<MassTransitProductCommandService>
{
    protected ProductModel? Result;

    protected override void Arrange()
    {
        InventoryId = Guid.NewGuid();

        Harness.AddSagaInstance<InventoryEntity>(InventoryId, entity =>
        {
            entity.PartNumber = InventoryPartNumber;
            entity.Description = InventoryDescription;
            entity.StockQuantity = InventoryStockQuantity;
            entity.CurrentState = InventoryStates.Available;
        });
    }

    protected override Task Act()
    {
        
    }
    
    public class When_inventory_discontinued : InventoryEventHandling_Tests
    {
        protected override void Arrange()
        {
            InventoryStockQuantity = 5;
            base.Arrange();
        }

        protected override async Task Act()
        {
            await PublishInventoryEvent<InventoryDiscontinued>(new
            {
                InventoryId,
                StockQuantity = 0,
                Status = InventoryStatus.Discontinued,
                Timestamp = Later
            });

            Result = await Subject.GetAsync(InventoryId);
        }

        [Test]
        public void It_should_discontinue_product()
            => Result!.IsEnabled.ShouldBeFalse();

        [Test]
        public async Task It_should_publish_product_discontinued_event()
            => (await Harness.Published.Any<ProductDiscontinued>()).ShouldBeTrue();
    }

    public class When_inventory_held : InventoryEventHandling_Tests
    {
        protected override async Task Act()
        {
            await base.Act();

            await PublishInventoryEvent<InventoryHeld>(new
            {
                InventoryId = InventoryId,
                StockQuantity = 50,
                Status = InventoryStatus.Held,
                Timestamp = Later
            });

            Result = await Subject.GetAsync(TestId);
        }

        [Test]
        public void It_should_mark_unavailable()
            => Result!.IsAvailable.ShouldBeFalse();

        [Test]
        public async Task It_should_publish_product_availability_changed_event()
            => (await Harness.Published.Any<ProductAvailabilityChanged>()).ShouldBeTrue();
    }

    public class When_inventory_released : InventoryEventHandling_Tests
    {
        protected override async Task Act()
        {
            await base.Act();

            // First hold the inventory
            await PublishInventoryEvent<InventoryHeld>();

            // Then release it
            await PublishInventoryEvent<InventoryReleased>(new
            {
                InventoryId = InventoryId,
                StockQuantity = 50,
                Status = InventoryStatus.Available,
                Timestamp = Later
            });

            Result = await Subject.GetAsync(TestId);
        }

        [Test]
        public void It_should_restore_availability()
            => Result!.IsAvailable.ShouldBeTrue();

        [Test]
        public async Task It_should_publish_product_availability_changed_event()
            => (await Harness.Published.Any<ProductAvailabilityChanged>()).ShouldBeTrue();
    }

    public class When_inventory_deleted : InventoryEventHandling_Tests
    {
        protected override async Task Act()
        {
            await base.Act();

            await PublishInventoryEvent<InventoryDeleted>(new
            {
                InventoryId = InventoryId,
                StockQuantity = 0,
                Status = InventoryStatus.Discontinued,
                Timestamp = Later
            });

            Result = await Subject.GetAsync(TestId);
        }

        [Test]
        public void It_should_clear_inventory_reference()
            => Result!.InventoryItemId.ShouldBeNull();

        [Test]
        public void It_should_disable_product()
            => Result!.IsEnabled.ShouldBeFalse();

        [Test]
        public async Task It_should_publish_product_availability_changed_event()
            => (await Harness.Published.Any<ProductAvailabilityChanged>()).ShouldBeTrue();
    }

    public class When_inventory_quantity_increases_above_threshold : InventoryEventHandling_Tests
    {
        protected override void Arrange()
        {
            base.Arrange();

            // Pre-create linked inventory saga
            Harness.AddSagaInstance<InventoryEntity>(InventoryId!.Value, entity =>
            {
                entity.StockQuantity = 5; // Below threshold
                entity.CurrentState = 3;
            });
        }

        protected override async Task Act()
        {
            await base.Act();

            // Start with low stock (below threshold of 10)
            await PublishInventoryEvent<InventoryQuantityChanged>(new
            {
                InventoryId = InventoryId,
                StockQuantity = 5,
                Timestamp = Now
            });

            // Increase above threshold
            await PublishInventoryEvent<InventoryQuantityChanged>(new
            {
                InventoryId = InventoryId,
                StockQuantity = 15,
                Timestamp = Later
            });

            Result = await Subject.GetAsync(TestId);
        }

        [Test]
        public void It_should_make_product_available()
            => Result!.IsAvailable.ShouldBeTrue();

        [Test]
        public async Task It_should_publish_product_availability_changed_event()
            => (await Harness.Published.Any<ProductAvailabilityChanged>()).ShouldBeTrue();
    }

    public class When_inventory_quantity_decreases_below_threshold : InventoryEventHandling_Tests
    {
        protected override async Task Act()
        {
            await base.Act();

            // Start with good stock (above threshold of 10)
            await PublishInventoryEvent<InventoryQuantityChanged>(new
            {
                InventoryId = InventoryId,
                StockQuantity = 20,
                Timestamp = Now
            });

            // Decrease below threshold
            await PublishInventoryEvent<InventoryQuantityChanged>(new
            {
                InventoryId = InventoryId,
                StockQuantity = 5,
                Timestamp = Later
            });

            Result = await Subject.GetAsync(TestId);
        }

        [Test]
        public void It_should_make_product_unavailable()
            => Result!.IsAvailable.ShouldBeFalse();

        [Test]
        public async Task It_should_publish_product_availability_changed_event()
            => (await Harness.Published.Any<ProductAvailabilityChanged>()).ShouldBeTrue();
    }
}