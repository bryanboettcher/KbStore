// ReSharper disable UnusedMember.Global
// ReSharper disable UnassignedGetOnlyAutoProperty
#pragma warning disable CS8618, CS9264
namespace KbStore.Services.Domains.Inventory;

using KbStore.Contracts.Domains;
using MassTransit;

public sealed class InventoryStateMachine : MassTransitStateMachine<InventorySaga>
{
    public InventoryStateMachine()
    {
        InstanceState(m => m.CurrentState,
            Available,
            OnHold,
            Backordered,
            Discontinued
        );

        Event(() => Created, e => e.CorrelateBy((s, c) => s.PartNumber == c.Message.PartNumber));

        Initially(
            When(Created)
                .Then(SetProperties)
                .TransitionTo(Available)
                .RespondAsync(Message<CreateInventoryResponse>)
                .PublishAsync(Message<InventoryCreated>)
        );

        During(Available,
            When(QuantityUpdated)
                .Then(c => c.Saga.StockQuantity = c.Message.StockQuantity)
                .RespondAsync(Message<UpdateInventoryResponse>)
                .PublishAsync(Message<InventoryQuantityUpdated>)
        );

        During(Available,
            When(Held)
                .TransitionTo(OnHold)
                .RespondAsync(Message<HoldInventoryResponse>)
                .PublishAsync(Message<InventoryHeld>)
        );

        During(OnHold,
            When(Released)
                .TransitionTo(Available)
                .RespondAsync(Message<ReleaseInventoryResponse>)
                .PublishAsync(Message<InventoryReleased>)
        );

        During(Available,
            When(Deleted)
                .TransitionTo(Discontinued)
                .RespondAsync(Message<DeleteInventoryResponse>)
                .PublishAsync(Message<InventoryDiscontinued>)
        );

        During(Discontinued,
            When(Deleted)
                .RespondAsync(Message<DeleteInventoryResponse>)
                .PublishAsync(Message<InventoryDeleted>)
                .Finalize()
        );
    }

    public Event<CreateInventoryRequest> Created { get; }
    public Event<UpdateInventoryQuantityRequest> QuantityUpdated { get; }
    public Event<HoldInventoryRequest> Held { get; }
    public Event<ReleaseInventoryRequest> Released { get; }
    public Event<DeleteInventoryRequest> Deleted { get; }
    
    public State Available { get; }
    public State OnHold { get; }
    public State Backordered { get; }
    public State Discontinued { get; }

    static void SetProperties(BehaviorContext<InventorySaga, CreateInventoryRequest> context)
    {
        context.Saga.PartNumber = context.Message.PartNumber;
        context.Saga.Description = context.Message.Description;
        context.Saga.StockQuantity = context.Message.StockQuantity;
        context.Saga.InventoryStatus = context.Message.InventoryStatus;
    }

    static Task<SendTuple<TMessage>> Message<TMessage>(BehaviorContext<InventorySaga> context)
        where TMessage : class, BaseInventoryEvent 
        => context.Init<TMessage>(context.Saga);
}