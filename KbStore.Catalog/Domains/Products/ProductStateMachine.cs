// ReSharper disable UnassignedGetOnlyAutoProperty
#pragma warning disable CS8618, CS9264, CS8602

namespace KbStore.Catalog.Domains.Products;

using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Abstractions.Exceptions;
using MassTransit;

public sealed class ProductStateMachine : MassTransitStateMachine<ProductEntity>
{
    public ProductStateMachine(ILogger<ProductStateMachine> logger)
    {
        InstanceState(m => m.CurrentState,
            Enabled,
            Disabled,
            Discontinued
        );

        Request(() => InventoryStatus, s => s.InventoryStatusId, c =>
        {
            c.Timeout = TimeSpan.FromSeconds(1);
            c.Completed = m => m.OnMissingInstance(b => b.Discard());
            c.Faulted = m => m.OnMissingInstance(b => b.Discard());
            c.TimeoutExpired = m => m.OnMissingInstance(b => b.Discard());
        });

        Event(() => Created, e => e.CorrelateBy((s, c) => s.Sku == c.Message.Sku).SelectId(_ => NewId.NextSequentialGuid()));
        Event(() => StatusRequested, e => e.CorrelateById(c => c.Message.ProductId).OnMissingInstance(b => b.Execute(c => throw new ProductNotFoundException(c.Message.ProductId))));

        Event(() => NameUpdated, ConfigureEvent);
        Event(() => DimensionsUpdated, ConfigureEvent);
        Event(() => StockThresholdUpdated, ConfigureEvent);
        Event(() => LeadTimeUpdated, ConfigureEvent);
        Event(() => EnableRequested, ConfigureEvent);
        Event(() => DisableRequested, ConfigureEvent);
        Event(() => Deleted, ConfigureEvent);

        Event(() => InventoryQuantityChanged, e => e.CorrelateBy((s, c) => s.InventoryId == c.Message.InventoryId));
        Event(() => InventoryDiscontinued, e => e.CorrelateBy((s, c) => s.InventoryId == c.Message.InventoryId));
        Event(() => InventoryDeleted, e => e.CorrelateBy((s, c) => s.InventoryId == c.Message.InventoryId));
        Event(() => InventoryHeld, e => e.CorrelateBy((s, c) => s.InventoryId == c.Message.InventoryId));
        Event(() => InventoryReleased, e => e.CorrelateBy((s, c) => s.InventoryId == c.Message.InventoryId));

        Initially(
            
            When(Created)
                .Then(ctx => logger.LogInformation("Creating new Product"))
                .Then(SetProperties)
                .Then(UpdateTimestamp)
                .IfElse(ctx => ctx.Saga.InventoryId is not null,
                    
                    t => t.Then(ctx => logger.LogWarning("Calling to Inventory to verify status"))
                        .TransitionTo(InventoryStatus.Pending)
                        .Request(InventoryStatus, c => c.Init<InventoryStatusRequest>(new { c.Saga.InventoryId })),
                        
                    f => f.Then(ctx => logger.LogInformation("No Inventory is attached, finishing creation"))
                        .Then(context => context.Saga.IsStocked = true)   // non-inventory products are always stocked unless disabled
                        .TransitionTo(Enabled)
                        .PublishAsync(Message<ProductCreated>)
                )
                .RespondAsync(Message<CreateProductResponse>)
        );

        During(InventoryStatus.Pending,

            When(InventoryStatus.Completed)
                .Then(ctx => logger.LogInformation("Inventory successful"))
                .Then(context => context.Saga.StockQuantity = context.Message.StockQuantity)
                .TransitionTo(Enabled)
                .PublishAsync(Message<ProductCreated>),

            When(InventoryStatus.Faulted)
                .Then(ctx => logger.LogWarning("Inventory faulted"))
                .Then(context => context.Saga.StockQuantity = 0)
                .TransitionTo(Disabled)
                .PublishAsync(Message<ProductCreated>),

            When(InventoryStatus.TimeoutExpired)
                .Then(ctx => logger.LogWarning("Inventory timed out"))
                .Then(context => context.Saga.StockQuantity = 0)
                .TransitionTo(Disabled)
                .PublishAsync(Message<ProductCreated>)

        );

        During(Enabled,
            // Handle duplicate creation attempts
            When(Created)
                .Then(context => throw ProductConflictException.DuplicateSku(context.Message.Sku)),

            When(NameUpdated)
                .Then(ctx => logger.LogInformation("Updating Product name to {name}", ctx.Message.Name))
                .Then(context => context.Saga.Name = context.Message.Name)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<UpdateProductResponse>)
                .PublishAsync(Message<ProductNameUpdated>),

            When(DimensionsUpdated)
                .Then(context => UpdateDimensions(context.Saga, context.Message.Dimensions))
                .Then(UpdateTimestamp)
                .RespondAsync(Message<UpdateProductResponse>)
                .PublishAsync(Message<ProductDimensionsUpdated>),

            When(StockThresholdUpdated)
                .Then(ctx => logger.LogInformation("Updating stock threshold to {threshold}", ctx.Message.StockThreshold))
                .Then(context => context.Saga.StockThreshold = context.Message.StockThreshold)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<UpdateProductResponse>)
                .PublishAsync(Message<ProductStockThresholdUpdated>),

            When(LeadTimeUpdated)
                .Then(context => context.Saga.LeadTime = context.Message.LeadTime)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<UpdateProductResponse>)
                .PublishAsync(Message<ProductLeadTimeUpdated>),

            When(DisableRequested)
                .TransitionTo(Disabled)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<DisableProductResponse>)
                .PublishAsync(Message<ProductDisabled>),

            When(EnableRequested)
                .Then(context => throw ProductStateException.AlreadyEnabled(context.Saga.CorrelationId)),

            When(Deleted)
                .TransitionTo(Discontinued)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<DeleteProductResponse>)
                .PublishAsync(Message<ProductDiscontinued>),

            When(InventoryQuantityChanged)
                .Then(ctx => logger.LogInformation("Inventory quantity changed"))
                .Then(UpdateStockQuantity)
                .PublishAsync(Message<ProductAvailabilityChanged>),

            When(InventoryDiscontinued)
                .Then(ctx => logger.LogInformation("Inventory discontinued"))
                .Then(UpdateStockQuantity)
                .Then(context => context.Saga.IsStocked = false)
                .TransitionTo(Discontinued)
                .PublishAsync(Message<ProductAvailabilityChanged>),

            When(InventoryDeleted)
                .Finalize()
                .PublishAsync(Message<ProductAvailabilityChanged>),

            When(InventoryHeld)
                .Then(UpdateStockQuantity)
                .Then(context => context.Saga.IsStocked = false)
                .PublishAsync(Message<ProductAvailabilityChanged>),

            When(InventoryReleased)
                .Then(UpdateStockQuantity)
                .PublishAsync(Message<ProductAvailabilityChanged>)
        );

        During(Disabled,
            When(NameUpdated)
                .Then(context => context.Saga.Name = context.Message.Name)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<UpdateProductResponse>)
                .PublishAsync(Message<ProductNameUpdated>),

            When(DimensionsUpdated)
                .Then(context => UpdateDimensions(context.Saga, context.Message.Dimensions))
                .Then(UpdateTimestamp)
                .RespondAsync(Message<UpdateProductResponse>)
                .PublishAsync(Message<ProductDimensionsUpdated>),

            When(StockThresholdUpdated)
                .Then(context => context.Saga.StockThreshold = context.Message.StockThreshold)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<UpdateProductResponse>)
                .PublishAsync(Message<ProductStockThresholdUpdated>),

            When(LeadTimeUpdated)
                .Then(context => context.Saga.LeadTime = context.Message.LeadTime)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<UpdateProductResponse>)
                .PublishAsync(Message<ProductLeadTimeUpdated>),

            When(EnableRequested)
                .TransitionTo(Enabled)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<EnableProductResponse>)
                .PublishAsync(Message<ProductEnabled>),

            When(DisableRequested)
                .Then(context => throw ProductStateException.AlreadyDisabled(context.Saga.CorrelationId)),

            When(Deleted)
                .TransitionTo(Discontinued)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<DeleteProductResponse>)
                .PublishAsync(Message<ProductDiscontinued>),

            When(InventoryQuantityChanged)
                .Then(UpdateStockQuantity)
                .PublishAsync(Message<ProductAvailabilityChanged>),

            When(InventoryDiscontinued)
                .Then(UpdateStockQuantity)
                .TransitionTo(Discontinued)
                .PublishAsync(Message<ProductAvailabilityChanged>),

            When(InventoryDeleted)
                .Finalize()
                .PublishAsync(Message<ProductAvailabilityChanged>),

            When(InventoryHeld)
                .Then(UpdateStockQuantity)
                .PublishAsync(Message<ProductAvailabilityChanged>),

            When(InventoryReleased)
                .Then(UpdateStockQuantity)
                .PublishAsync(Message<ProductAvailabilityChanged>)
        );

        During(Discontinued,
            
            When(Deleted)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<DeleteProductResponse>)
                .PublishAsync(Message<ProductDeleted>)
                .Finalize(),

            Ignore(InventoryDiscontinued),
                

            // Reject all other operations on discontinued products
            When(NameUpdated)
                .Then(context => throw ProductStateException.CannotModifyDiscontinuedProduct(context.Saga.CorrelationId, "UpdateName")),

            When(DimensionsUpdated)
                .Then(context => throw ProductStateException.CannotModifyDiscontinuedProduct(context.Saga.CorrelationId, "UpdateDimensions")),

            When(StockThresholdUpdated)
                .Then(context => throw ProductStateException.CannotModifyDiscontinuedProduct(context.Saga.CorrelationId, "UpdateStockThreshold")),

            When(LeadTimeUpdated)
                .Then(context => throw ProductStateException.CannotModifyDiscontinuedProduct(context.Saga.CorrelationId, "UpdateLeadTime")),

            When(EnableRequested)
                .Then(context => throw ProductStateException.CannotModifyDiscontinuedProduct(context.Saga.CorrelationId, "Enable")),

            When(DisableRequested)
                .Then(context => throw ProductStateException.CannotModifyDiscontinuedProduct(context.Saga.CorrelationId, "Disable"))
        );

        DuringAny(
            When(StatusRequested)
                .RespondAsync(Message<ProductStatusResponse>)
        );

        SetCompletedWhenFinalized();
    }

    public Event<CreateProductRequest> Created { get; }
    public Event<UpdateProductNameRequest> NameUpdated { get; }
    public Event<UpdateProductDimensionsRequest> DimensionsUpdated { get; }
    public Event<UpdateProductStockThresholdRequest> StockThresholdUpdated { get; }
    public Event<UpdateProductLeadTimeRequest> LeadTimeUpdated { get; }
    public Event<EnableProductRequest> EnableRequested { get; }
    public Event<DisableProductRequest> DisableRequested { get; }
    public Event<DeleteProductRequest> Deleted { get; }
    public Event<ProductStatusRequest> StatusRequested { get; }

    // Inventory-related events
    public Event<InventoryQuantityChanged> InventoryQuantityChanged { get; }
    public Event<InventoryDiscontinued> InventoryDiscontinued { get; }
    public Event<InventoryDeleted> InventoryDeleted { get; }
    public Event<InventoryHeld> InventoryHeld { get; }
    public Event<InventoryReleased> InventoryReleased { get; }
    
    public Request<ProductEntity, InventoryStatusRequest, InventoryStatusResponse> InventoryStatus { get; }

    public State Enabled { get; }
    public State Disabled { get; }
    public State Discontinued { get; }

    private static void SetProperties(BehaviorContext<ProductEntity, CreateProductRequest> context)
    {
        context.Saga.Sku = context.Message.Sku;
        context.Saga.Name = context.Message.Name;
        context.Saga.InventoryId = context.Message.InventoryId;
        context.Saga.StockThreshold = context.Message.StockThreshold;
        context.Saga.LeadTime = context.Message.LeadTime;
        
        UpdateDimensions(context.Saga, context.Message.Dimensions);

        context.Saga.CreatedOn = context.Message.Timestamp;
    }

    private static void UpdateDimensions(ProductEntity saga, ProductDimensions? dimensions)
    {
        if (dimensions.HasValue)
        {
            saga.Width = dimensions.Value.Width;
            saga.Height = dimensions.Value.Height;
            saga.Depth = dimensions.Value.Length;
            saga.Weight = dimensions.Value.Weight;
        }
        else
        {
            saga.Width = null;
            saga.Height = null;
            saga.Depth = null;
            saga.Weight = null;
        }
    }

    private static void UpdateTimestamp(BehaviorContext<ProductEntity, ProductCommand> context)
    {
        context.Saga.UpdatedOn = context.Message.Timestamp;
    }

    private static void UpdateStockQuantity(BehaviorContext<ProductEntity, InventoryModel> context)
    {
        context.Saga.StockQuantity = context.Message.StockQuantity;
        context.Saga.IsStocked = IsStocked();
        return;

        bool IsStocked()
        {
            if (context.Saga.InventoryId == null)
                return true;

            if (context.Message.Status != Abstractions.Contracts.InventoryStatus.Available)
                return false;

            return (context.Saga.StockQuantity ?? 0) >= (context.Saga.StockThreshold ?? context.Saga.Quantity);
        }
    }
    
    private static Task<SendTuple<TMessage>> Message<TMessage>(BehaviorContext<ProductEntity> context)
        where TMessage : class, ProductModel
        => context.Init<TMessage>(new
        {
            ProductId = context.Saga.CorrelationId,
            context.Saga.Sku,
            context.Saga.Name,
            Dimensions = GetProductDimensions(context.Saga),
            context.Saga.InventoryId,
            context.Saga.StockQuantity,
            context.Saga.StockThreshold,
            context.Saga.LeadTime,
            context.Saga.IsStocked,
            context.Saga.IsEnabled,
            context.Saga.IsAvailable,
            context.Saga.CreatedOn,
            context.Saga.UpdatedOn
        });

    private static void ConfigureEvent<TMessage>(IEventCorrelationConfigurator<ProductEntity, TMessage> conf)
        where TMessage : class, ProductCommand
    {
        conf.CorrelateById(s => s.Message.ProductId);
        conf.OnMissingInstance(b => b.ExecuteAsync(c => throw new ProductNotFoundException(c.Message.ProductId)));
    }

    private static ProductDimensions? GetProductDimensions(ProductEntity entity)
    {
        if (entity.Width == null && entity.Height == null && entity.Depth == null && entity.Weight == null)
            return null;

        return new ProductDimensions
        {
            Width = entity.Width,
            Height = entity.Height,
            Length = entity.Depth,
            Weight = entity.Weight
        };
    }
}