namespace KbStore.Catalog.Tests.Domains.Inventory;

using Abstractions.Contracts;
using Catalog.Domains.Inventory;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Shouldly;


[TestFixture]
public class Inventory_GetStatus : StateMachine_Tests
{
    protected IRequestClient<InventoryStatusRequest> Client = null!;
    protected Response<InventoryStatusResponse> Response = null!;

    protected override void Arrange()
    {
        Client = Harness.Bus.CreateRequestClient<InventoryStatusRequest>();
    }

    protected override async Task Act()
    {
        Response = await Client.GetResponse<InventoryStatusResponse>(new
        {
            InventoryId = ExistingId,
            Timestamp = Now
        });
    }

    public class When_getting_available_item_status : Inventory_GetStatus
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddSagaInstance<InventoryEntity>(ExistingId, entity =>
            {
                entity.CurrentState = InventoryStates.Available;
                entity.PartNumber = "STATUS_PART";
                entity.Description = "Status Test Item";
                entity.StockQuantity = 35;
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.InventoryId.ShouldBe(ExistingId);
            Response.Message.PartNumber.ShouldBe("STATUS_PART");
            Response.Message.Description.ShouldBe("Status Test Item");
            Response.Message.StockQuantity.ShouldBe(35);
            Response.Message.Status.ShouldBe(InventoryStatus.Available);

            InventorySagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(ExistingId);
                o.CurrentState.ShouldBe(InventoryStates.Available);
            });

            await Task.CompletedTask;
        });
    }

    public class When_getting_held_item_status : Inventory_GetStatus
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddSagaInstance<InventoryEntity>(ExistingId, entity =>
            {
                entity.CurrentState = InventoryStates.OnHold;
                entity.PartNumber = "HELD_STATUS_PART";
                entity.Description = "Held Status Item";
                entity.StockQuantity = 20;
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.InventoryId.ShouldBe(ExistingId);
            Response.Message.Status.ShouldBe(InventoryStatus.Held);
            Response.Message.StockQuantity.ShouldBe(20);

            InventorySagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(ExistingId);
                o.CurrentState.ShouldBe(InventoryStates.OnHold);
            });

            await Task.CompletedTask;
        });
    }

    public class When_getting_backordered_item_status : Inventory_GetStatus
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddSagaInstance<InventoryEntity>(ExistingId, entity =>
            {
                entity.CurrentState = InventoryStates.Backordered;
                entity.PartNumber = "BACKORDER_STATUS_PART";
                entity.Description = "Backordered Status Item";
                entity.StockQuantity = 0;
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Status.ShouldBe(InventoryStatus.Backordered);
            Response.Message.StockQuantity.ShouldBe(0);

            InventorySagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(ExistingId);
                o.CurrentState.ShouldBe(InventoryStates.Backordered);
            });

            await Task.CompletedTask;
        });
    }

    public class When_getting_discontinued_item_status : Inventory_GetStatus
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddSagaInstance<InventoryEntity>(ExistingId, entity =>
            {
                entity.CurrentState = InventoryStates.Discontinued;
                entity.PartNumber = "DISCONTINUED_STATUS_PART";
                entity.Description = "Discontinued Status Item";
                entity.StockQuantity = 0;
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Status.ShouldBe(InventoryStatus.Discontinued);

            InventorySagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(ExistingId);
                o.CurrentState.ShouldBe(InventoryStates.Discontinued);
            });

            await Task.CompletedTask;
        });
    }

    public class When_getting_nonexistent_item_status : Inventory_GetStatus
    {
        // No Arrange override - uses nonexistent ExistingId

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<RequestFaultException>();

            await Task.CompletedTask;
        });
    }
}