namespace KbStore.Catalog.Tests.Domains.Inventory;

using Abstractions.Contracts;
using Catalog.Domains.Inventory;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Shouldly;


[TestFixture]
public class Inventory_UpdateDescription : StateMachine_Tests<InventoryStateMachine, InventoryEntity>
{
    protected IRequestClient<UpdateInventoryDescriptionRequest> Client = null!;
    protected Response<UpdateInventoryResponse> Response = null!;

    protected override void Arrange()
    {
        Client = Harness.Bus.CreateRequestClient<UpdateInventoryDescriptionRequest>();
    }

    protected override async Task Act()
    {
        Response = await Client.GetResponse<UpdateInventoryResponse>(new
        {
            InventoryId = ExistingId,
            Description = "Updated Description",
            Timestamp = Now
        });
    }

    public class When_updating_available_item : Inventory_UpdateDescription
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddOrUpdateSagaInstance<InventoryEntity>(ExistingId, entity =>
            {
                entity.CurrentState = InventoryStates.Available;
                entity.PartNumber = "DESC_PART";
                entity.Description = "Original Description";
                entity.StockQuantity = 50;
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Description.ShouldBe("Updated Description");
            Response.Message.InventoryId.ShouldBe(ExistingId);

            SagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(ExistingId);
                o.CurrentState.ShouldBe(InventoryStates.Available);
                o.Description.ShouldBe("Updated Description");
            });

            (await Harness.Published.Any<InventoryDescriptionUpdated>()).ShouldBeTrue();
        });
    }

    public class When_updating_held_item : Inventory_UpdateDescription
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddOrUpdateSagaInstance<InventoryEntity>(ExistingId, entity =>
            {
                entity.CurrentState = InventoryStates.OnHold;
                entity.PartNumber = "HELD_DESC_PART";
                entity.Description = "Held Description";
                entity.StockQuantity = 25;
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Description.ShouldBe("Updated Description");
            Response.Message.InventoryId.ShouldBe(ExistingId);

            SagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(ExistingId);
                o.CurrentState.ShouldBe(InventoryStates.OnHold);
                o.Description.ShouldBe("Updated Description");
            });

            (await Harness.Published.Any<InventoryDescriptionUpdated>()).ShouldBeTrue();
        });
    }

    public class When_updating_backordered_item : Inventory_UpdateDescription
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddOrUpdateSagaInstance<InventoryEntity>(ExistingId, entity =>
            {
                entity.CurrentState = InventoryStates.Backordered;
                entity.PartNumber = "BACKORDER_DESC_PART";
                entity.Description = "Backordered Description";
                entity.StockQuantity = 0;
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.Description.ShouldBe("Updated Description");

            SagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(ExistingId);
                o.CurrentState.ShouldBe(InventoryStates.Backordered);
                o.Description.ShouldBe("Updated Description");
            });

            (await Harness.Published.Any<InventoryDescriptionUpdated>()).ShouldBeTrue();
        });
    }

    public class When_updating_discontinued_item : Inventory_UpdateDescription
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddOrUpdateSagaInstance<InventoryEntity>(ExistingId, entity =>
            {
                entity.CurrentState = InventoryStates.Discontinued;
                entity.PartNumber = "DISCONTINUED_DESC_PART";
                entity.Description = "Discontinued Description";
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
                o.Description.ShouldBe("Discontinued Description");
            });

            (await Harness.Published.Any<InventoryDescriptionUpdated>()).ShouldBeFalse();
        });
    }
}