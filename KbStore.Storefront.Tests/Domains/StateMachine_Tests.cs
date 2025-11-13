namespace KbStore.Storefront.Tests.Domains;

using KbStore.Storefront.Domains.SellableItems;
using KbStore.Tests;
using MassTransit;
using MassTransit.Testing;

public abstract class StateMachine_Tests<TStateMachine, TSaga> : EventingTestBase
    where TStateMachine : class, SagaStateMachine<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    protected ISagaStateMachineTestHarness<TStateMachine, TSaga> SagaHarness = null!;

    protected static readonly Guid ExistingId = Guid.Parse("abcd1234-bbbb-cccc-dddd-deadbeef0001");
    protected static readonly Guid LinkedId   = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-01234567dcba");

    protected static readonly DateTime Now = new(2025, 06, 16, 13, 30, 00, DateTimeKind.Utc);
    protected static readonly DateTime Later = new(2025, 06, 16, 14, 00, 00, DateTimeKind.Utc);

    protected override void OnHarnessCreating(IBusRegistrationConfigurator configurator)
    {
        configurator.AddSagaStateMachine<TStateMachine, TSaga>().InMemoryRepository();
    }

    protected override Task OnPostSetup()
    {
        SagaHarness = Harness.GetSagaStateMachineHarness<TStateMachine, TSaga>();
        return Task.CompletedTask;
    }
}
