// ReSharper disable UnusedMember.Global
// ReSharper disable UnassignedGetOnlyAutoProperty
#pragma warning disable CS8618, CS9264
namespace KbStore.Catalog.Domains.Inventory;

using Abstractions.Contracts;
using KbStore.Abstractions;
using MassTransit;

public sealed class InventoryStateMachine : MassTransitStateMachine<InventoryEntity>
{
    public InventoryStateMachine()
    {
        InstanceState(m => m.CurrentState,
            Available,
            OnHold,
            Backordered,
            Discontinued
        );

        Event(() => Created, e => e.CorrelateBy((s, c) => s.PartNumber == c.Message.PartNumber).SelectId(_ => NewId.NextSequentialGuid()));

        Event(() => QuantityIncreased, ConfigureEvent);
        Event(() => QuantityDecreased, ConfigureEvent);
        Event(() => DescriptionUpdated, ConfigureEvent);
        Event(() => Held, ConfigureEvent);
        Event(() => Released, ConfigureEvent);
        Event(() => Deleted, ConfigureEvent);
        Event(() => StatusRequested, ConfigureEvent);

        Initially(
            When(Created, context => context.Message.StockQuantity < 0)
                .RespondAsync(Failure("Stock quantity cannot be negative", FailureTypes.Validation)),

            When(Created, context => context.Message.StockQuantity >= 0)
                .Then(SetProperties)
                .Then(UpdateTimestamp)
                .TransitionTo(Available)
                .RespondAsync(Message<CreateInventoryResponse>)
                .PublishAsync(Message<InventoryCreated>)
        );

        During(Available,
            When(Created)
                .RespondAsync(context => Failure($"Inventory item with part number '{context.Message.PartNumber}' already exists", FailureTypes.Conflict)(context)),

            When(QuantityIncreased)
                .Then(context => context.Saga.StockQuantity += context.Message.Amount)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<UpdateInventoryResponse>)
                .PublishAsync(Message<InventoryQuantityIncreased>),

            When(QuantityDecreased, context => context.Message.Amount > context.Saga.StockQuantity)
                .RespondAsync(context => Failure($"Cannot decrease quantity by {context.Message.Amount}. Current stock: {context.Saga.StockQuantity}", FailureTypes.InvalidState)(context)),

            When(QuantityDecreased, context => context.Message.Amount <= context.Saga.StockQuantity)
                .Then(context => context.Saga.StockQuantity -= context.Message.Amount)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<UpdateInventoryResponse>)
                .PublishAsync(Message<InventoryQuantityDecreased>),

            When(DescriptionUpdated)
                .Then(context => context.Saga.Description = context.Message.Description)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<UpdateInventoryResponse>)
                .PublishAsync(Message<InventoryDescriptionUpdated>),

            When(Held)
                .TransitionTo(OnHold)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<HoldInventoryResponse>)
                .PublishAsync(Message<InventoryHeld>),

            When(Deleted)
                .TransitionTo(Discontinued)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<DeleteInventoryResponse>)
                .PublishAsync(Message<InventoryDiscontinued>)
        );

        During(OnHold,
            When(DescriptionUpdated)
                .Then(context => context.Saga.Description = context.Message.Description)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<UpdateInventoryResponse>)
                .PublishAsync(Message<InventoryDescriptionUpdated>),

            When(Released)
                .TransitionTo(Available)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<ReleaseInventoryResponse>)
                .PublishAsync(Message<InventoryReleased>),

            When(Deleted)
                .TransitionTo(Discontinued)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<DeleteInventoryResponse>)
                .PublishAsync(Message<InventoryDiscontinued>),
            
            // Reject quantity changes while on hold
            When(QuantityIncreased)
                .RespondAsync(context => Failure("Cannot modify quantity while inventory item is on hold", FailureTypes.InvalidState)(context)),

            When(QuantityDecreased)
                .RespondAsync(context => Failure("Cannot modify quantity while inventory item is on hold", FailureTypes.InvalidState)(context))
        );

        During(Discontinued,
            
            When(Deleted)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<DeleteInventoryResponse>)
                .PublishAsync(Message<InventoryDeleted>)
                .Finalize(),

            // Reject all other operations on discontinued items
            When(QuantityIncreased)
                .RespondAsync(context => Failure("Cannot modify discontinued inventory item", FailureTypes.InvalidState)(context)),

            When(QuantityDecreased)
                .RespondAsync(context => Failure("Cannot modify discontinued inventory item", FailureTypes.InvalidState)(context)),

            When(DescriptionUpdated)
                .RespondAsync(context => Failure("Cannot modify discontinued inventory item", FailureTypes.InvalidState)(context)),

            When(Held)
                .RespondAsync(context => Failure("Cannot hold discontinued inventory item", FailureTypes.InvalidState)(context))
        );

        During(Backordered,
            
            When(DescriptionUpdated)
                .Then(context => context.Saga.Description = context.Message.Description)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<UpdateInventoryResponse>)
                .PublishAsync(Message<InventoryDescriptionUpdated>),

            When(QuantityIncreased)
                .Then(context => context.Saga.StockQuantity += context.Message.Amount)
                .Then(UpdateTimestamp)
                .TransitionTo(Available)
                .RespondAsync(Message<UpdateInventoryResponse>)
                .PublishAsync(Message<InventoryQuantityIncreased>),

            When(QuantityDecreased)
                .RespondAsync(context => Failure("Cannot decrease quantity of backordered inventory item", FailureTypes.InvalidState)(context)),

            When(Held)
                .RespondAsync(context => Failure("Cannot hold backordered inventory item", FailureTypes.InvalidState)(context)),

            When(Deleted)
                .TransitionTo(Discontinued)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<DeleteInventoryResponse>)
                .PublishAsync(Message<InventoryDiscontinued>)
        );

        DuringAny(
            When(StatusRequested)
                .RespondAsync(Message<InventoryStatusResponse>)
        );
    }

    public Event<CreateInventoryRequest> Created { get; }
    public Event<IncreaseInventoryQuantityRequest> QuantityIncreased { get; }
    public Event<DecreaseInventoryQuantityRequest> QuantityDecreased { get; }
    public Event<UpdateInventoryDescriptionRequest> DescriptionUpdated { get; }
    public Event<HoldInventoryRequest> Held { get; }
    public Event<ReleaseInventoryRequest> Released { get; }
    public Event<DeleteInventoryRequest> Deleted { get; }
    public Event<InventoryStatusRequest> StatusRequested { get; }

    public State Available { get; }
    public State OnHold { get; }
    public State Backordered { get; }
    public State Discontinued { get; }

    private static void SetProperties(BehaviorContext<InventoryEntity, CreateInventoryRequest> context)
    {
        context.Saga.PartNumber = context.Message.PartNumber;
        context.Saga.Description = context.Message.Description;
        context.Saga.StockQuantity = context.Message.StockQuantity;
        context.Saga.CreatedOn = context.Message.Timestamp;
    }

    private static void UpdateTimestamp(BehaviorContext<InventoryEntity, InventoryCommand> context)
    {
        context.Saga.UpdatedOn = context.Message.Timestamp;
    }

    private static Task<SendTuple<TMessage>> Message<TMessage>(BehaviorContext<InventoryEntity> context)
        where TMessage : class, InventoryModel
        => context.Init<TMessage>(new
        {
            InventoryId = context.Saga.CorrelationId,
            Status = CalculateInventoryStatus(context.Saga),
            context.Saga.PartNumber,
            context.Saga.Description,
            context.Saga.StockQuantity,
            context.Saga.CreatedOn,
            context.Saga.UpdatedOn
        });

    private static Func<BehaviorContext<InventoryEntity>, Task<SendTuple<InventoryFailure>>> Failure(string message, FailureTypes failureType)
        => context => context.Init<InventoryFailure>(new
        {
            Message = message,
            FailureType = failureType
        });

    private static void ConfigureEvent<TMessage>(IEventCorrelationConfigurator<InventoryEntity, TMessage> conf)
        where TMessage : class, InventoryCommand
    {
        conf.CorrelateById(c => c.Message.InventoryId);
        conf.OnMissingInstance(b => b.Execute(c => c.RespondAsync<InventoryFailure>(new
        {
            Message = $"Instance not found for {c.Message.InventoryId:D}",
            FailureType = FailureTypes.Missing
        })));
    }

    /// <summary>
    /// Maps MassTransit internal state numbers to InventoryStatus enum values.
    /// MassTransit reserves states 1 and 2 internally, so our states start at 3.
    /// </summary>
    private static InventoryStatus CalculateInventoryStatus(InventoryEntity entity)
    {
        return entity.CurrentState switch
        {
            3 => InventoryStatus.Available,    // Available state
            4 => InventoryStatus.Held,         // OnHold state  
            5 => InventoryStatus.Backordered,  // Backordered state
            6 => InventoryStatus.Discontinued, // Discontinued state
            _ => InventoryStatus.Invalid
        };
    }
}