# MassTransit Test Harness Performance Optimization

## Problem

Tests were taking 5+ seconds each due to harness start/stop per test:

```csharp
[SetUp]  // ❌ PER TEST - 5 second cost
public async Task Setup()
{
    await Harness.Start();  // Expensive operation
    // ...
}

[TearDown]  // ❌ PER TEST
public async Task Teardown()
{
    await Harness.Stop();  // Expensive operation
}
```

## Solution: Follow MassTransit's Own Pattern

From `/c/Users/bryan/source/repos/MassTransit/MassTransit/src/MassTransit.TestFramework/InMemoryTestFixture.cs`:

```csharp
[OneTimeSetUp]  // ✅ ONCE per test class
public Task SetupInMemoryTestFixture()
{
    return InMemoryTestHarness.Start();
}

[OneTimeTearDown]  // ✅ ONCE per test class
public async Task TearDownInMemoryTestFixture()
{
    await InMemoryTestHarness.Stop();
    InMemoryTestHarness.Dispose();
}

[SetUp]  // Per test, but NO start/stop
public Task SetupInMemoryTest()
{
    return Task.CompletedTask;
}

[TearDown]  // Per test, but NO start/stop
public Task TearDownInMemoryTest()
{
    return Task.CompletedTask;
}
```

## Key Insights from MassTransit Source

1. **Harness lifecycle**: Start ONCE per test class, not per test
2. **Saga isolation**: Use unique `Guid` values per test (via `NewId.NextGuid()`)
3. **No saga cleanup**: MassTransit doesn't clear `InMemorySagaRepository` between tests
4. **Shared repository**: Tests in a class share the same saga repository instance

## Changes Applied to `/mnt/c/Users/bryan/source/bryanboettcher/KbStore/KbStore.Tests/EventingTestBase.cs`

```csharp
[OneTimeSetUp]
public async Task InitializeOnce()
{
    // Build service provider
    Services.AddMassTransitTestHarness(/* ... */);
    RootProvider = Services.BuildServiceProvider(true);
    Harness = RootProvider.GetRequiredService<ITestHarness>();

    // Start harness ONCE per test class
    await Harness.Start();
}

[SetUp]
public async Task Setup()
{
    // Create new scope per test for scoped services
    ScopedProvider = RootProvider!.CreateScope();

    Arrange();
    await Act();
}

[TearDown]
public async Task Teardown()
{
    // Dispose scope but DON'T stop the harness
    ScopedProvider?.Dispose();
}

[OneTimeTearDown]
public async Task FinalizeOnce()
{
    // Stop harness ONCE per test class
    if (Harness != null)
        await Harness.Stop();

    await DisposeAsync(Harness);
    await DisposeAsync(RootProvider);
}
```

## Removed Code

**Deleted reflection-based saga cleanup**:
- `RegisterSagaCleanup<TSaga>()` method
- `ClearAllSagas()` method
- `ClearSagaRepository()` method
- Static `SagaTypesToClear` list

MassTransit doesn't do this - they rely on unique correlation IDs.

## Performance Results

**Before**: ~5 seconds per test (25+ seconds for 5 tests)
**After**: ~1.5 seconds per test (7.5 seconds for 5 tests)

**Improvement**: ~70% faster test execution

## Test Isolation Strategy

Tests are isolated via:
1. **Unique correlation IDs**: Use `NewId.NextGuid()` for new saga instances
2. **Service scopes**: Fresh `IServiceScope` per test for scoped dependencies
3. **Acceptance of shared state**: Sagas accumulate in the repository across tests in a class (this is fine)

## Important Notes

- **Nested test classes**: Each nested class (e.g., `Product_Create.When_creating_without_inventory_link`) is a separate test fixture with its own harness instance
- **Handler registration**: Handlers registered via `OnHarnessCreating` are configured once during `OneTimeSetUp`
- **No cross-talk**: Each test class has isolated bus configuration

## Reference

- MassTransit source: `/c/Users/bryan/source/repos/MassTransit/MassTransit/src/MassTransit.TestFramework/InMemoryTestFixture.cs`
- MassTransit test examples: `/c/Users/bryan/source/repos/MassTransit/MassTransit/tests/MassTransit.Tests/SagaStateMachineTests/SimpleStateMachine_Specs.cs`
