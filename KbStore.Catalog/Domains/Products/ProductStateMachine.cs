// ReSharper disable UnassignedGetOnlyAutoProperty
#pragma warning disable CS8618, CS9264

namespace KbStore.Catalog.Domains.Products;

using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Abstractions.Exceptions;
using MassTransit;

public sealed class ProductStateMachine : MassTransitStateMachine<ProductEntity>
{
    public ProductStateMachine()
    {
        InstanceState(m => m.CurrentState,
            Enabled,
            Disabled,
            Discontinued
        );

        Event(() => Created, e => e.CorrelateBy((s, c) => s.Sku == c.Message.Sku).SelectId(_ => NewId.NextSequentialGuid()));
        Event(() => StatusRequested, e => e.CorrelateById(c => c.Message.ProductId).OnMissingInstance(b => b.Execute(c => throw new ProductNotFoundException(c.Message.ProductId))));

        Event(() => NameUpdated, ConfigureEvent);
        Event(() => DimensionsUpdated, ConfigureEvent);
        Event(() => StockThresholdUpdated, ConfigureEvent);
        Event(() => LeadTimeUpdated, ConfigureEvent);
        Event(() => EnableRequested, ConfigureEvent);
        Event(() => DisableRequested, ConfigureEvent);
        Event(() => Deleted, ConfigureEvent);
        Event(() => AvailabilityChanged, ConfigureEvent);

        Initially(
            When(Created, context => string.IsNullOrWhiteSpace(context.Message.Sku))
                .Then(context => throw ProductValidationException.InvalidSku(context.Message.Sku)),

            When(Created)
                .Then(SetProperties)
                .TransitionTo(Enabled)
                .Then(UpdateTimestamp)
                .RespondAsync(Response<CreateProductResponse>)
                .PublishAsync(Message<ProductCreated>)
        );

        During(Enabled,
            // Handle duplicate creation attempts
            When(Created)
                .Then(context => throw ProductConflictException.DuplicateSku(context.Message.Sku)),

            When(NameUpdated)
                .Then(context => context.Saga.Name = context.Message.Name)
                .Then(UpdateTimestamp)
                .RespondAsync(Response<UpdateProductResponse>)
                .PublishAsync(Message<ProductNameUpdated>),

            When(DimensionsUpdated)
                .Then(context => UpdateDimensions(context.Saga, context.Message.Dimensions))
                .Then(UpdateTimestamp)
                .RespondAsync(Response<UpdateProductResponse>)
                .PublishAsync(Message<ProductDimensionsUpdated>),

            When(StockThresholdUpdated)
                .Then(context => context.Saga.StockThreshold = context.Message.StockThreshold)
                .Then(UpdateTimestamp)
                .RespondAsync(Response<UpdateProductResponse>)
                .PublishAsync(Message<ProductStockThresholdUpdated>),

            When(LeadTimeUpdated)
                .Then(context => context.Saga.LeadTime = context.Message.LeadTime)
                .Then(UpdateTimestamp)
                .RespondAsync(Response<UpdateProductResponse>)
                .PublishAsync(Message<ProductLeadTimeUpdated>),

            When(DisableRequested)
                .TransitionTo(Disabled)
                .Then(UpdateTimestamp)
                .RespondAsync(Response<DisableProductResponse>)
                .PublishAsync(Message<ProductDisabled>),

            When(EnableRequested)
                .Then(context => throw ProductStateException.AlreadyEnabled(context.Saga.CorrelationId)),

            When(Deleted)
                .TransitionTo(Discontinued)
                .Then(UpdateTimestamp)
                .RespondAsync(Response<DeleteProductResponse>)
                .PublishAsync(Message<ProductDiscontinued>),

            When(AvailabilityChanged)
                .Then(context => context.Saga.IsStocked = context.Message.IsStocked)
                .Then(UpdateTimestamp)
                .PublishAsync(Message<ProductAvailabilityChanged>),

            When(StatusRequested)
                .RespondAsync(Response<ProductStatusResponse>)
        );

        During(Disabled,
            When(NameUpdated)
                .Then(context => context.Saga.Name = context.Message.Name)
                .Then(UpdateTimestamp)
                .RespondAsync(Response<UpdateProductResponse>)
                .PublishAsync(Message<ProductNameUpdated>),

            When(DimensionsUpdated)
                .Then(context => UpdateDimensions(context.Saga, context.Message.Dimensions))
                .Then(UpdateTimestamp)
                .RespondAsync(Response<UpdateProductResponse>)
                .PublishAsync(Message<ProductDimensionsUpdated>),

            When(StockThresholdUpdated)
                .Then(context => context.Saga.StockThreshold = context.Message.StockThreshold)
                .Then(UpdateTimestamp)
                .RespondAsync(Response<UpdateProductResponse>)
                .PublishAsync(Message<ProductStockThresholdUpdated>),

            When(LeadTimeUpdated)
                .Then(context => context.Saga.LeadTime = context.Message.LeadTime)
                .Then(UpdateTimestamp)
                .RespondAsync(Response<UpdateProductResponse>)
                .PublishAsync(Message<ProductLeadTimeUpdated>),

            When(EnableRequested)
                .TransitionTo(Enabled)
                .Then(UpdateTimestamp)
                .RespondAsync(Response<EnableProductResponse>)
                .PublishAsync(Message<ProductEnabled>),

            When(DisableRequested)
                .Then(context => throw ProductStateException.AlreadyDisabled(context.Saga.CorrelationId)),

            When(Deleted)
                .TransitionTo(Discontinued)
                .Then(UpdateTimestamp)
                .RespondAsync(Response<DeleteProductResponse>)
                .PublishAsync(Message<ProductDiscontinued>),

            When(AvailabilityChanged)
                .Then(context => context.Saga.IsStocked = context.Message.IsStocked)
                .Then(UpdateTimestamp)
                .PublishAsync(Message<ProductAvailabilityChanged>),

            When(StatusRequested)
                .RespondAsync(Response<ProductStatusResponse>)
        );

        During(Discontinued,
            When(StatusRequested)
                .RespondAsync(Response<ProductStatusResponse>),

            When(Deleted)
                .Then(UpdateTimestamp)
                .RespondAsync(Response<DeleteProductResponse>)
                .PublishAsync(Message<ProductDeleted>)
                .Finalize(),

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
                .Then(context => throw ProductStateException.CannotModifyDiscontinuedProduct(context.Saga.CorrelationId, "Disable")),

            When(AvailabilityChanged)
                .Then(context => throw ProductStateException.CannotModifyDiscontinuedProduct(context.Saga.CorrelationId, "AvailabilityChanged"))
        );
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
    public Event<ProductAvailabilityChangedInternal> AvailabilityChanged { get; }

    public State Enabled { get; }
    public State Disabled { get; }
    public State Discontinued { get; }

    private static void SetProperties(BehaviorContext<ProductEntity, CreateProductRequest> context)
    {
        context.Saga.Sku = context.Message.Sku;
        context.Saga.Name = context.Message.Name;
        context.Saga.InventoryId = context.Message.InventoryItemId;
        context.Saga.StockThreshold = context.Message.StockThreshold;
        context.Saga.LeadTime = context.Message.LeadTime;
        context.Saga.IsStocked = true; // Default for new products

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

    private static Task<SendTuple<TMessage>> Message<TMessage>(BehaviorContext<ProductEntity> context)
        where TMessage : class, BaseProductEvent
        => context.Init<TMessage>(new
        {
            ProductId = context.Saga.CorrelationId,
            context.Saga.Sku,
            context.Saga.Name,
            Dimensions = GetProductDimensions(context.Saga),
            InventoryItemId = context.Saga.InventoryId,
            context.Saga.StockThreshold,
            LeadTime = context.Saga.LeadTime,
            context.Saga.IsStocked,
            IsEnabled = CalculateIsEnabled(context.Saga),
            IsAvailable = context.Saga.IsStocked && CalculateIsEnabled(context.Saga),
            context.Saga.CreatedOn,
            context.Saga.UpdatedOn
        });

    private static Task<SendTuple<TMessage>> Response<TMessage>(BehaviorContext<ProductEntity> context)
        where TMessage : class, ProductModel
        => context.Init<TMessage>(new
        {
            ProductId = context.Saga.CorrelationId,
            context.Saga.Sku,
            context.Saga.Name,
            Dimensions = GetProductDimensions(context.Saga),
            InventoryItemId = context.Saga.InventoryId,
            context.Saga.StockThreshold,
            LeadTime = context.Saga.LeadTime,
            context.Saga.IsStocked,
            IsEnabled = CalculateIsEnabled(context.Saga),
            IsAvailable = context.Saga.IsStocked && CalculateIsEnabled(context.Saga),
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

    private static bool CalculateIsEnabled(ProductEntity entity)
    {
        return entity.CurrentState switch
        {
            3 => true,  // Enabled
            4 => false, // Disabled
            6 => false, // Discontinued
            _ => false
        };
    }
}

// Internal event for system-generated availability changes
public interface ProductAvailabilityChangedInternal : ProductCommand
{
    bool IsStocked { get; }
}