// ReSharper disable UnassignedGetOnlyAutoProperty
#pragma warning disable CS8618, CS9264

namespace KbStore.Storefront.Domains.SellableItems;

using KbStore.Storefront.Abstractions.Contracts;
using KbStore.Storefront.Abstractions.Exceptions;
using MassTransit;

public sealed class SellableItemStateMachine : MassTransitStateMachine<SellableItemEntity>
{
    public SellableItemStateMachine()
    {
        InstanceState(m => m.CurrentState, Draft, Published, Hidden, Discontinued);

        Event(() => Created, e =>
        {
            e.CorrelateBy((saga, context) => saga.SKU == context.Message.SKU);
            e.SelectId(_ => NewId.NextSequentialGuid());
            e.InsertOnInitial = true;
            e.SetSagaFactory(context => new SellableItemEntity
            {
                CorrelationId = NewId.NextSequentialGuid(),
                SKU = context.Message.SKU
            });
        });
        Event(() => NameUpdated, ConfigureEvent);
        Event(() => PriceUpdated, ConfigureEvent);
        Event(() => PublishRequested, ConfigureEvent);
        Event(() => HideRequested, ConfigureEvent);
        Event(() => DiscontinueRequested, ConfigureEvent);
        Event(() => ReinstateRequested, ConfigureEvent);
        Event(() => DeleteRequested, ConfigureEvent);

        Initially(
            When(Created)
                .Then(ValidateCreationRequest)
                .Then(SetProperties)
                .TransitionTo(Draft)
                .RespondAsync(CreateResponse)
                .PublishAsync(CreateEvent)
        );

        During(Draft,
            When(Created)
                .Then(context => throw SellableItemConflictException.DuplicateSku(context.Message.SKU)),

            When(PublishRequested)
                .TransitionTo(Published)
                .Then(UpdateTimestamp)
                .RespondAsync(CreateResponse)
                .PublishAsync(context => context.Init<SellableItemPublished>(new
                {
                    SellableItemId = context.Saga.CorrelationId,
                    PublishedAt = DateTime.UtcNow
                })),

            When(DeleteRequested)
                .Then(UpdateTimestamp)
                .PublishAsync(context => context.Init<SellableItemDeleted>(new
                {
                    SellableItemId = context.Saga.CorrelationId,
                    DeletedAt = DateTime.UtcNow
                }))
                .Finalize(),

            When(NameUpdated)
                .Then(ValidateName)
                .Then(context => context.Saga.Name = context.Message.Name)
                .Then(UpdateTimestamp)
                .RespondAsync(CreateResponse),

            When(PriceUpdated)
                .Then(ValidatePrice)
                .ThenAsync(async context =>
                {
                    var oldPrice = context.Saga.BasePrice;
                    context.Saga.BasePrice = context.Message.BasePrice;
                    await context.Publish<SellableItemPriceChanged>(new
                    {
                        SellableItemId = context.Saga.CorrelationId,
                        OldPrice = oldPrice,
                        NewPrice = context.Saga.BasePrice,
                        ChangedAt = DateTime.UtcNow
                    });
                })
                .Then(UpdateTimestamp)
                .RespondAsync(CreateResponse)
        );

        During(Published,
            When(Created)
                .Then(context => throw SellableItemConflictException.DuplicateSku(context.Message.SKU)),

            When(HideRequested)
                .TransitionTo(Hidden)
                .Then(UpdateTimestamp)
                .RespondAsync(CreateResponse)
                .PublishAsync(context => context.Init<SellableItemHidden>(new
                {
                    SellableItemId = context.Saga.CorrelationId,
                    HiddenAt = DateTime.UtcNow
                })),

            When(DiscontinueRequested)
                .TransitionTo(Discontinued)
                .Then(UpdateTimestamp)
                .RespondAsync(CreateResponse)
                .PublishAsync(context => context.Init<SellableItemDiscontinued>(new
                {
                    SellableItemId = context.Saga.CorrelationId,
                    DiscontinuedAt = DateTime.UtcNow
                })),

            When(NameUpdated)
                .Then(ValidateName)
                .Then(context => context.Saga.Name = context.Message.Name)
                .Then(UpdateTimestamp)
                .RespondAsync(CreateResponse),

            When(PriceUpdated)
                .Then(ValidatePrice)
                .ThenAsync(async context =>
                {
                    var oldPrice = context.Saga.BasePrice;
                    context.Saga.BasePrice = context.Message.BasePrice;
                    await context.Publish<SellableItemPriceChanged>(new
                    {
                        SellableItemId = context.Saga.CorrelationId,
                        OldPrice = oldPrice,
                        NewPrice = context.Saga.BasePrice,
                        ChangedAt = DateTime.UtcNow
                    });
                })
                .Then(UpdateTimestamp)
                .RespondAsync(CreateResponse)
        );

        During(Hidden,
            When(Created)
                .Then(context => throw SellableItemConflictException.DuplicateSku(context.Message.SKU)),

            When(PublishRequested)
                .TransitionTo(Published)
                .Then(UpdateTimestamp)
                .RespondAsync(CreateResponse)
                .PublishAsync(context => context.Init<SellableItemPublished>(new
                {
                    SellableItemId = context.Saga.CorrelationId,
                    PublishedAt = DateTime.UtcNow
                })),

            When(DiscontinueRequested)
                .TransitionTo(Discontinued)
                .Then(UpdateTimestamp)
                .RespondAsync(CreateResponse)
                .PublishAsync(context => context.Init<SellableItemDiscontinued>(new
                {
                    SellableItemId = context.Saga.CorrelationId,
                    DiscontinuedAt = DateTime.UtcNow
                })),

            When(NameUpdated)
                .Then(ValidateName)
                .Then(context => context.Saga.Name = context.Message.Name)
                .Then(UpdateTimestamp)
                .RespondAsync(CreateResponse),

            When(PriceUpdated)
                .Then(ValidatePrice)
                .ThenAsync(async context =>
                {
                    var oldPrice = context.Saga.BasePrice;
                    context.Saga.BasePrice = context.Message.BasePrice;
                    await context.Publish<SellableItemPriceChanged>(new
                    {
                        SellableItemId = context.Saga.CorrelationId,
                        OldPrice = oldPrice,
                        NewPrice = context.Saga.BasePrice,
                        ChangedAt = DateTime.UtcNow
                    });
                })
                .Then(UpdateTimestamp)
                .RespondAsync(CreateResponse)
        );

        During(Discontinued,
            When(Created)
                .Then(context => throw SellableItemConflictException.DuplicateSku(context.Message.SKU)),

            When(ReinstateRequested)
                .TransitionTo(Draft)
                .Then(UpdateTimestamp)
                .RespondAsync(CreateResponse),

            When(NameUpdated)
                .Then(context => throw new SellableItemStateException("Discontinued", "update name")),

            When(PriceUpdated)
                .Then(context => throw new SellableItemStateException("Discontinued", "update price")),

            When(PublishRequested)
                .Then(context => throw new SellableItemStateException("Discontinued", "publish")),

            When(HideRequested)
                .Then(context => throw new SellableItemStateException("Discontinued", "hide")),

            When(DiscontinueRequested)
                .Then(context => throw new SellableItemStateException("Discontinued", "discontinue"))
        );

        SetCompletedWhenFinalized();
    }

    public Event<CreateSellableItemRequest> Created { get; }
    public Event<UpdateSellableItemNameRequest> NameUpdated { get; }
    public Event<UpdateSellableItemPriceRequest> PriceUpdated { get; }
    public Event<PublishSellableItemRequest> PublishRequested { get; }
    public Event<HideSellableItemRequest> HideRequested { get; }
    public Event<DiscontinueSellableItemRequest> DiscontinueRequested { get; }
    public Event<ReinstateSellableItemRequest> ReinstateRequested { get; }
    public Event<DeleteSellableItemRequest> DeleteRequested { get; }

    public State Draft { get; }
    public State Published { get; }
    public State Hidden { get; }
    public State Discontinued { get; }

    private static void ValidateCreationRequest(BehaviorContext<SellableItemEntity, CreateSellableItemRequest> context)
    {
        var msg = context.Message;

        if (string.IsNullOrWhiteSpace(msg.SKU))
            throw new SellableItemValidationException("SKU cannot be empty");

        if (string.IsNullOrWhiteSpace(msg.Name))
            throw new SellableItemValidationException("Name cannot be empty");

        if (msg.BasePrice < 0)
            throw new SellableItemValidationException($"BasePrice cannot be negative: {msg.BasePrice}");

        if (string.IsNullOrWhiteSpace(msg.ItemType))
            throw new SellableItemValidationException("ItemType cannot be empty");
    }

    private static void ValidateName(BehaviorContext<SellableItemEntity, UpdateSellableItemNameRequest> context)
    {
        if (string.IsNullOrWhiteSpace(context.Message.Name))
            throw new SellableItemValidationException("Name cannot be empty");
    }

    private static void ValidatePrice(BehaviorContext<SellableItemEntity, UpdateSellableItemPriceRequest> context)
    {
        if (context.Message.BasePrice < 0)
            throw new SellableItemValidationException($"BasePrice cannot be negative: {context.Message.BasePrice}");
    }

    private static void SetProperties(BehaviorContext<SellableItemEntity, CreateSellableItemRequest> context)
    {
        context.Saga.ProductId = context.Message.ProductId;
        context.Saga.SKU = context.Message.SKU;
        context.Saga.Name = context.Message.Name;
        context.Saga.Description = context.Message.Description;
        context.Saga.BasePrice = context.Message.BasePrice;
        context.Saga.ItemType = context.Message.ItemType;
        context.Saga.Payload = context.Message.Payload;
        context.Saga.IsAvailable = true;
        context.Saga.CreatedAt = DateTime.UtcNow;
        context.Saga.UpdatedAt = null;
    }

    private static void UpdateTimestamp(BehaviorContext<SellableItemEntity> context)
    {
        context.Saga.UpdatedAt = DateTime.UtcNow;
    }

    private static Task<SendTuple<SellableItemResponse>> CreateResponse(BehaviorContext<SellableItemEntity> context)
        => context.Init<SellableItemResponse>(new
        {
            Id = context.Saga.CorrelationId,
            context.Saga.ProductId,
            context.Saga.SKU,
            context.Saga.Name,
            context.Saga.Description,
            context.Saga.BasePrice,
            context.Saga.ItemType,
            context.Saga.Payload,
            State = GetStateName(context.Saga.CurrentState),
            context.Saga.IsAvailable,
            context.Saga.Version,
            context.Saga.CreatedAt,
            context.Saga.UpdatedAt
        });

    private static Task<SendTuple<SellableItemCreated>> CreateEvent(BehaviorContext<SellableItemEntity> context)
        => context.Init<SellableItemCreated>(new
        {
            SellableItemId = context.Saga.CorrelationId,
            context.Saga.SKU,
            context.Saga.Name,
            context.Saga.BasePrice,
            context.Saga.ItemType,
            context.Saga.CreatedAt
        });

    private static void ConfigureEvent<TMessage>(IEventCorrelationConfigurator<SellableItemEntity, TMessage> conf)
        where TMessage : class
    {
        conf.CorrelateById(GetCorrelationId);
        conf.OnMissingInstance(b => b.ExecuteAsync(c =>
        {
            var id = GetCorrelationId(c);
            throw new SellableItemNotFoundException(id);
        }));
    }

    private static Guid GetCorrelationId<TMessage>(ConsumeContext<TMessage> context) where TMessage : class
    {
        return context.Message switch
        {
            UpdateSellableItemNameRequest msg => msg.SellableItemId,
            UpdateSellableItemPriceRequest msg => msg.SellableItemId,
            PublishSellableItemRequest msg => msg.SellableItemId,
            HideSellableItemRequest msg => msg.SellableItemId,
            DiscontinueSellableItemRequest msg => msg.SellableItemId,
            ReinstateSellableItemRequest msg => msg.SellableItemId,
            DeleteSellableItemRequest msg => msg.SellableItemId,
            _ => throw new InvalidOperationException($"Cannot extract SellableItemId from message type {typeof(TMessage).Name}")
        };
    }

    private static string GetStateName(int state) => state switch
    {
        SellableItemStates.Draft => "Draft",
        SellableItemStates.Published => "Published",
        SellableItemStates.Hidden => "Hidden",
        SellableItemStates.Discontinued => "Discontinued",
        _ => "Unknown"
    };
}
