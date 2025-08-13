namespace KbStore.Catalog.Tests.Domains;

using KbStore.Catalog.Domains.Inventory;
using KbStore.Catalog.Domains.Products;
using KbStore.Tests;
using MassTransit;
using MassTransit.Testing;


public abstract class StateMachine_Tests : EventingTestBase
{
    protected ISagaStateMachineTestHarness<InventoryStateMachine, InventoryEntity> InventorySagaHarness = null!;
    protected ISagaStateMachineTestHarness<ProductStateMachine, ProductEntity> ProductSagaHarness = null!;

    protected override void OnHarnessCreating(IBusRegistrationConfigurator configurator)
    {
        configurator.AddSagaStateMachine<ProductStateMachine, ProductEntity>().InMemoryRepository();
        configurator.AddSagaStateMachine<InventoryStateMachine, InventoryEntity>().InMemoryRepository();

        base.OnHarnessCreating(configurator);
    }

    protected override Task OnPostSetup()
    {
        ProductSagaHarness = Harness.GetSagaStateMachineHarness<ProductStateMachine, ProductEntity>();
        InventorySagaHarness = Harness.GetSagaStateMachineHarness<InventoryStateMachine, InventoryEntity>();
        
        return base.OnPostSetup();
    }
}
