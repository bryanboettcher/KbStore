
// ReSharper disable UnassignedGetOnlyAutoProperty
#pragma warning disable CS8618, CS9264


namespace KbStore.Catalog.Domains.Inventory;

using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Abstractions.Exceptions;
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
        Event(() => StatusRequested, e => e.CorrelateById(c => c.Message.InventoryId).OnMissingInstance(b => b.Execute(c => throw new InventoryNotFoundException(c.Message.InventoryId))));
        
        Event(() => QuantityIncreased, ConfigureEvent);
        Event(() => QuantityDecreased, ConfigureEvent);
        Event(() => DescriptionUpdated, ConfigureEvent);
        Event(() => Held, ConfigureEvent);
        Event(() => Released, ConfigureEvent);
        Event(() => Deleted, ConfigureEvent);
        
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
                .Then(UpdateTimestamp)
                .RespondAsync(Message<CreateInventoryResponse>)
                .PublishAsync(Message<InventoryCreated>)
        );

        During(Available,
            // Handle duplicate creation attempts
            When(Created)
                .Then(context => throw InventoryConflictException.DuplicatePartNumber(context.Message.PartNumber)),

            When(QuantityIncreased)
                .Then(context => context.Saga.StockQuantity += context.Message.Amount)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<UpdateInventoryResponse>)
                .PublishAsync(Message<InventoryQuantityIncreased>),

            When(QuantityDecreased, context => context.Message.Amount > context.Saga.StockQuantity)
                .Then(context => throw InventoryValidationException.InsufficientStock(
                    context.Message.Amount,
                    context.Saga.StockQuantity,
                    context.Saga.CorrelationId)),

            When(QuantityDecreased)
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
                .Then(context => throw InventoryStateException.NotHeld(context.Saga.CorrelationId)),

            When(Deleted)
                .TransitionTo(Discontinued)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<DeleteInventoryResponse>)
                .PublishAsync(Message<InventoryDiscontinued>)
        );

        During(OnHold,
            // Only allow description updates and release while on hold
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
                .Then(context => throw InventoryStateException.CannotModifyHeldItem(context.Saga.CorrelationId, "IncreaseQuantity")),

            When(QuantityDecreased)
                .Then(context => throw InventoryStateException.CannotModifyHeldItem(context.Saga.CorrelationId, "DecreaseQuantity")),

            When(Held)
                .Then(context => throw InventoryStateException.AlreadyHeld(context.Saga.CorrelationId))
        );

        During(Discontinued,
            
            When(Deleted)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<DeleteInventoryResponse>)
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
            
            // Allow description updates
            When(DescriptionUpdated)
                .Then(context => context.Saga.Description = context.Message.Description)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<UpdateInventoryResponse>)
                .PublishAsync(Message<InventoryDescriptionUpdated>),

            // Allow quantity increases (restocking)
            When(QuantityIncreased)
                .Then(context => context.Saga.StockQuantity += context.Message.Amount)
                .TransitionTo(Available) // Return to available when restocked
                .Then(UpdateTimestamp)
                .RespondAsync(Message<UpdateInventoryResponse>)
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
            context.Saga.Status,
            context.Saga.PartNumber,
            context.Saga.Description,
            context.Saga.StockQuantity,
            context.Saga.CreatedOn,
            context.Saga.UpdatedOn
        });
    
    private static void ConfigureEvent<TMessage>(IEventCorrelationConfigurator<InventoryEntity, TMessage> conf)
        where TMessage : class, InventoryCommand
    {
        conf.CorrelateById(s => s.Message.InventoryId);
        conf.OnMissingInstance(b => b.ExecuteAsync(c => throw new InventoryNotFoundException(c.Message.InventoryId)));
    }
}