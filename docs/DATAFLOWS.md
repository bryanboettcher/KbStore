# Data Flows and Lifecycles

This document describes how data flows through the KbStore system, including entity lifecycles, request flows, and event choreography.

## Table of Contents

1. [Product Lifecycle](#product-lifecycle)
2. [Inventory Lifecycle](#inventory-lifecycle)
3. [Product-Inventory Relationship](#product-inventory-relationship)
4. [HTTP Request Flow](#http-request-flow)
5. [State Machine Request/Response Flow](#state-machine-requestresponse-flow)
6. [Event Publishing Flow](#event-publishing-flow)
7. [Cross-Domain Orchestration (Planned)](#cross-domain-orchestration-planned)

---

## Product Lifecycle

### State Diagram

```
         [Initial State]
                |
                | CreateProductRequest
                v
        +---------------+
        | InventoryStatus|  (if InventoryId provided)
        |   .Pending    |
        +---------------+
         /            \
   Completed       Faulted/Timeout
        |              |
        v              v
   [Enabled]      [Disabled]
        |              |
        |              | EnableProductRequest
        |              +----------+
        |                         |
        | DisableProductRequest   |
        +<------------------------+
        |
        | DeleteProductRequest
        v
   [Discontinued]
        |
        | DeleteProductRequest (again)
        v
    [Finalized]
   (Removed from DB)
```

### State Transitions

#### Creation Flow

**Happy Path (No Inventory Link)**:
```
1. HTTP POST /products
2. ApiService validates payload
3. ApiService calls IProductCommandService.CreateAsync()
4. Service sends CreateProductRequest to state machine
5. State machine:
   - Creates entity with Initial state
   - Sets properties from request
   - Transitions to Enabled state
   - Publishes ProductCreated event
   - Responds with CreateProductResponse
6. Service returns ProductModel
7. ApiService returns 200 OK with product data
```

**With Inventory Link**:
```
1-4. Same as above
5. State machine:
   - Creates entity with Initial state
   - Sets properties from request
   - Transitions to InventoryStatus.Pending state
   - Sends InventoryStatusRequest to Inventory state machine
   - Waits for response (timeout: 5 seconds)
6a. If inventory responds successfully:
   - State machine receives InventoryStatusResponse
   - Updates StockQuantity from response
   - Transitions to Enabled state
   - Publishes ProductCreated event
   - Responds with CreateProductResponse
6b. If inventory faulted or times out:
   - State machine receives fault/timeout
   - Sets StockQuantity to 0
   - Transitions to Disabled state
   - Publishes ProductCreated event
   - Responds with CreateProductResponse
7-8. Same as above
```

#### Update Flow

**Update Name**:
```
1. HTTP PATCH /products/{id}/name
2. ApiService calls IProductCommandService.UpdateNameAsync()
3. Service sends UpdateProductNameRequest
4. State machine (if in Enabled or Disabled):
   - Updates Name property
   - Updates UpdatedOn timestamp
   - Publishes ProductNameUpdated event
   - Responds with UpdateProductResponse
5. Service returns ProductModel
6. ApiService returns 200 OK
```

**Update from Discontinued State**:
```
1-3. Same as above
4. State machine (in Discontinued):
   - Throws ProductStateException.CannotModifyDiscontinuedProduct
5. Service catches RequestFaultException
6. Service converts to ProductStateException via extension method
7. ApiService exception middleware converts to 409 Conflict
```

#### State Change Flow

**Enable Product**:
```
Enabled -> Throws ProductStateException.AlreadyEnabled
Disabled -> Transitions to Enabled, publishes ProductEnabled
Discontinued -> Throws ProductStateException.CannotModifyDiscontinuedProduct
```

**Disable Product**:
```
Enabled -> Transitions to Disabled, publishes ProductDisabled
Disabled -> Throws ProductStateException.AlreadyDisabled
Discontinued -> Throws ProductStateException.CannotModifyDiscontinuedProduct
```

#### Deletion Flow

**First Delete (Discontinue)**:
```
1. HTTP DELETE /products/{id}
2. ApiService calls IProductCommandService.DeleteAsync()
3. Service sends DeleteProductRequest
4. State machine (if in Enabled or Disabled):
   - Transitions to Discontinued state
   - Updates UpdatedOn timestamp
   - Publishes ProductDiscontinued event
   - Responds with DeleteProductResponse
5. Service returns ProductModel
6. ApiService returns 200 OK
```

**Second Delete (Finalize)**:
```
1-3. Same as above
4. State machine (in Discontinued):
   - Publishes ProductDeleted event
   - Calls Finalize() (marks for removal)
   - Responds with DeleteProductResponse
5-6. Same as above
7. MassTransit removes saga instance from repository
```

### Events Published

| State Transition | Events Published |
|-----------------|------------------|
| Initial → Enabled | `ProductCreated` |
| Initial → Disabled | `ProductCreated` |
| Enabled → Disabled | `ProductDisabled` |
| Disabled → Enabled | `ProductEnabled` |
| Enabled/Disabled → Discontinued | `ProductDiscontinued` |
| Discontinued → Finalized | `ProductDeleted` |
| Any → (name updated) | `ProductNameUpdated` |
| Any → (dimensions updated) | `ProductDimensionsUpdated` |
| Any → (quantity updated) | `ProductQuantityUpdated` |
| Any → (threshold updated) | `ProductStockThresholdUpdated` |
| Any → (lead time updated) | `ProductLeadTimeUpdated` |
| Any → (stock changed) | `ProductAvailabilityChanged` |

### External Event Reactions

Products react to Inventory domain events:

| Event | From State | Behavior |
|-------|-----------|----------|
| `InventoryQuantityChanged` | Enabled/Disabled | Updates StockQuantity, recalculates IsStocked, publishes ProductAvailabilityChanged |
| `InventoryDiscontinued` | Enabled/Disabled | Updates StockQuantity, transitions to Discontinued, publishes ProductAvailabilityChanged |
| `InventoryDeleted` | Enabled/Disabled | Finalizes product, publishes ProductAvailabilityChanged |
| `InventoryHeld` | Enabled | Sets IsStocked=false, publishes ProductAvailabilityChanged |
| `InventoryReleased` | Enabled | Recalculates IsStocked, publishes ProductAvailabilityChanged |

---

## Inventory Lifecycle

### State Diagram

```
        [Initial State]
               |
               | CreateInventoryRequest
               v
          [Available]
          /    |    \
         /     |     \
    Hold |  Decrease  | Increase
         |     |     /
         v     |    /
      [OnHold] |   /
         |     |  /
     Release   | / Delete
         |     |/
         +-----+
               |
               | DeleteInventoryRequest
               v
         [Discontinued]
               |
               | DeleteInventoryRequest (again)
               v
           [Finalized]
        (Removed from DB)
```

### State Transitions

#### Creation Flow

```
1. HTTP POST /inventory
2. ApiService validates payload
3. ApiService calls IInventoryCommandService.CreateAsync()
4. Service sends CreateInventoryRequest
5. State machine:
   - Creates entity with Initial state
   - Sets properties from request
   - Transitions to Available state
   - Publishes InventoryCreated event
   - Responds with CreateInventoryResponse
6. Service returns InventoryModel
7. ApiService returns 200 OK
```

#### Quantity Management

**Increase Quantity**:
```
Available:
1. Receives IncreaseInventoryQuantityRequest
2. Adds amount to StockQuantity
3. Updates timestamp
4. Publishes InventoryQuantityIncreased event
5. Responds with UpdateInventoryResponse

OnHold: Throws InventoryStateException.CannotModifyHeldItem
Discontinued: Throws InventoryStateException.CannotModifyDiscontinuedItem
Backordered: Increases quantity AND transitions back to Available
```

**Decrease Quantity**:
```
Available (sufficient stock):
1. Validates amount <= StockQuantity
2. Subtracts amount from StockQuantity
3. Updates timestamp
4. Publishes InventoryQuantityDecreased event
5. Responds with UpdateInventoryResponse

Available (insufficient stock):
1. Validates amount > StockQuantity
2. Throws InventoryValidationException.InsufficientStock

OnHold: Throws InventoryStateException.CannotModifyHeldItem
Discontinued: Throws InventoryStateException.CannotModifyDiscontinuedItem
Backordered: Throws InventoryStateException.CannotModifyBackorderedItem
```

#### Hold/Release Flow

**Hold Inventory**:
```
Available:
1. Receives HoldInventoryRequest
2. Transitions to OnHold state
3. Updates timestamp
4. Publishes InventoryHeld event
5. Responds with HoldInventoryResponse

OnHold: Throws InventoryStateException.AlreadyHeld
Discontinued: Throws InventoryStateException.CannotModifyDiscontinuedItem
Backordered: Throws InventoryStateException.CannotModifyBackorderedItem
```

**Release Inventory**:
```
OnHold:
1. Receives ReleaseInventoryRequest
2. Transitions back to Available state
3. Updates timestamp
4. Publishes InventoryReleased event
5. Responds with ReleaseInventoryResponse

Available/Discontinued/Backordered: Throws InventoryStateException.NotHeld
```

#### Deletion Flow

**First Delete (Discontinue)**:
```
Available/OnHold:
1. Receives DeleteInventoryRequest
2. Transitions to Discontinued state
3. Updates timestamp
4. Publishes InventoryDiscontinued event
5. Responds with DeleteInventoryResponse
```

**Second Delete (Finalize)**:
```
Discontinued:
1. Receives DeleteInventoryRequest
2. Updates timestamp
3. Publishes InventoryDeleted event
4. Finalizes (marks for removal)
5. Responds with DeleteInventoryResponse
6. MassTransit removes from repository
```

**Resurrect from Discontinued**:
```
Discontinued:
1. Receives CreateInventoryRequest with same PartNumber
2. Transitions to Available state
3. Publishes InventoryCreated event
4. Responds with CreateInventoryResponse
```

### Events Published

| State Transition | Events Published |
|-----------------|------------------|
| Initial → Available | `InventoryCreated` |
| Available (quantity+) | `InventoryQuantityIncreased` |
| Available (quantity-) | `InventoryQuantityDecreased` |
| Available → OnHold | `InventoryHeld` |
| OnHold → Available | `InventoryReleased` |
| Available/OnHold → Discontinued | `InventoryDiscontinued` |
| Discontinued → Finalized | `InventoryDeleted` |
| Any (description) | `InventoryDescriptionUpdated` |

---

## Product-Inventory Relationship

### Correlation Strategy

Products and Inventory are correlated by:
- **Product.InventoryId** (foreign key to Inventory.CorrelationId)
- **Optional**: A Product may exist without Inventory (digital goods, services)
- **Exclusive**: An Inventory item may support multiple Products (future enhancement)

### Event Flow: Inventory → Product

```
┌─────────────┐
│  Inventory  │
│    State    │
│   Machine   │
└──────┬──────┘
       │
       │ InventoryQuantityChanged event published
       │
       v
   [RabbitMQ]
       │
       │ Event routed to all subscribers
       │
       v
┌──────────────┐
│   Product    │
│    State     │  (Subscribed via Event<InventoryQuantityChanged>)
│   Machine    │
└──────┬───────┘
       │
       │ Correlation: saga.InventoryId == event.InventoryId
       │
       v
  Update Product.StockQuantity
  Recalculate Product.IsStocked
  Publish ProductAvailabilityChanged
```

### Example Flow: Inventory Quantity Increase

```
1. HTTP PATCH /inventory/{id}/increase
   Body: { "amount": 50 }

2. ApiService → InventoryCommandService.IncreaseQuantityAsync()

3. Service → InventoryStateMachine
   Message: IncreaseInventoryQuantityRequest

4. InventoryStateMachine:
   - StockQuantity: 100 → 150
   - Publishes InventoryQuantityIncreased event:
     {
       InventoryId: guid-123,
       StockQuantity: 150,
       Status: Available
     }

5. Event published to RabbitMQ

6. ProductStateMachine receives event:
   - Correlation: Product.InventoryId == guid-123
   - Updates Product.StockQuantity: 100 → 150
   - Recalculates IsStocked:
     StockQuantity (150) >= StockThreshold (50)? YES
   - Publishes ProductAvailabilityChanged event:
     {
       ProductId: guid-456,
       IsAvailable: true,
       StockQuantity: 150
     }

7. (Future) ApiService consumer receives ProductAvailabilityChanged
   - Calls StorefrontService.UpdateAvailability()
   - Updates SellableItem read model
```

### IsStocked Calculation Logic

```csharp
bool IsStocked()
{
    // Products without inventory are always "stocked"
    if (InventoryId == null)
        return true;

    // Inventory must be Available status
    if (InventoryStatus != Available)
        return false;

    // Stock quantity must meet threshold
    var threshold = StockThreshold ?? Quantity;
    return StockQuantity >= threshold;
}
```

---

## HTTP Request Flow

### Typical Request Path

```
┌──────────┐
│  Client  │
└────┬─────┘
     │ HTTP POST /products
     │ Body: { sku: "WIDGET", name: "Widget", ... }
     v
┌─────────────────┐
│   ApiService    │ (KbStore.ApiService)
│  HTTP Endpoint  │
└────┬────────────┘
     │ 1. Validate payload
     │ 2. Call service layer
     v
┌───────────────────┐
│  Service Layer    │ (KbStore.Catalog.Services)
│ CommandService    │
└────┬──────────────┘
     │ 1. Validate business rules
     │ 2. Create request client
     │ 3. Send command message
     v
┌──────────────┐
│  MassTransit │
│   Request    │
│   Pipeline   │
└────┬─────────┘
     │ Route to state machine
     v
┌───────────────────┐
│  State Machine    │ (KbStore.Catalog)
│  Domain Logic     │
└────┬──────────────┘
     │ 1. Validate state
     │ 2. Update entity
     │ 3. Transition state
     │ 4. Publish events
     │ 5. Send response
     v
┌──────────────┐
│  PostgreSQL  │
│  Repository  │
└──────────────┘
     │ Entity persisted
     │
     v
   [Response flows back up the chain]
     │
     v
┌──────────────┐
│   Client     │
│  200 OK      │
│  { product } │
└──────────────┘
```

### Request Timing

```
Total Request Time: ~50-200ms (typical)

├─ HTTP Processing: 5-10ms
│  └─ Endpoint validation and delegation
│
├─ Service Layer: 10-20ms
│  ├─ Business validation: 1-2ms
│  └─ Request client setup: 8-18ms
│
├─ MassTransit Pipeline: 20-50ms
│  ├─ Message serialization: 5-10ms
│  ├─ Transport (in-memory/RabbitMQ): 5-20ms
│  └─ Message deserialization: 5-10ms
│
├─ State Machine: 10-100ms
│  ├─ State validation: 1-2ms
│  ├─ Business logic execution: 2-10ms
│  ├─ Event publishing: 5-20ms
│  └─ Database persistence: 10-70ms
│
└─ Response Pipeline: 10-20ms
   └─ Return path through MassTransit
```

### Error Flow

```
┌───────────────────┐
│  State Machine    │
│  Domain Logic     │
└────┬──────────────┘
     │ Throws ProductNotFoundException
     v
┌──────────────┐
│  MassTransit │
│    Fault     │
│   Handler    │
└────┬─────────┘
     │ Wraps exception in Fault message
     │ Returns RequestFaultException
     v
┌───────────────────┐
│  Service Layer    │
│ CommandService    │
└────┬──────────────┘
     │ Catches RequestFaultException
     │ Converts via extension method:
     │   .ToProductException()
     v
┌─────────────────┐
│   ApiService    │
│    Exception    │
│   Middleware    │
└────┬────────────┘
     │ Converts exception to ProblemDetails
     │ Maps to HTTP status code
     v
┌──────────────┐
│   Client     │
│  404 Not     │
│   Found      │
└──────────────┘
```

---

## State Machine Request/Response Flow

### Command Pattern (Fire and Forget - NOT USED)

We DON'T use fire-and-forget commands because we need responses.

### Request/Response Pattern (USED)

All operations use request/response for immediate feedback:

```
┌─────────────┐                          ┌─────────────┐
│   Service   │                          │    State    │
│    Layer    │                          │   Machine   │
└──────┬──────┘                          └──────┬──────┘
       │                                        │
       │ 1. Create request client               │
       │    for CreateProductRequest            │
       │                                        │
       │ 2. Send CreateProductRequest           │
       │────────────────────────────────────────>│
       │                                        │
       │                                        │ 3. Validate state
       │                                        │    Execute transitions
       │                                        │    Persist changes
       │                                        │    Publish events
       │                                        │
       │ 4. Response: CreateProductResponse     │
       │<────────────────────────────────────────│
       │    { ProductId, Sku, Name, ... }       │
       │                                        │
       │ 5. Return ProductModel                 │
       │                                        │
```

### Saga-to-Saga Request Pattern

Products can request data from Inventory state machines:

```
┌─────────────┐                          ┌─────────────┐
│   Product   │                          │  Inventory  │
│    State    │                          │    State    │
│   Machine   │                          │   Machine   │
└──────┬──────┘                          └──────┬──────┘
       │                                        │
       │ 1. Product created with InventoryId    │
       │                                        │
       │ 2. Request(InventoryStatus)            │
       │    Sends InventoryStatusRequest        │
       │────────────────────────────────────────>│
       │                                        │
       │ 3. Transitions to                      │
       │    InventoryStatus.Pending state       │
       │    (Waiting for response)              │
       │                                        │ 4. Receives request
       │                                        │    Responds with status
       │                                        │
       │ 5. Response: InventoryStatusResponse   │
       │<────────────────────────────────────────│
       │    { StockQuantity, Status }           │
       │                                        │
       │ 6. Transitions to Enabled              │
       │    Updates StockQuantity               │
       │    Publishes ProductCreated            │
       │                                        │
```

**Timeout Handling**:
```
Product State Machine (timeout: 5 seconds)

If InventoryStatusResponse not received within 5s:
  └─> Receives InventoryStatus.TimeoutExpired
      ├─ Sets StockQuantity = 0
      ├─ Transitions to Disabled state
      └─ Publishes ProductCreated
```

**Fault Handling**:
```
If Inventory state machine throws exception:
  └─> Product receives InventoryStatus.Faulted
      ├─ Sets StockQuantity = 0
      ├─ Transitions to Disabled state
      └─ Publishes ProductCreated
```

---

## Event Publishing Flow

### Publish Pipeline

```
┌───────────────────┐
│  State Machine    │
│  (After state     │
│   transition)     │
└────┬──────────────┘
     │ .PublishAsync(Message<ProductCreated>)
     │
     v
┌──────────────────┐
│  Message Factory │
│  (context.Init)  │
└────┬─────────────┘
     │ Creates message from saga properties
     │ { ProductId, Sku, Name, ... }
     v
┌──────────────┐
│  MassTransit │
│   Publish    │
│   Pipeline   │
└────┬─────────┘
     │ 1. Serialize message
     │ 2. Add metadata (timestamp, correlationId)
     │ 3. Determine exchange name
     v
┌──────────────┐
│   RabbitMQ   │
│    Exchange  │
│   (topic)    │
└────┬─────────┘
     │ Message published to exchange:
     │ "KbStore.Catalog.Abstractions:ProductCreated"
     │
     │ Fanout to all bindings
     │
     v
┌────────────────────────────────────────┐
│  Queues (one per consumer)             │
├────────────────────────────────────────┤
│  • KbStore.ApiService_ProductCreated   │  (Future)
│  • KbStore.Analytics_ProductCreated    │  (Future)
│  • (No consumers currently exist)      │
└────────────────────────────────────────┘
```

### Current State: Events Without Consumers

```
Product State Machine publishes:
  • ProductCreated
  • ProductEnabled
  • ProductDisabled
  • ProductDiscontinued
  • ProductDeleted
  • ProductAvailabilityChanged
  • ProductNameUpdated
  • ProductDimensionsUpdated
  • ProductQuantityUpdated
  • ProductStockThresholdUpdated
  • ProductLeadTimeUpdated

Inventory State Machine publishes:
  • InventoryCreated
  • InventoryQuantityIncreased
  • InventoryQuantityDecreased
  • InventoryHeld
  • InventoryReleased
  • InventoryDiscontinued
  • InventoryDeleted
  • InventoryDescriptionUpdated

Current subscribers:
  • Product State Machine subscribes to Inventory events (IMPLEMENTED)
  • ApiService consumers (PLACEHOLDER ONLY - empty classes)
  • Storefront consumers (NOT CREATED - domain doesn't exist)

Events are published but mostly not consumed:
  ✓ Inventory → Product: WORKING
  ✗ Product → ApiService: NOT IMPLEMENTED
  ✗ ApiService → Storefront: NOT IMPLEMENTED
```

### Event Retention

Events are published to RabbitMQ but:
- No persistent queues configured yet
- Events not consumed are lost
- Future consumers won't see historical events
- Replay mechanism not implemented

**Future enhancement**: Add event sourcing or durable queues for event replay.

---

## Cross-Domain Orchestration (Planned)

### Planned Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Catalog Domain                       │
├─────────────────────────────────────────────────────────┤
│  Product/Inventory State Machines                       │
│  Publish: ProductAvailabilityChanged                    │
└────────────────────┬────────────────────────────────────┘
                     │
                     │ Event published to RabbitMQ
                     v
            ┌─────────────────┐
            │    RabbitMQ     │
            │    Exchange     │
            └────────┬────────┘
                     │
                     │ Route to consumer queue
                     v
┌─────────────────────────────────────────────────────────┐
│                   ApiService                            │
├─────────────────────────────────────────────────────────┤
│  ProductAvailabilityConsumer                            │
│  (MassTransit IConsumer<ProductAvailabilityChanged>)    │
│                                                         │
│  Orchestration Logic:                                   │
│  • Determine if product should be visible               │
│  • Calculate pricing tier adjustments                   │
│  • Apply business rules spanning domains                │
│  • Call Storefront service                              │
└────────────────────┬────────────────────────────────────┘
                     │
                     │ Call service layer
                     v
┌─────────────────────────────────────────────────────────┐
│                 Storefront Domain                       │
├─────────────────────────────────────────────────────────┤
│  IStorefrontService.UpdateAvailability()                │
│  • Sends command to SellableItem state machine          │
│  • Updates denormalized read model                      │
│  • Publishes SellableItemUpdated event                  │
└─────────────────────────────────────────────────────────┘
```

### Planned Event Flows

#### Flow 1: Inventory Quantity Changed

```
1. Inventory quantity increased via HTTP PATCH

2. InventoryStateMachine:
   └─ Publishes InventoryQuantityIncreased

3. ProductStateMachine (subscriber):
   ├─ Receives InventoryQuantityIncreased
   ├─ Updates Product.StockQuantity
   ├─ Recalculates Product.IsStocked
   └─ Publishes ProductAvailabilityChanged

4. ApiService.ProductAvailabilityConsumer (future):
   ├─ Receives ProductAvailabilityChanged
   ├─ Determines visibility rules
   ├─ Calculates pricing impacts
   └─ Calls StorefrontService.UpdateAvailability()

5. StorefrontStateMachine (future):
   ├─ Updates SellableItem.IsAvailable
   ├─ Updates SellableItem.StockQuantity
   └─ Publishes SellableItemUpdated

6. Frontend (future):
   └─ SignalR notification: product availability changed
```

#### Flow 2: Product Discontinued

```
1. Product discontinued via HTTP DELETE

2. ProductStateMachine:
   └─ Publishes ProductDiscontinued

3. ApiService.ProductDiscontinuedConsumer (future):
   ├─ Receives ProductDiscontinued
   ├─ Calls StorefrontService.RemoveFromCatalog()
   └─ Calls CartService.RemoveDiscontinuedItems()

4. StorefrontStateMachine (future):
   └─ Sets SellableItem.IsVisible = false

5. CartStateMachine (future):
   └─ Removes item from all active carts
```

#### Flow 3: Product Created

```
1. Product created via HTTP POST

2. ProductStateMachine:
   └─ Publishes ProductCreated

3. ApiService.ProductCreatedConsumer (future):
   ├─ Receives ProductCreated
   ├─ Applies default pricing rules
   ├─ Determines initial visibility
   └─ Calls StorefrontService.CreateSellableItem()

4. StorefrontStateMachine (future):
   ├─ Creates SellableItem read model
   ├─ Sets customer-facing pricing
   └─ Publishes SellableItemCreated

5. Search Index Service (future):
   └─ Adds product to search index
```

### Orchestration Patterns

**Pattern 1: Simple Delegation**
```csharp
// ApiService consumer simply delegates to other domain
public class ProductAvailabilityConsumer : IConsumer<ProductAvailabilityChanged>
{
    private readonly IStorefrontService _storefront;

    public async Task Consume(ConsumeContext<ProductAvailabilityChanged> context)
    {
        await _storefront.UpdateAvailabilityAsync(
            context.Message.ProductId,
            context.Message.IsAvailable,
            context.CancellationToken);
    }
}
```

**Pattern 2: Orchestration with Business Rules**
```csharp
// ApiService consumer applies business logic spanning domains
public class ProductAvailabilityConsumer : IConsumer<ProductAvailabilityChanged>
{
    private readonly IStorefrontService _storefront;
    private readonly IPricingService _pricing;

    public async Task Consume(ConsumeContext<ProductAvailabilityChanged> context)
    {
        // Business rule: Low stock triggers price increase
        var priceMultiplier = context.Message.StockQuantity < 10 ? 1.1m : 1.0m;

        // Business rule: Out of stock products are hidden
        var isVisible = context.Message.IsAvailable && context.Message.StockQuantity > 0;

        // Coordinate across domains
        await _storefront.UpdateAvailabilityAsync(
            productId: context.Message.ProductId,
            isAvailable: isVisible,
            priceMultiplier: priceMultiplier,
            cancellationToken: context.CancellationToken);
    }
}
```

**Pattern 3: Saga Orchestration**
```csharp
// ApiService saga coordinates multi-step workflow
public class OrderFulfillmentSaga : MassTransitStateMachine<OrderFulfillmentState>
{
    public OrderFulfillmentSaga()
    {
        Initially(
            When(OrderPlaced)
                .PublishAsync(context => context.Init<ReserveInventoryCommand>(new {
                    context.Message.OrderId,
                    context.Message.Items
                }))
                .TransitionTo(AwaitingInventoryReservation)
        );

        During(AwaitingInventoryReservation,
            When(InventoryReserved)
                .PublishAsync(context => context.Init<ProcessPaymentCommand>(new {
                    context.Saga.OrderId,
                    context.Message.Amount
                }))
                .TransitionTo(AwaitingPayment)
        );

        // ... continue orchestration
    }
}
```

### Why ApiService for Orchestration?

**Separation of Concerns**:
- **Catalog domain**: "What physically exists"
- **Storefront domain**: "What customers can buy"
- **ApiService**: "How the two coordinate"

**Prevents Domain Coupling**:
- Catalog never knows about Storefront
- Storefront never knows about Catalog
- Both can evolve independently
- ApiService handles translation and coordination

**Single Responsibility**:
- Domains: Pure business logic for their bounded context
- ApiService: HTTP gateway + cross-domain workflows
- Clear ownership of concerns

---

## Summary

### What's Implemented

- ✅ Product lifecycle with state transitions
- ✅ Inventory lifecycle with state transitions
- ✅ Product-Inventory relationship via events
- ✅ HTTP request flow through API → Service → State Machine
- ✅ State machine request/response patterns
- ✅ Event publishing from Catalog domain
- ✅ Intra-domain event subscription (Inventory → Product)

### What's Planned

- ⏳ Cross-domain event consumers in ApiService
- ⏳ Storefront domain implementation
- ⏳ ApiService orchestration logic
- ⏳ SellableItem read model synchronization
- ⏳ Cart and Order workflows
- ⏳ Event sourcing for replay capability
- ⏳ Saga-based order fulfillment

### Current Limitations

- Events are published but mostly not consumed (except Product subscribes to Inventory)
- No event persistence or replay mechanism
- No cross-domain workflows active yet
- Storefront domain is infrastructure only (no logic)

For implementation details, see `docs/PATTERNS.md`.
For navigation and file locations, see `docs/NAVIGATION.md`.
