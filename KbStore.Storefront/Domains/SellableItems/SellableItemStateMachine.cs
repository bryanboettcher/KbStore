// ReSharper disable UnassignedGetOnlyAutoProperty
#pragma warning disable CS8618, CS9264

namespace KbStore.Storefront.Domains.SellableItems;

using KbStore.Abstractions;
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
            e.CorrelateBy((saga, context) => saga.Sku == context.Message.Sku);
            e.SelectId(context => DeterministicGuid.FromSellableItemSku(context.Message.Sku));
            e.InsertOnInitial = true;
        });
        Event(() => NameUpdated);
        Event(() => PriceUpdated);
        Event(() => PublishRequested);
        Event(() => HideRequested);
        Event(() => DiscontinueRequested);
        Event(() => ReinstateRequested);
        Event(() => DeleteRequested);

        Initially(
            When(Created)
                .Then(ValidateCreationRequest)
                .Then(SetProperties)
                .TransitionTo(Draft)
                .RespondAsync(Message<CreateSellableItemResponse>)
                .PublishAsync(Message<SellableItemCreated>)
        );

        During(Draft,
            When(Created)
                .Then(context => throw SellableItemConflictException.DuplicateSku(context.Message.Sku)),

            When(PublishRequested)
                .TransitionTo(Published)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<PublishSellableItemResponse>)
                .PublishAsync(Message<SellableItemPublished>),

            When(DeleteRequested)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<DeleteSellableItemResponse>)
                .PublishAsync(Message<SellableItemDeleted>)
                .Finalize(),

            When(NameUpdated)
                .Then(ValidateName)
                .Then(context => context.Saga.Name = context.Message.Name)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<UpdateSellableItemResponse>),

            When(PriceUpdated)
                .Then(ValidatePrice)
                .ThenAsync(async context =>
                {
                    context.Saga.BasePrice = context.Message.BasePrice;
                    await context.Publish<SellableItemPriceChanged>(CreateModelPayload(context.Saga));
                })
                .Then(UpdateTimestamp)
                .RespondAsync(Message<UpdateSellableItemResponse>)
        );

        During(Published,
            When(Created)
                .Then(context => throw SellableItemConflictException.DuplicateSku(context.Message.Sku)),

            When(HideRequested)
                .TransitionTo(Hidden)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<HideSellableItemResponse>)
                .PublishAsync(Message<SellableItemHidden>),

            When(DiscontinueRequested)
                .TransitionTo(Discontinued)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<DiscontinueSellableItemResponse>)
                .PublishAsync(Message<SellableItemDiscontinued>),

            When(NameUpdated)
                .Then(ValidateName)
                .Then(context => context.Saga.Name = context.Message.Name)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<UpdateSellableItemResponse>),

            When(PriceUpdated)
                .Then(ValidatePrice)
                .ThenAsync(async context =>
                {
                    context.Saga.BasePrice = context.Message.BasePrice;
                    await context.Publish<SellableItemPriceChanged>(CreateModelPayload(context.Saga));
                })
                .Then(UpdateTimestamp)
                .RespondAsync(Message<UpdateSellableItemResponse>)
        );

        During(Hidden,
            When(Created)
                .Then(context => throw SellableItemConflictException.DuplicateSku(context.Message.Sku)),

            When(PublishRequested)
                .TransitionTo(Published)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<PublishSellableItemResponse>)
                .PublishAsync(Message<SellableItemPublished>),

            When(DiscontinueRequested)
                .TransitionTo(Discontinued)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<DiscontinueSellableItemResponse>)
                .PublishAsync(Message<SellableItemDiscontinued>),

            When(NameUpdated)
                .Then(ValidateName)
                .Then(context => context.Saga.Name = context.Message.Name)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<UpdateSellableItemResponse>),

            When(PriceUpdated)
                .Then(ValidatePrice)
                .ThenAsync(async context =>
                {
                    context.Saga.BasePrice = context.Message.BasePrice;
                    await context.Publish<SellableItemPriceChanged>(CreateModelPayload(context.Saga));
                })
                .Then(UpdateTimestamp)
                .RespondAsync(Message<UpdateSellableItemResponse>)
        );

        During(Discontinued,
            When(Created)
                .Then(context => throw SellableItemConflictException.DuplicateSku(context.Message.Sku)),

            When(ReinstateRequested)
                .TransitionTo(Draft)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<ReinstateSellableItemResponse>),

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

        if (string.IsNullOrWhiteSpace(msg.Sku))
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
        context.Saga.Sku = context.Message.Sku;
        context.Saga.Name = context.Message.Name;
        context.Saga.Description = context.Message.Description;
        context.Saga.BasePrice = context.Message.BasePrice;
        context.Saga.ItemType = context.Message.ItemType;
        context.Saga.Payload = context.Message.Payload.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        context.Saga.IsAvailable = true;
        context.Saga.CreatedOn = DateTimeOffset.UtcNow;
        context.Saga.UpdatedOn = context.Saga.CreatedOn;
    }

    private static void UpdateTimestamp(BehaviorContext<SellableItemEntity> context)
    {
        context.Saga.UpdatedOn = DateTimeOffset.UtcNow;
    }

    private static object CreateModelPayload(SellableItemEntity saga)
    {
        return new
        {
            SellableItemId = saga.CorrelationId,
            saga.ProductId,
            saga.Sku,
            saga.Name,
            saga.Description,
            saga.BasePrice,
            saga.ItemType,
            Payload = (IReadOnlyDictionary<string, object?>)saga.Payload,
            saga.IsAvailable,
            saga.Version,
            saga.CreatedOn,
            saga.UpdatedOn
        };
    }

    private static Task<SendTuple<TMessage>> Message<TMessage>(BehaviorContext<SellableItemEntity> context)
        where TMessage : class, SellableItemModel
        => context.Init<TMessage>(CreateModelPayload(context.Saga));
}
