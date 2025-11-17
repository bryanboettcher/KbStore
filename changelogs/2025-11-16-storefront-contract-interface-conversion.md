# Storefront Contract Conversion to MassTransit-Idiomatic Interfaces

**Date**: 2025-11-16
**Commit(s)**: a82fcbb
**Author**: Bryan Boettcher
**Category**: Refactor

## Summary

Converted KbStore.Storefront contract definitions from C# record types to MassTransit-idiomatic interface hierarchies. This refactoring aligns the Storefront domain with established patterns used in the Catalog domain, improving consistency across the codebase and resolving runtime message initialization errors that occurred when MassTransit's `context.Init<T>()` required parameterless constructors that records with required properties could not provide.

## Changes

### Files Modified

- `KbStore.Storefront.Abstractions/Contracts/SellableItems.cs` - Complete contract redesign from records to interfaces
- `KbStore.Storefront/Domains/SellableItems/SellableItemStateMachine.cs` - Updated message initialization pattern
- `KbStore.Storefront/Domains/SellableItems/SellableItemEntity.cs` - Aligned timestamp property names
- `KbStore.Storefront.Services/SellableItemCommandService.cs` - Unchanged, works with interface contracts
- `KbStore.Storefront.Services/SellableItemQueryService.cs` - Unchanged, works with interface contracts
- `KbStore.Storefront.Abstractions/Interfaces/ISellableItemCommandService.cs` - Service contracts now reference interface types
- `KbStore.Storefront.Abstractions/Interfaces/ISellableItemQueryService.cs` - Service contracts now reference interface types
- 9 test files in `KbStore.Storefront.Tests/Domains/SellableItems/` - Updated to work with interface-based contract types

### Key Decisions

1. **Interface Hierarchy Pattern**: Established base interfaces (`SellableItemCommand`, `SellableItemModel`, `BaseSellableItemEvent`) that define common properties and correlation semantics, with specialized interfaces inheriting for specific operations.

2. **ExcludeFromTopology Annotation**: Applied `[ExcludeFromTopology]` attribute to base interfaces to prevent MassTransit from attempting to create separate queue/exchange definitions for inherited types, reducing infrastructure noise.

3. **Timestamp Property Standardization**: Renamed `CreatedAt`/`UpdatedAt` to `CreatedOn`/`UpdatedOn` for consistency with established .NET conventions and alignment with the Catalog domain.

4. **DateTime to DateTimeOffset**: Converted timestamp properties from `DateTime` to `DateTimeOffset` to properly capture timezone information and prevent ambiguity about UTC vs. local time.

5. **Payload Type Consistency**: Changed payload from `Dictionary<string, object?>` to `IReadOnlyDictionary<string, object?>` in interface contracts to enforce immutability semantics while maintaining flexibility in implementations.

6. **SKU Property Naming**: Renamed `SKU` to `Sku` to follow C# naming conventions (PascalCase for property names, not acronyms).

7. **Direct Instantiation Pattern**: Updated state machine to use direct object instantiation with `Task.FromResult()` instead of `context.Init<T>()`, which avoids MassTransit's parameterless constructor requirement that causes runtime errors with interface-based contracts.

## Impact

### Before

Contracts were defined as C# records with init-only properties:

```csharp
public record CreateSellableItemRequest
{
    public Guid? ProductId { get; init; }
    public string SKU { get; init; } = "";
    public string Name { get; init; } = "";
    public decimal BasePrice { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record SellableItemResponse(
    Guid Id,
    Guid? ProductId,
    string SKU,
    string Name,
    decimal BasePrice,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
```

**Problems**:
- Records with required init properties have no parameterless constructor
- MassTransit's `context.Init<T>()` method requires parameterless constructors, causing "No default constructor available for message type" runtime errors
- Property naming inconsistencies (`SKU` vs. `Sku`, `CreatedAt` vs. `CreatedOn`)
- Different timestamp types (`DateTime`, `DateTime?`) across request/response/event contracts
- No semantic hierarchy - couldn't distinguish command vs. event contract intent
- Record type requires type declarations in many locations, reducing flexibility

### After

Contracts are now defined as interface hierarchies:

```csharp
[ExcludeFromTopology]
public interface SellableItemCommand : CorrelatedBy<Guid>
{
    Guid SellableItemId { get; }
    DateTimeOffset Timestamp { get; }
    Guid CorrelationId => SellableItemId;
}

[ExcludeFromTopology]
public interface SellableItemModel : CorrelatedBy<Guid>
{
    Guid SellableItemId { get; }
    Guid? ProductId { get; }
    string Sku { get; }
    string Name { get; }
    string? Description { get; }
    decimal BasePrice { get; }
    string ItemType { get; }
    IReadOnlyDictionary<string, object?> Payload { get; }
    bool IsAvailable { get; }
    int Version { get; }
    DateTimeOffset CreatedOn { get; }
    DateTimeOffset UpdatedOn { get; }
    Guid CorrelationId => SellableItemId;
}

[ExcludeFromTopology]
public interface BaseSellableItemEvent : SellableItemModel;

public interface CreateSellableItemRequest
{
    Guid? ProductId { get; }
    string Sku { get; }
    string Name { get; }
    string? Description { get; }
    decimal BasePrice { get; }
    string ItemType { get; }
    IReadOnlyDictionary<string, object?> Payload { get; }
    DateTimeOffset Timestamp { get; }
}

public interface CreateSellableItemResponse : SellableItemModel;
public interface SellableItemCreated : BaseSellableItemEvent;
```

**Benefits**:
- Interface contracts have no constructor constraints - MassTransit can serialize/deserialize freely
- Clear semantic hierarchy: Commands inherit common correlation semantics, Models define response shape, Events extend Model with temporal immutability
- Consistent property naming (Sku, CreatedOn, UpdatedOn) across all contracts
- Unified timestamp type (DateTimeOffset) eliminates ambiguity
- ExcludeFromTopology reduces unnecessary queue/exchange definitions
- State machine message initialization uses simple object construction with Task.FromResult()
- Interfaces enable flexible implementation strategies (records, classes, anonymous types)

## Technical Details

### Contract Organization

Contracts are now organized into semantic groups:

1. **Base Interfaces** (marked with `[ExcludeFromTopology]`):
   - `SellableItemCommand`: Base for all command requests (contains SellableItemId + Timestamp)
   - `SellableItemModel`: Base for all responses/events (complete entity state)
   - `BaseSellableItemEvent`: Marker interface for events (inherits SellableItemModel)

2. **Specialized Request Interfaces**:
   - `CreateSellableItemRequest`: Full item properties for creation
   - `UpdateSellableItemNameRequest`: Command + name property
   - `UpdateSellableItemPriceRequest`: Command + price property
   - `PublishSellableItemRequest`: Simple command
   - `HideSellableItemRequest`: Simple command
   - `DiscontinueSellableItemRequest`: Simple command
   - `ReinstateSellableItemRequest`: Simple command
   - `DeleteSellableItemRequest`: Simple command
   - `SellableItemStatusRequest`: Status query

3. **Response Interfaces**:
   - `CreateSellableItemResponse`: Extends SellableItemModel
   - `UpdateSellableItemResponse`: Extends SellableItemModel
   - `PublishSellableItemResponse`: Extends SellableItemModel
   - `HideSellableItemResponse`: Extends SellableItemModel
   - `DiscontinueSellableItemResponse`: Extends SellableItemModel
   - `ReinstateSellableItemResponse`: Extends SellableItemModel
   - `DeleteSellableItemResponse`: Extends SellableItemModel
   - `SellableItemStatusResponse`: Extends SellableItemModel

4. **Event Interfaces**:
   - `SellableItemCreated`: Extends BaseSellableItemEvent
   - `SellableItemUpdated`: Extends BaseSellableItemEvent (marker for updates)
   - `SellableItemNameUpdated`: Extends SellableItemUpdated
   - `SellableItemDescriptionUpdated`: Extends SellableItemUpdated
   - `SellableItemPayloadUpdated`: Extends SellableItemUpdated
   - `SellableItemAvailabilityChanged`: Extends SellableItemUpdated
   - `SellableItemPriceChanged`: Extends SellableItemAvailabilityChanged
   - `SellableItemPublished`: Extends SellableItemUpdated
   - `SellableItemHidden`: Extends SellableItemUpdated
   - `SellableItemDiscontinued`: Extends SellableItemUpdated
   - `SellableItemReinstated`: Extends SellableItemUpdated
   - `SellableItemDeleted`: Extends BaseSellableItemEvent

### Message Initialization Pattern

**Old Pattern** (causes runtime errors):
```csharp
.PublishAsync(Message<SellableItemPublished>)

private static Task<SendTuple<TMessage>> Message<TMessage>(BehaviorContext<SellableItemEntity> context)
    where TMessage : class, SellableItemModel
    => context.Init<TMessage>(CreateModelPayload(context.Saga));
```

**New Pattern** (direct instantiation):
```csharp
.ThenAsync(async context =>
{
    context.Saga.BasePrice = context.Message.BasePrice;
    await context.Publish<SellableItemPriceChanged>(CreateModelPayload(context.Saga));
})

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
```

The new pattern uses anonymous object types that implicitly implement the interface contracts, avoiding MassTransit's constructor constraints while maintaining full semantic typing.

### Entity Alignment

`SellableItemEntity` properties were updated for consistency:

```csharp
// Before
public DateTime CreatedAt { get; set; }
public DateTime? UpdatedAt { get; set; }

// After
public DateTimeOffset CreatedOn { get; set; }
public DateTimeOffset UpdatedOn { get; set; }
```

This ensures the entity's timestamp representation matches interface contracts exactly.

### Test Updates

All 9 test classes in `KbStore.Storefront.Tests/Domains/SellableItems/` were updated to:
- Reference interface types instead of record types
- Use the new timestamp property names (CreatedOn/UpdatedOn)
- Use DateTimeOffset assertions
- Verify interface contract compliance

## Validation

- All Storefront domain tests pass with interface-based contracts
- State machine message initialization executes without "No default constructor" errors
- Anonymous object types correctly implement interface contracts at runtime
- Services continue to work unchanged (contract changes are additive to interface hierarchy)
- Responses properly serialize with new timestamp format (DateTimeOffset)

## Related

- **Catalog Domain Reference**: This pattern mirrors the interface-based contract design used in Catalog domain (`KbStore.Catalog.Abstractions/Contracts/`)
- **MassTransit Documentation**: Context.Init<T>() requires parameterless constructors (https://masstransit.io/documentation/concepts/messages)
- **Cross-Domain Correlation**: Interface hierarchy enables consistent use of CorrelationId across Catalog → Storefront orchestration
- **ADR-001**: See `docs/adr/001-sellable-item-architecture.md` for broader Storefront architecture decisions

## Migration Notes

If consuming Storefront domain contracts from external services:

1. **Request types**: No breaking changes - consumers continue using same method signatures
2. **Response types**: Now implements `SellableItemModel` interface instead of record type
3. **Event types**: Now implements `BaseSellableItemEvent` interface instead of record type
4. **Timestamp handling**: Code reading CreatedOn/UpdatedOn now receives DateTimeOffset (was DateTime/DateTime?)
5. **Payload property**: Now typed as `IReadOnlyDictionary<string, object?>` (previously `Dictionary<string, object?>`)
6. **SKU property**: Renamed from `SKU` to `Sku` in all interfaces
