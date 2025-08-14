using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Domains.Inventory;
using KbStore.Catalog.Tests.Domains;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Shouldly;
using System;
using System.Threading.Tasks;

namespace KbStore.Catalog.Tests.Domains.Inventory;

[TestFixture]
public class Inventory_IncreaseQuantity : StateMachine_Tests<InventoryStateMachine, InventoryEntity>
{
    protected IRequestClient<IncreaseInventoryQuantityRequest> Client = null!;
    protected Response<UpdateInventoryResponse> Response = null!;
    protected Guid InventoryId;

    protected override void Arrange()
    {
        Client = Harness.Bus.CreateRequestClient<IncreaseInventoryQuantityRequest>();
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

    public class When_increasing_available_item : Inventory_IncreaseQuantity
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddOrUpdateSagaInstance<InventoryEntity>(ExistingId, entity =>
            {
                entity.CurrentState = InventoryStates.Available;
                entity.PartNumber = "PART_456";
                entity.Description = "Available Item";
                entity.StockQuantity = 100;
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.StockQuantity.ShouldBe(125);
            Response.Message.InventoryId.ShouldBe(ExistingId);

            SagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(ExistingId);
                o.CurrentState.ShouldBe(InventoryStates.Available);
                o.StockQuantity.ShouldBe(125);
            });

            (await Harness.Published.Any<InventoryQuantityIncreased>()).ShouldBeTrue();
        });
    }

    public class When_increasing_held_item : Inventory_IncreaseQuantity
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddOrUpdateSagaInstance<InventoryEntity>(ExistingId, entity =>
            {
                entity.CurrentState = InventoryStates.OnHold;
                entity.PartNumber = "HELD_PART";
                entity.StockQuantity = 50;
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
                o.StockQuantity.ShouldBe(50);
            });

            (await Harness.Published.Any<InventoryQuantityIncreased>()).ShouldBeFalse();
        });
    }

    public class When_increasing_backordered_item : Inventory_IncreaseQuantity
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
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.StockQuantity.ShouldBe(25);
            Response.Message.InventoryId.ShouldBe(ExistingId);

            SagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(ExistingId);
                o.CurrentState.ShouldBe(InventoryStates.Available);
                o.StockQuantity.ShouldBe(25);
            });

            (await Harness.Published.Any<InventoryQuantityIncreased>()).ShouldBeTrue();
        });
    }

    public class When_increasing_discontinued_item : Inventory_IncreaseQuantity
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

            (await Harness.Published.Any<InventoryQuantityIncreased>()).ShouldBeFalse();
        });
    }
}