namespace KbStore.Catalog.Tests.Domains.Inventory;

using Abstractions.Contracts;
using Catalog.Domains.Inventory;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Shouldly;


[TestFixture]
public class Inventory_Create : StateMachine_Tests
{
    protected IRequestClient<CreateInventoryRequest> Client = null!;

    protected object Request = new { };
    protected Response<CreateInventoryResponse>? Response;

    protected override void Arrange()
    {
        Client = Harness.Bus.CreateRequestClient<CreateInventoryRequest>();
    }

    protected override async Task Act()
    {
        Response = await Client.GetResponse<CreateInventoryResponse>(Request, timeout: RequestTimeout.After(ms:500));
    }

    public class When_creating_without_inventory_link : Inventory_Create
    {
        protected override void Arrange()
        {
            Request = new
            {
                PartNumber = "TEST_123",
                Description = "Test Description",
                StockQuantity = 50
            };

            base.Arrange();
        }
        
        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Response.ShouldNotBeNull();
            Response.Message.ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.InventoryId.ShouldNotBe(Guid.Empty);
                o.PartNumber.ShouldBe("TEST_123");
                o.Description.ShouldBe("Test Description");
                o.StockQuantity.ShouldBe(50);
            });

            var sagaId = Response.Message.InventoryId;
            InventorySagaHarness.Sagas.Contains(sagaId).ShouldSatisfyAllConditions(o =>
            {
                o.ShouldNotBeNull();
                o.CorrelationId.ShouldBe(sagaId);
                o.CurrentState.ShouldBe(InventoryStates.Available);
                o.PartNumber.ShouldBe("TEST_123");
                o.Description.ShouldBe("Test Description");
                o.StockQuantity.ShouldBe(50);
            });

            await Task.CompletedTask;
        });
    }

    public class When_creating_with_duplicate : Inventory_Create
    {
        protected override void Arrange()
        {
            Harness.AddSagaInstance<InventoryEntity>(callback: entity =>
            {
                entity.CurrentState = InventoryStates.Available;
                entity.PartNumber = "TEST_123";
                entity.Description = "Original Description";
                entity.StockQuantity = 10;
            });

            Request = new
            {
                PartNumber = "TEST_123",
                Description = "Test Description",
                StockQuantity = 50
            };

            base.Arrange();
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeOfType<RequestFaultException>();

            InventorySagaHarness.Sagas.Select(_ => true).ToList().ShouldSatisfyAllConditions(o =>
            {
                o.Count.ShouldBe(1);
                o[0].Saga.CurrentState.ShouldBe(InventoryStates.Available);
                o[0].Saga.PartNumber.ShouldBe("TEST_123");
                o[0].Saga.Description.ShouldBe("Original Description");
                o[0].Saga.StockQuantity.ShouldBe(10);
            });
            
            await Task.CompletedTask;
        });
    }
}
