namespace KbStore.Catalog.Tests;

using Abstractions.Contracts;
using Domains.Inventory;
using Services;
using KbStore.Tests;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;


[TestFixture]
public abstract class CatalogDomain_Tests : TestBase
{
    protected ISagaStateMachineTestHarness<InventoryStateMachine, InventoryEntity>? SagaHarness;
    protected InventoryModel? Result = null!;
    protected MassTransitInventoryCommandService Subject = null!;

    protected Guid TestId;
    protected string? TestPartNumber;
    protected string? TestDescription;
    protected int TestQuantity;
    protected DateTimeOffset Now = DateTimeOffset.Now;

    protected override void OnServicesCreating(IServiceCollection services)
    {
        services.AddScoped<MassTransitInventoryCommandService>();
        services.AddScoped<Func<DateTimeOffset>>(_ => () => Now);
    }

    protected override void OnHarnessCreating(IBusRegistrationConfigurator configurator)
    {
        configurator.AddSagaStateMachine<InventoryStateMachine, InventoryEntity>();
        // configurator.SetTestTimeouts(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(250));
        // configurator.SetDefaultRequestTimeout(ms: 500);
    }

    protected override Task OnPostSetup()
    {
        Subject = ScopedProvider.ServiceProvider.GetRequiredService<MassTransitInventoryCommandService>();
        SagaHarness = Harness!.GetSagaStateMachineHarness<InventoryStateMachine, InventoryEntity>();

        return Task.CompletedTask;
    }

    protected async Task CreateExistingInventory(
        string partNumber = "TEST_PART_123",
        string description = "Test Description",
        int quantity = 50
    )
    {
        if (TestId != Guid.Empty)
            return;

        var result = await Subject.CreateAsync(partNumber, description, quantity);
        await SagaHarness!.Created.Any();

        TestId = result.InventoryId;
    }
}