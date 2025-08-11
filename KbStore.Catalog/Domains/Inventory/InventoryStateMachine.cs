using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Abstractions.Exceptions;
using KbStore.Catalog.Domains.Inventory;
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

        Event(() => Created, e => e.CorrelateBy((s, c) => s.PartNumber == c.Message.PartNumber));
        Event(() => QuantityIncreased, e => e.CorrelateById(s => s.Message.InventoryId));
        Event(() => QuantityDecreased, e => e.CorrelateById(s => s.Message.InventoryId));
        Event(() => DescriptionUpdated, e => e.CorrelateById(s => s.Message.InventoryId));
        Event(() => Held, e => e.CorrelateById(s => s.Message.InventoryId));
        Event(() => Released, e => e.CorrelateById(s => s.Message.InventoryId));
        Event(() => Deleted, e => e.CorrelateById(s => s.Message.InventoryId));
        Event(() => StatusRequested, e => e.CorrelateById(s => s.Message.InventoryId));

        Initially(
            When(Created, context => context.Message.StockQuantity < 0)
                .Then(context => throw InventoryValidationException.InvalidQuantity(context.Message.StockQuantity)),

            When(Created, context => string.IsNullOrWhiteSpace(context.Message.PartNumber))
                .Then(context => throw InventoryValidationException.InvalidPartNumber(context.Message.PartNumber)),

            When(Created, context => string.IsNullOrWhiteSpace(context.Message.Description))
                .Then(context => throw InventoryValidationException.InvalidDescription(context.Message.Description)),

            When(Created)
                .Then(SetProperties)
                .TransitionTo(Available)
                .RespondAsync(Response<CreateInventoryResponse>)
                .PublishAsync(Message<InventoryCreated>)
        );

        During(Available,
            // Handle duplicate creation attempts
            When(Created)
                .Then(context => throw InventoryConflictException.DuplicatePartNumber(context.Message.PartNumber)),

            When(QuantityIncreased)
                .Then(context => context.Saga.StockQuantity += context.Message.Amount)
                .RespondAsync(Response<UpdateInventoryResponse>)
                .PublishAsync(Message<InventoryQuantityIncreased>),

            When(QuantityDecreased, context => context.Message.Amount > context.Saga.StockQuantity)
                .Then(context => throw InventoryValidationException.InsufficientStock(
                    context.Message.Amount,
                    context.Saga.StockQuantity,
                    context.Saga.CorrelationId)),

            When(QuantityDecreased)
                .Then(context => context.Saga.StockQuantity -= context.Message.Amount)
                .RespondAsync(Response<UpdateInventoryResponse>)
                .PublishAsync(Message<InventoryQuantityDecreased>),

            When(DescriptionUpdated)
                .Then(context => context.Saga.Description = context.Message.Description)
                .RespondAsync(Response<UpdateInventoryResponse>)
                .PublishAsync(Message<InventoryDescriptionUpdated>),

            When(Held)
                .TransitionTo(OnHold)
                .RespondAsync(Response<HoldInventoryResponse>)
                .PublishAsync(Message<InventoryHeld>),

            When(Deleted)
                .TransitionTo(Discontinued)
                .RespondAsync(Response<DeleteInventoryResponse>)
                .PublishAsync(Message<InventoryDiscontinued>),

            When(StatusRequested)
                .RespondAsync(Response<InventoryStatusResponse>)
        );

        During(OnHold,
            // Only allow description updates and release while on hold
            When(DescriptionUpdated)
                .Then(context => context.Saga.Description = context.Message.Description)
                .RespondAsync(Response<UpdateInventoryResponse>)
                .PublishAsync(Message<InventoryDescriptionUpdated>),

            When(Released)
                .TransitionTo(Available)
                .RespondAsync(Response<ReleaseInventoryResponse>)
                .PublishAsync(Message<InventoryReleased>),

            When(Deleted)
                .TransitionTo(Discontinued)
                .RespondAsync(Response<DeleteInventoryResponse>)
                .PublishAsync(Message<InventoryDiscontinued>),

            When(StatusRequested)
                .RespondAsync(Response<InventoryStatusResponse>),

            // Reject quantity changes while on hold
            When(QuantityIncreased)
                .Then(context => throw InventoryStateException.CannotModifyHeldItem(context.Saga.CorrelationId, "IncreaseQuantity")),

            When(QuantityDecreased)
                .Then(context => throw InventoryStateException.CannotModifyHeldItem(context.Saga.CorrelationId, "DecreaseQuantity")),

            When(Held)
                .Then(context => throw InventoryStateException.AlreadyHeld(context.Saga.CorrelationId))
        );

        During(Discontinued,
            When(StatusRequested)
                .RespondAsync(Response<InventoryStatusResponse>),

            When(Deleted)
                .RespondAsync(Response<DeleteInventoryResponse>)
                .PublishAsync(Message<InventoryDeleted>)
                .Finalize(),

            // Reject all other operations on discontinued items
            When(QuantityIncreased)
                .Then(context => throw InventoryStateException.CannotModifyDiscontinuedItem(context.Saga.CorrelationId, "IncreaseQuantity")),

            When(QuantityDecreased)
                .Then(context => throw InventoryStateException.CannotModifyDiscontinuedItem(context.Saga.CorrelationId, "DecreaseQuantity")),

            When(DescriptionUpdated)
                .Then(context => throw InventoryStateException.CannotModifyDiscontinuedItem(context.Saga.CorrelationId, "UpdateDescription")),

            When(Held)
                .Then(context => throw InventoryStateException.CannotModifyDiscontinuedItem(context.Saga.CorrelationId, "Hold"))
        );

        // Handle Backordered state (minimal implementation for now)
        During(Backordered,
            When(StatusRequested)
                .RespondAsync(Response<InventoryStatusResponse>),

            // Allow description updates
            When(DescriptionUpdated)
                .Then(context => context.Saga.Description = context.Message.Description)
                .RespondAsync(Response<UpdateInventoryResponse>)
                .PublishAsync(Message<InventoryDescriptionUpdated>),

            // Allow quantity increases (restocking)
            When(QuantityIncreased)
                .Then(context => context.Saga.StockQuantity += context.Message.Amount)
                .TransitionTo(Available) // Return to available when restocked
                .RespondAsync(Response<UpdateInventoryResponse>)
                .PublishAsync(Message<InventoryQuantityIncreased>),

            // Reject other operations
            When(QuantityDecreased)
                .Then(context => throw InventoryStateException.CannotModifyBackorderedItem(context.Saga.CorrelationId, "DecreaseQuantity")),

            When(Held)
                .Then(context => throw InventoryStateException.CannotModifyBackorderedItem(context.Saga.CorrelationId, "Hold")),

            When(Released)
                .Then(context => throw InventoryStateException.NotHeld(context.Saga.CorrelationId)),

            When(Deleted)
                .TransitionTo(Discontinued)
                .RespondAsync(Response<DeleteInventoryResponse>)
                .PublishAsync(Message<InventoryDiscontinued>)
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
    }

    private static Task<SendTuple<TMessage>> Message<TMessage>(BehaviorContext<InventoryEntity> context)
        where TMessage : class, BaseInventoryEvent
        => context.Init<TMessage>(new
        {
            InventoryId = context.Saga.CorrelationId,
            Status = CalculateInventoryStatus(context.Saga),
            context.Saga.PartNumber,
            context.Saga.Description,
            context.Saga.StockQuantity
        });

    private static Task<SendTuple<TMessage>> Response<TMessage>(BehaviorContext<InventoryEntity> context)
        where TMessage : class, InventoryModel
        => context.Init<TMessage>(new
        {
            InventoryId = context.Saga.CorrelationId,
            Status = CalculateInventoryStatus(context.Saga),
            context.Saga.PartNumber,
            context.Saga.Description,
            context.Saga.StockQuantity
        });

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