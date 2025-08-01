// ReSharper disable UnusedMember.Global
// ReSharper disable UnassignedGetOnlyAutoProperty
#pragma warning disable CS8618, CS9264
namespace KbStore.Inventory.Domains.StockItems;

using Abstractions.Contracts;
using MassTransit;


public sealed class StockItemStateMachine : MassTransitStateMachine<StockItemEntity>
{
    public StockItemStateMachine()
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
                .RespondAsync(Message<CreateStockItemResponse>)
                .PublishAsync(Message<StockItemCreated>)
        );

        During(Available,

            When(QuantityUpdated)
                .Then(c => c.Saga.StockQuantity = c.Message.StockQuantity)
                .RespondAsync(Message<UpdateStockItemResponse>)
                .PublishAsync(Message<StockItemQuantityUpdated>),

            When(DescriptionUpdated)
                .Then(c => c.Saga.Description = c.Message.Description)
                .RespondAsync(Message<UpdateStockItemResponse>)
                .PublishAsync(Message<StockItemQuantityUpdated>),

            When(Held)
                .TransitionTo(OnHold)
                .RespondAsync(Message<HoldStockItemResponse>)
                .PublishAsync(Message<StockItemHeld>),

            When(Deleted)
                .TransitionTo(Discontinued)
                .RespondAsync(Message<DeleteStockItemResponse>)
                .PublishAsync(Message<StockItemDiscontinued>)
        );

        During(OnHold,
            When(Released)
                .TransitionTo(Available)
                .RespondAsync(Message<ReleaseStockItemResponse>)
                .PublishAsync(Message<StockItemReleased>)
        );
        
        During(Discontinued,
            When(Deleted)
                .RespondAsync(Message<DeleteStockItemResponse>)
                .PublishAsync(Message<StockItemDeleted>)
                .Finalize()
        );
    }

    public Event<CreateStockItemRequest> Created { get; }
    public Event<UpdateStockItemQuantityRequest> QuantityUpdated { get; }
    public Event<UpdateStockItemDescriptionRequest> DescriptionUpdated { get; }
    public Event<HoldStockItemRequest> Held { get; }
    public Event<ReleaseStockItemRequest> Released { get; }
    public Event<DeleteStockItemRequest> Deleted { get; }
    
    public State Available { get; }
    public State OnHold { get; }
    public State Backordered { get; }
    public State Discontinued { get; }

    static void SetProperties(BehaviorContext<StockItemEntity, CreateStockItemRequest> context)
    {
        context.Saga.PartNumber = context.Message.PartNumber;
        context.Saga.Description = context.Message.Description;
        context.Saga.StockQuantity = context.Message.StockQuantity;
    }

    static Task<SendTuple<TMessage>> Message<TMessage>(BehaviorContext<StockItemEntity> context)
        where TMessage : class, BaseStockItemEvent 
        => context.Init<TMessage>(context.Saga);
}