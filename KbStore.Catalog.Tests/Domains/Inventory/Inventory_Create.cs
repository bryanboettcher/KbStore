namespace KbStore.Catalog.Tests.Domains.Inventory;

using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Domains.Inventory;
using KbStore.Catalog.Tests.Domains;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Shouldly;


[TestFixture]
public class Inventory_Create : StateMachine_Tests<InventoryStateMachine, InventoryEntity>
{
    protected IRequestClient<CreateInventoryRequest> Client = null!;
    protected Response<CreateInventoryResponse> Response = null!;

    protected override void Arrange()
    {
        Client = Harness.Bus.CreateRequestClient<CreateInventoryRequest>();
    }

    protected override async Task Act()
    {
        Response = await Client.GetResponse<CreateInventoryResponse>(new
        {
            PartNumber = "TEST_123",
            Description = "Test Description",
            StockQuantity = 50,
            Timestamp = Now
        });
    }

    public class When_creating_without_inventory_link : Inventory_Create
    {
        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.PartNumber.ShouldBe("TEST_123");
            Response.Message.StockQuantity.ShouldBe(50);

            var sagaId = Response.Message.InventoryId;
            SagaHarness.Sagas.Contains(sagaId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(sagaId);
                o.CurrentState.ShouldBe(InventoryStates.Available);
                o.PartNumber.ShouldBe("TEST_123");
                o.Description.ShouldBe("Test Description");
                o.StockQuantity.ShouldBe(50);
            });

            (await Harness.Published.Any<InventoryCreated>()).ShouldBeTrue();
        });
    }

    public class When_creating_with_duplicate : Inventory_Create
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddOrUpdateSagaInstance<InventoryEntity>(ExistingId, entity =>
            {
                entity.CurrentState = InventoryStates.Available;
                entity.PartNumber = "TEST_123";
                entity.Description = "Original Description";
                entity.StockQuantity = 10;
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.ShouldSatisfyAllConditions(o =>
            {
                o.PartNumber.ShouldBe("TEST_123");
                o.Status.ShouldBe(InventoryStatus.Available);
            });

            SagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(ExistingId);
                o.CurrentState.ShouldBe(InventoryStates.Available);
                o.PartNumber.ShouldBe("TEST_123");
                o.Description.ShouldBe("Original Description");
                o.StockQuantity.ShouldBe(10);
            });

            (await Harness.Published.Any<InventoryCreated>()).ShouldBeFalse();
        });
    }

    public class When_creating_discontinued_item : Inventory_Create
    {
        protected override void Arrange()
        {
            base.Arrange();

            Harness.AddOrUpdateSagaInstance<InventoryEntity>(ExistingId, entity =>
            {
                entity.CurrentState = InventoryStates.Discontinued;
                entity.PartNumber = "TEST_123";
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.ShouldSatisfyAllConditions(o =>
            {
                o.PartNumber.ShouldBe("TEST_123");
                o.Status.ShouldBe(InventoryStatus.Available);
            });

            SagaHarness.Sagas.Contains(ExistingId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(ExistingId);
                o.CurrentState.ShouldBe(InventoryStates.Available);
                o.PartNumber.ShouldBe("TEST_123");
            });

            (await Harness.Published.Any<InventoryCreated>()).ShouldBeTrue();
        });
    }

}