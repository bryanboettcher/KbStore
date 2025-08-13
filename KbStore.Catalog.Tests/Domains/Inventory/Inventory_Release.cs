namespace KbStore.Catalog.Tests.Domains.Inventory;

using Abstractions.Contracts;
using Catalog.Domains.Inventory;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Shouldly;


[TestFixture]
public class Inventory_Release : StateMachine_Tests
{
    protected IRequestClient<ReleaseInventoryRequest> Client = null!;
    protected Response<ReleaseInventoryResponse> Response = null!;

    protected override void Arrange()
    {
        Client = Harness.Bus.CreateRequestClient<ReleaseInventoryRequest>();
    }

    protected override async Task Act()
    {
        Response = await Client.GetResponse<ReleaseInventoryResponse>(new
        {
            InventoryId = ExistingId,
            Timestamp = Now
        });
    }

    public class When_releasing_held_item : Inventory_Release
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddSagaInstance<InventoryEntity>(ExistingId, entity =>
            {
                entity.CurrentState = InventoryStates.OnHold;
                entity.PartNumber = "RELEASE_PART";
                entity.StockQuantity = 40;
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Status.ShouldBe(InventoryStatus.Available);
            Response.Message.InventoryId.ShouldBe(ExistingId);

            InventorySagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(ExistingId);
                o.CurrentState.ShouldBe(InventoryStates.Available);
                o.StockQuantity.ShouldBe(40);
            });

            (await Harness.Published.Any<InventoryReleased>()).ShouldBeTrue();
        });
    }

    public class When_releasing_available_item : Inventory_Release
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddSagaInstance<InventoryEntity>(ExistingId, entity =>
            {
                entity.CurrentState = InventoryStates.Available;
                entity.PartNumber = "AVAILABLE_RELEASE_PART";
                entity.StockQuantity = 60;
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<RequestFaultException>();

            InventorySagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(ExistingId);
                o.CurrentState.ShouldBe(InventoryStates.Available);
            });

            (await Harness.Published.Any<InventoryReleased>()).ShouldBeFalse();
        });
    }

    public class When_releasing_backordered_item : Inventory_Release
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddSagaInstance<InventoryEntity>(ExistingId, entity =>
            {
                entity.CurrentState = InventoryStates.Backordered;
                entity.PartNumber = "BACKORDER_RELEASE_PART";
                entity.StockQuantity = 0;
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<RequestFaultException>();

            InventorySagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(ExistingId);
                o.CurrentState.ShouldBe(InventoryStates.Backordered);
            });

            (await Harness.Published.Any<InventoryReleased>()).ShouldBeFalse();
        });
    }

    public class When_releasing_discontinued_item : Inventory_Release
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddSagaInstance<InventoryEntity>(ExistingId, entity =>
            {
                entity.CurrentState = InventoryStates.Discontinued;
                entity.PartNumber = "DISCONTINUED_RELEASE_PART";
                entity.StockQuantity = 0;
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<RequestFaultException>();

            InventorySagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(ExistingId);
                o.CurrentState.ShouldBe(InventoryStates.Discontinued);
            });

            (await Harness.Published.Any<InventoryReleased>()).ShouldBeFalse();
        });
    }
}