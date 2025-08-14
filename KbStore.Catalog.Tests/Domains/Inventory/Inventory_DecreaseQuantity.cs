using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Domains.Inventory;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;

namespace KbStore.Catalog.Tests.Domains.Inventory;

using Shouldly;


[TestFixture]
public class Inventory_DecreaseQuantity : StateMachine_Tests<InventoryStateMachine, InventoryEntity>
{
    protected IRequestClient<DecreaseInventoryQuantityRequest> Client = null!;
    protected Response<UpdateInventoryResponse> Response = null!;
    protected Guid InventoryId;
    
    protected override void Arrange()
    {
        Client = Harness.Bus.CreateRequestClient<DecreaseInventoryQuantityRequest>();
        InventoryId = ExistingId;
    }

    protected override async Task Act()
    {
        Response = await Client.GetResponse<UpdateInventoryResponse>(new
        {
            InventoryId,
            Amount = 25,
            Timestamp = Now
        });
    }

    public class When_decreasing_available_item : Inventory_DecreaseQuantity
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddOrUpdateSagaInstance<InventoryEntity>(ExistingId, entity =>
            {
                entity.CurrentState = InventoryStates.Available;
                entity.PartNumber = "PART_789";
                entity.StockQuantity = 100;
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.StockQuantity.ShouldBe(75);
            Response.Message.InventoryId.ShouldBe(ExistingId);

            SagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(ExistingId);
                o.CurrentState.ShouldBe(InventoryStates.Available);
                o.StockQuantity.ShouldBe(75);
            });

            (await Harness.Published.Any<InventoryQuantityDecreased>()).ShouldBeTrue();
        });
    }

    public class When_decreasing_more_than_available : Inventory_DecreaseQuantity
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddOrUpdateSagaInstance<InventoryEntity>(ExistingId, entity =>
            {
                entity.CurrentState = InventoryStates.Available;
                entity.PartNumber = "PART_789";
                entity.StockQuantity = 100;
            });
        }

        protected override async Task Act()
        {
            Response = await Client.GetResponse<UpdateInventoryResponse>(new
            {
                InventoryId,
                Amount = 150, // More than the 100 available
                Timestamp = Now
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
                o.CurrentState.ShouldBe(InventoryStates.Available);
                o.StockQuantity.ShouldBe(100);
            });

            (await Harness.Published.Any<InventoryQuantityDecreased>()).ShouldBeFalse();
        });
    }

    public class When_decreasing_held_item : Inventory_DecreaseQuantity
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddOrUpdateSagaInstance<InventoryEntity>(ExistingId, entity =>
            {
                entity.CurrentState = InventoryStates.OnHold;
                entity.PartNumber = "HELD_PART";
                entity.StockQuantity = 100;
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
                o.CurrentState.ShouldBe(InventoryStates.OnHold);
                o.StockQuantity.ShouldBe(100);
            });

            (await Harness.Published.Any<InventoryQuantityDecreased>()).ShouldBeFalse();
        });
    }

    public class When_decreasing_backordered_item : Inventory_DecreaseQuantity
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddOrUpdateSagaInstance<InventoryEntity>(ExistingId, entity =>
            {
                entity.CurrentState = InventoryStates.Backordered;
                entity.PartNumber = "BACKORDER_PART";
                entity.StockQuantity = 0;
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
                o.CurrentState.ShouldBe(InventoryStates.Backordered);
                o.StockQuantity.ShouldBe(0);
            });

            (await Harness.Published.Any<InventoryQuantityDecreased>()).ShouldBeFalse();
        });
    }

    public class When_decreasing_discontinued_item : Inventory_DecreaseQuantity
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddOrUpdateSagaInstance<InventoryEntity>(ExistingId, entity =>
            {
                entity.CurrentState = InventoryStates.Discontinued;
                entity.PartNumber = "DISCONTINUED_PART";
                entity.StockQuantity = 0;
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
                o.CurrentState.ShouldBe(InventoryStates.Discontinued);
                o.StockQuantity.ShouldBe(0);
            });

            (await Harness.Published.Any<InventoryQuantityDecreased>()).ShouldBeFalse();
        });
    }
}