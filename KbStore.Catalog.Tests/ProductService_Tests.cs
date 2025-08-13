namespace KbStore.Catalog.Tests;

using Abstractions.Contracts;
using Domains.Products;
using KbStore.Catalog.Domains.Inventory;
using KbStore.Tests;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Services;


[TestFixture]
public abstract class CatalogBase_Tests<TService> : TestBase
    where TService : class
{
    protected ISagaStateMachineTestHarness<InventoryStateMachine, InventoryEntity>? InventorySagaHarness;
    protected ISagaStateMachineTestHarness<ProductStateMachine, ProductEntity>? ProductSagaHarness;
    protected TService Subject = null!;

    protected DateTimeOffset Now = new(2025, 08, 01, 15, 55, 18, TimeSpan.Zero);
    protected DateTimeOffset Later = new(2025, 08, 01, 16, 55, 18, TimeSpan.Zero);
    protected DateTimeOffset InitialCreatedOn = new(2025, 08, 01, 15, 55, 18, TimeSpan.Zero);

    protected Guid? InventoryId;
    protected string InventoryPartNumber;
    protected string InventoryDescription;
    protected int InventoryStockQuantity;

    protected Guid ProductId;
    protected string ProductSku;
    protected string ProductName;
    protected string ProductDescription;
    protected ProductDimensions ProductDimensions;
    protected int? ProductStockThreshold;
    protected TimeSpan? ProductLeadTime;

    protected override void OnServicesCreating(IServiceCollection services)
    {
        services.AddScoped<MassTransitInventoryCommandService>();
        services.AddScoped<MassTransitProductCommandService>();
        services.AddScoped<Func<DateTimeOffset>>(_ => () => Now);
    }

    protected override void OnHarnessCreating(IBusRegistrationConfigurator configurator)
    {
        configurator.AddSagaStateMachine<ProductStateMachine, ProductEntity>();
        configurator.AddSagaStateMachine<InventoryStateMachine, InventoryEntity>();
    }

    protected override Task OnPostSetup()
    {
        Subject = ScopedProvider.ServiceProvider.GetRequiredService<TService>();

        ProductSagaHarness = Harness!.GetSagaStateMachineHarness<ProductStateMachine, ProductEntity>();
        InventorySagaHarness = Harness!.GetSagaStateMachineHarness<InventoryStateMachine, InventoryEntity>();
        
        return Task.CompletedTask;
    }

    protected async Task CreateExistingInventory(
        string partNumber = "TEST_PART_123",
        string description = "Test Description",
        int quantity = 50
    )
    {
        if (InventoryId != Guid.Empty)
            return;

        var service = ScopedProvider.ServiceProvider.GetRequiredService<MassTransitInventoryCommandService>();

        var result = await service.CreateAsync(partNumber, description, quantity);
        await InventorySagaHarness!.Created.Any();

        InventoryId = result.InventoryId;
    }

    protected async Task CreateExistingProduct(
        string sku = "TEST_SKU_123",
        string? name = "Test Product",
        ProductDimensions? dimensions = null,
        Guid? inventoryItemId = null,
        int? stockThreshold = 10,
        TimeSpan? leadTime = null
    )
    {
        if (ProductId != Guid.Empty)
            return;

        var service = ScopedProvider.ServiceProvider.GetRequiredService<MassTransitProductCommandService>();

        var testDimensions = dimensions ?? new ProductDimensions
        {
            Width = 10m,
            Height = 5m,
            Length = 15m,
            Weight = 2.5m
        };

        var result = await service.CreateAsync(sku, name, testDimensions, inventoryItemId, stockThreshold, leadTime);
        await ProductSagaHarness!.Created.Any();

        ProductId = result.ProductId;
    }

    protected async Task PublishInventoryEvent<TEvent>(object? eventData = null)
        where TEvent : class
    {
        var data = eventData ?? new
        {
            InventoryId = InventoryId ?? Guid.NewGuid(),
            StockQuantity = 50,
            Timestamp = Now
        };

        await Harness!.Bus.Publish<TEvent>(data);
        await Harness.Consumed.Any<TEvent>();
    }
}

[TestFixture]
public abstract class ProductService_Tests<TService> : TestBase
    where TService : class
{
    protected ISagaStateMachineTestHarness<InventoryStateMachine, InventoryEntity>? InventorySagaHarness;
    protected ISagaStateMachineTestHarness<ProductStateMachine, ProductEntity>? SagaHarness;
    protected TService Subject = null!;

    protected Guid TestId;
    protected string? TestSku;
    protected string? TestName;
    protected ProductDimensions? TestDimensions;
    protected Guid? TestInventoryItemId;
    protected int? TestStockThreshold;
    protected TimeSpan? TestLeadTime;

    protected DateTimeOffset Now = new(2025, 08, 01, 15, 55, 18, TimeSpan.Zero);
    protected DateTimeOffset Later = new(2025, 08, 01, 16, 55, 18, TimeSpan.Zero);
    protected DateTimeOffset InitialCreatedOn = new(2025, 08, 01, 15, 55, 18, TimeSpan.Zero);

    protected override void OnServicesCreating(IServiceCollection services)
    {
        services.AddScoped<MassTransitProductCommandService>();
        services.AddScoped<Func<DateTimeOffset>>(_ => () => Now);
    }

    protected override void OnHarnessCreating(IBusRegistrationConfigurator configurator)
    {
        configurator.AddSagaStateMachine<ProductStateMachine, ProductEntity>();
        configurator.AddSagaStateMachine<InventoryStateMachine, InventoryEntity>();
    }

    protected override Task OnPostSetup()
    {
        Subject = ScopedProvider.ServiceProvider.GetRequiredService<TService>();
        SagaHarness = Harness!.GetSagaStateMachineHarness<ProductStateMachine, ProductEntity>();
        InventorySagaHarness = Harness!.GetSagaStateMachineHarness<InventoryStateMachine, InventoryEntity>(); // Add this

        return Task.CompletedTask;
    }

    protected async Task CreateExistingProduct(
        string sku = "TEST_SKU_123",
        string? name = "Test Product",
        ProductDimensions? dimensions = null,
        Guid? inventoryItemId = null,
        int? stockThreshold = 10,
        TimeSpan? leadTime = null
    )
    {
        if (Subject is not MassTransitProductCommandService service)
            return;

        if (TestId != Guid.Empty)
            return;

        var testDimensions = dimensions ?? new ProductDimensions
        {
            Width = 10m,
            Height = 5m,
            Length = 15m,
            Weight = 2.5m
        };

        var result = await service.CreateAsync(sku, name, testDimensions, inventoryItemId, stockThreshold, leadTime);
        await SagaHarness!.Created.Any();

        TestId = result.ProductId;
    }

    protected async Task PublishInventoryEvent<TEvent>(object? eventData = null)
        where TEvent : class
    {
        var data = eventData ?? new
        {
            InventoryId = TestInventoryItemId ?? Guid.NewGuid(),
            StockQuantity = 50,
            Timestamp = Now
        };

        await Harness!.Bus.Publish<TEvent>(data);
    }
}