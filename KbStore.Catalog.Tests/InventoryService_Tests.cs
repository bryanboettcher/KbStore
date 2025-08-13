namespace KbStore.Catalog.Tests;

using Domains.Inventory;
using KbStore.Tests;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Services;


[TestFixture]
public abstract class InventoryService_Tests<TService> : TestBase
    where TService : class
{
    protected ISagaStateMachineTestHarness<InventoryStateMachine, InventoryEntity>? SagaHarness;
    protected TService Subject = null!;

    protected Guid TestId;
    protected string? TestPartNumber;
    protected string? TestDescription;
    protected int TestQuantity;
    protected DateTimeOffset Now = new(2025, 08, 01, 15, 55, 18, TimeSpan.Zero);
    protected DateTimeOffset Later = new(2025, 08, 01, 16, 55, 18, TimeSpan.Zero);
    protected DateTimeOffset InitialCreatedOn = new(2025, 08, 01, 15, 55, 18, TimeSpan.Zero);

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
        Subject = ScopedProvider.ServiceProvider.GetRequiredService<TService>();
        SagaHarness = Harness!.GetSagaStateMachineHarness<InventoryStateMachine, InventoryEntity>();

        return Task.CompletedTask;
    }

    protected async Task CreateExistingInventory(
        string partNumber = "TEST_PART_123",
        string description = "Test Description",
        int quantity = 50
    )
    {
        if (Subject is not MassTransitInventoryCommandService service)
            return;

        if (TestId != Guid.Empty)
            return;

        var result = await service.CreateAsync(partNumber, description, quantity);
        await SagaHarness!.Created.Any();

        TestId = result.InventoryId;
    }
}