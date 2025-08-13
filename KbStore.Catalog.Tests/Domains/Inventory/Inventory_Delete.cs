namespace KbStore.Catalog.Tests.Domains.Inventory;

using Abstractions.Contracts;
using Catalog.Domains.Inventory;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Shouldly;


[TestFixture]
public class Inventory_Delete : StateMachine_Tests
{
    protected IRequestClient<DeleteInventoryRequest> Client = null!;
    protected Response<DeleteInventoryResponse> Response = null!;

    protected override void Arrange()
    {
        Client = Harness.Bus.CreateRequestClient<DeleteInventoryRequest>();
    }

    protected override async Task Act()
    {
        Response = await Client.GetResponse<DeleteInventoryResponse>(new
        {
            InventoryId = ExistingId,
            Timestamp = Now
        });
    }

    public class When_deleting_available_item : Inventory_Delete
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddSagaInstance<InventoryEntity>(ExistingId, entity =>
            {
                entity.CurrentState = InventoryStates.Available;
                entity.PartNumber = "DELETE_PART";
                entity.StockQuantity = 25;
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Status.ShouldBe(InventoryStatus.Discontinued);
            Response.Message.InventoryId.ShouldBe(ExistingId);

            InventorySagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(ExistingId);
                o.CurrentState.ShouldBe(InventoryStates.Discontinued);
                o.StockQuantity.ShouldBe(25);
            });

            (await Harness.Published.Any<InventoryDiscontinued>()).ShouldBeTrue();
        });
    }

    public class When_deleting_held_item : Inventory_Delete
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddSagaInstance<InventoryEntity>(ExistingId, entity =>
            {
                entity.CurrentState = InventoryStates.OnHold;
                entity.PartNumber = "HELD_DELETE_PART";
                entity.StockQuantity = 15;
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Status.ShouldBe(InventoryStatus.Discontinued);
            Response.Message.InventoryId.ShouldBe(ExistingId);

            InventorySagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(ExistingId);
                o.CurrentState.ShouldBe(InventoryStates.Discontinued);
            });

            (await Harness.Published.Any<InventoryDiscontinued>()).ShouldBeTrue();
        });
    }

    public class When_deleting_discontinued_item : Inventory_Delete
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddSagaInstance<InventoryEntity>(ExistingId, entity =>
            {
                entity.CurrentState = InventoryStates.Discontinued;
                entity.PartNumber = "ALREADY_DISCONTINUED_PART";
                entity.StockQuantity = 0;
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Status.ShouldBe(InventoryStatus.Discontinued);
            Response.Message.InventoryId.ShouldBe(ExistingId);

            // Second delete should finalize the saga
            (await InventorySagaHarness.NotExists(ExistingId)).ShouldNotBe(ExistingId);
            
            (await Harness.Published.Any<InventoryDeleted>()).ShouldBeTrue();
        });
    }
}