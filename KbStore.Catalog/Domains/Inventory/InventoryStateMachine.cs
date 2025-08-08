// ReSharper disable UnusedMember.Global
// ReSharper disable UnassignedGetOnlyAutoProperty
#pragma warning disable CS8618, CS9264
namespace KbStore.Catalog.Domains.Inventory;

using Abstractions.Contracts;
using KbStore.Abstractions;
using MassTransit;

public sealed class InventoryStateMachine : MassTransitStateMachine<InventoryEntity>
{
    private enum Failures
    {
        CreateNegativeQuantity,
        DuplicatePartNumber,
        InsufficientQuantity,
        AlreadyHeld,
        ModifyHeld,
        NotHeld,
        ModifyDiscontinued,
        HoldDiscontinued,
        ModifyBackordered,
        HoldBackordered
    }


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
                .RespondAsync(Failure(Failures.CreateNegativeQuantity)),

            When(Created, context => context.Message.StockQuantity >= 0)
                .Then(SetProperties)
                .Then(UpdateTimestamp)
                .TransitionTo(Available)
                .RespondAsync(Message<CreateInventoryResponse>)
                .PublishAsync(Message<InventoryCreated>)
        );

        During(Available,
            When(Created)
                .RespondAsync(context => Failure(Failures.DuplicatePartNumber)(context)),

            When(QuantityIncreased)
                .Then(context => context.Saga.StockQuantity += context.Message.Amount)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<UpdateInventoryResponse>)
                .PublishAsync(Message<InventoryQuantityIncreased>),

            When(QuantityDecreased, context => context.Message.Amount > context.Saga.StockQuantity)
                .RespondAsync(context => Failure(Failures.InsufficientQuantity)(context)),

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

            When(Released)
                .RespondAsync(context => Failure(Failures.NotHeld)(context)),

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

            When(Held)
                .RespondAsync(context => Failure(Failures.AlreadyHeld)(context)),
            
            // Reject quantity changes while on hold
            When(QuantityIncreased)
                .RespondAsync(context => Failure(Failures.ModifyHeld)(context)),

            When(QuantityDecreased)
                .RespondAsync(context => Failure(Failures.ModifyHeld)(context))
        );

        During(Discontinued,
            
            When(Deleted)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<DeleteInventoryResponse>)
                .PublishAsync(Message<InventoryDeleted>)
                .Finalize(),

            When(QuantityIncreased)
                .RespondAsync(context => Failure(Failures.ModifyDiscontinued)(context)),

            When(QuantityDecreased)
                .RespondAsync(context => Failure(Failures.ModifyDiscontinued)(context)),

            When(DescriptionUpdated)
                .RespondAsync(context => Failure(Failures.ModifyDiscontinued)(context)),

            When(Held)
                .RespondAsync(context => Failure(Failures.HoldDiscontinued)(context))
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
                .RespondAsync(context => Failure(Failures.ModifyBackordered)(context)),

            When(Held)
                .RespondAsync(context => Failure(Failures.HoldBackordered)(context)),

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

    private static Func<BehaviorContext<InventoryEntity>, Task<SendTuple<InventoryFailure>>> Failure(Failures failure)
    {
        return context => context.Init<InventoryFailure>(MapFailure(context.Saga, failure));

        static object MapFailure(InventoryEntity entity, Failures failure)
        {
            var currentState = CalculateInventoryStatus(entity);

            return failure switch
            {
                Failures.CreateNegativeQuantity => new
                {
                    FailureType = FailureTypes.Validation,
                    CurrentState = "Missing",
                    Operation = "Create",
                    Message = "Stock quantity cannot be negative"
                },
                Failures.DuplicatePartNumber => new
                {
                    FailureType = FailureTypes.Conflict,
                    CurrentState = currentState.ToString(),
                    Operation = "Create",
                    Message = $"Inventory item with part number '{entity.PartNumber}' already exists"
                },
                Failures.InsufficientQuantity => new
                {
                    FailureType = FailureTypes.InvalidState,
                    CurrentState = currentState.ToString(),
                    Operation = "Modify",
                    Message = $"Cannot decrease quantity. Current stock: {entity.StockQuantity}"
                },
                Failures.AlreadyHeld => new
                {
                    FailureType = FailureTypes.InvalidState,
                    CurrentState = currentState.ToString(),
                    Operation = "Hold",
                    Message = "Inventory item is already on hold"
                },
                Failures.ModifyHeld => new
                {
                    FailureType = FailureTypes.InvalidState,
                    CurrentState = currentState.ToString(),
                    Operation = "Modify",
                    Message = "Cannot modify quantity while inventory item is on hold"
                },
                Failures.NotHeld => new
                {
                    FailureType = FailureTypes.InvalidState,
                    CurrentState = currentState.ToString(),
                    Operation = "Release",
                    Message = "Cannot release an inventory item not on hold"
                },
                Failures.ModifyDiscontinued => new
                {
                    FailureType = FailureTypes.InvalidState,
                    CurrentState = currentState.ToString(),
                    Operation = "Modify",
                    Message = "Cannot modify discontinued inventory item"
                },
                Failures.HoldDiscontinued => new
                {
                    FailureType = FailureTypes.InvalidState,
                    CurrentState = currentState.ToString(),
                    Operation = "Hold",
                    Message = "Cannot hold discontinued inventory item"
                },
                Failures.ModifyBackordered => new
                {
                    FailureType = FailureTypes.InvalidState,
                    CurrentState = currentState.ToString(),
                    Operation = "Modify",
                    Message = "Cannot decrease quantity of backordered inventory item"
                },
                Failures.HoldBackordered => new
                {
                    FailureType = FailureTypes.InvalidState,
                    CurrentState = currentState.ToString(),
                    Operation = "Hold",
                    Message = "Cannot hold backordered inventory item"
                },
                _ => throw new ArgumentOutOfRangeException(nameof(failure), failure, null)
            };
        }
    }

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