# KbStore.ApiService - Integration & Event Choreography

## Overview

This document describes how **KbStore.ApiService** orchestrates cross-domain workflows by consuming events from bounded contexts and coordinating actions across Catalog and Storefront domains.

**Key Principle**: Domains are self-contained and do not directly communicate. ApiService contains event consumers that tie domains together using their service layers.

## Architecture Pattern

```
┌─────────────────┐         ┌─────────────────┐
│     Catalog     │         │   Storefront    │
│                 │         │                 │
│  State Machines │         │  State Machines │
│        ↓        │         │        ↓        │
│   PostgreSQL    │         │     MongoDB     │
│        ↓        │         │        ↓        │
│  Publishes      │         │  Publishes      │
│  Events         │         │  Events         │
└────────┬────────┘         └────────┬────────┘
         │                           │
         └───────────┬───────────────┘
                     ↓
              ┌──────────────┐
              │   RabbitMQ   │
              │  Event Bus   │
              └──────┬───────┘
                     ↓
         ┌───────────────────────────┐
         │  KbStore.ApiService       │
         │                           │
         │  Event Consumers          │
         │  (Orchestration Logic)    │
         │           ↓               │
         │  ┌─────────────────────┐ │
         │  │ Catalog.Services    │ │
         │  │ Storefront.Services │ │
         │  └─────────────────────┘ │
         └───────────────────────────┘
```

## Correlation Strategy

**All domains use the same Guid (CorrelationId) to identify entities across boundaries.**

### Product Correlation

- Catalog: `Product(Id=Guid-123, SKU="WIDGET-RED", ...)`
- Storefront: `SellableItem(Id=Guid-123, SKU="WIDGET-RED", Price=$9.99, ...)`

When Storefront places an order, it uses `Guid-123` to inform Catalog which product's inventory to hold.

### Workflow Example

```
1. Catalog.ProductCreated (ProductId=Guid-123, SKU="WIDGET-RED")
   ↓
2. ApiService.ProductCreatedConsumer receives event
   ↓
3. Consumer calls StorefrontService.CreateSellableItem(productId: Guid-123, sku, name, initialPrice)
   ↓
4. Storefront creates SellableItem with same Guid-123
```

Later:
```
5. Customer adds item to cart, places order
   ↓
6. Storefront.OrderPlaced (OrderId=..., Items=[{ProductId: Guid-123, Quantity: 2}])
   ↓
7. ApiService.OrderPlacedConsumer receives event
   ↓
8. Consumer calls CatalogService.HoldInventory(productId: Guid-123, quantity: 2)
   ↓
9. Catalog holds inventory using Guid-123 correlation
```

## Event Choreography Scenarios

### Scenario 1: Catalog Stock Change → Storefront Availability Update

**Trigger**: Inventory quantity changes in Catalog

**Events Flow**:
```
Catalog Domain:
  InventoryStateMachine.DecreaseQuantity()
    → Publishes: InventoryQuantityChanged
         (ProductId=Guid, InventoryId=Guid, StockQuantity=5)

ApiService:
  InventoryQuantityChangedConsumer.Consume(event)
    → Check: Should update Storefront? (business rules)
    → Call: StorefrontService.UpdateAvailability(
               productId: event.Message.ProductId,
               isAvailable: event.Message.StockQuantity > 0
           )

Storefront Domain:
  SellableItemStateMachine.UpdateAvailability()
    → Updates MongoDB document
    → Publishes: SellableItemAvailabilityChanged (if needed)
```

**Business Rules** (in ApiService consumer):
- If stock drops to zero → mark unavailable
- If stock quantity >= product.StockThreshold → mark available
- May implement additional logic (e.g., "featured items" get priority notification)

### Scenario 2: Product Created in Catalog → Create SellableItem in Storefront

**Trigger**: New product added to Catalog

**Events Flow**:
```
Catalog Domain:
  ProductStateMachine.Create()
    → Publishes: ProductCreated
         (ProductId=Guid, SKU="WIDGET", Name="Red Widget", Quantity=1, InventoryId=...)

ApiService:
  ProductCreatedConsumer.Consume(event)
    → Determine initial price (may query pricing service or use default)
    → Call: StorefrontService.CreateSellableItem(
               productId: event.Message.ProductId,
               sku: event.Message.SKU,
               name: event.Message.Name,
               price: DeterminePrice(event.Message),
               isAvailable: event.Message.IsAvailable
           )

Storefront Domain:
  SellableItemStateMachine.Create()
    → Creates MongoDB document with pricing
    → Publishes: SellableItemCreated
```

**Pricing Logic** (in ApiService consumer):
- May apply markup to cost (Catalog's `Product.Price` is cost)
- May query external pricing service
- May use database-driven rules (future)

### Scenario 3: Order Placed in Storefront → Hold Inventory in Catalog

**Trigger**: Customer completes checkout

**Events Flow**:
```
Storefront Domain:
  OrderStateMachine.PlaceOrder()
    → Publishes: OrderPlaced
         (OrderId=Guid, CustomerId=Guid, Items=[{ProductId: Guid, Quantity: 2}])

ApiService:
  OrderPlacedConsumer.Consume(event)
    → For each item in order:
         Call: CatalogService.HoldInventory(
                  productId: item.ProductId,  // Guid correlation
                  quantity: item.Quantity
              )
    → If hold fails (insufficient stock):
         Call: StorefrontService.CancelOrder(orderId, reason)

Catalog Domain:
  InventoryStateMachine.Hold()
    → Publishes: InventoryHeld
    → Product reacts to InventoryHeld → IsStocked = false
    → Publishes: ProductAvailabilityChanged

ApiService:
  ProductAvailabilityChangedConsumer.Consume(event)
    → Call: StorefrontService.UpdateAvailability(...)

Storefront Domain:
  Updates SellableItem availability
```

**Orchestration Logic** (in ApiService consumer):
- Transaction coordination: If any item fails to hold, rollback all holds
- Saga pattern may be needed for complex order workflows

### Scenario 4: Order Cancelled → Release Inventory in Catalog

**Trigger**: Customer or system cancels order

**Events Flow**:
```
Storefront Domain:
  OrderStateMachine.Cancel()
    → Publishes: OrderCancelled
         (OrderId=Guid, Items=[{ProductId: Guid, Quantity: 2}])

ApiService:
  OrderCancelledConsumer.Consume(event)
    → For each item in order:
         Call: CatalogService.ReleaseInventory(
                  productId: item.ProductId,
                  quantity: item.Quantity
              )

Catalog Domain:
  InventoryStateMachine.Release()
    → Publishes: InventoryReleased
    → Product reacts → IsStocked = true (if quantity sufficient)
    → Publishes: ProductAvailabilityChanged

ApiService:
  ProductAvailabilityChangedConsumer.Consume(event)
    → Call: StorefrontService.UpdateAvailability(...)
```

## Consumer Implementation Pattern

### Basic Consumer Structure

```csharp
public class InventoryQuantityChangedConsumer : IConsumer<InventoryQuantityChanged>
{
    private readonly ILogger<InventoryQuantityChangedConsumer> _logger;
    private readonly IStorefrontService _storefrontService;
    private readonly IProductQueryService _productQueryService;  // Optional

    public InventoryQuantityChangedConsumer(
        ILogger<InventoryQuantityChangedConsumer> logger,
        IStorefrontService storefrontService,
        IProductQueryService productQueryService)
    {
        _logger = logger;
        _storefrontService = storefrontService;
        _productQueryService = productQueryService;
    }

    public async Task Consume(ConsumeContext<InventoryQuantityChanged> context)
    {
        var evt = context.Message;

        _logger.LogInformation(
            "Inventory quantity changed for Product {ProductId}: Quantity={Quantity}",
            evt.ProductId, evt.StockQuantity);

        // Optional: Query additional product details if needed
        var product = await _productQueryService.GetByIdAsync(evt.ProductId);

        // Orchestration logic
        if (ShouldUpdateStorefront(evt, product))
        {
            await _storefrontService.UpdateAvailability(
                productId: evt.ProductId,
                isAvailable: evt.StockQuantity > 0,
                cancellationToken: context.CancellationToken
            );
        }
    }

    private bool ShouldUpdateStorefront(InventoryQuantityChanged evt, Product product)
    {
        // Business rules here
        // May read from database-driven configuration
        return true;  // For now, always update
    }
}
```

### Database-Driven Orchestration Rules (Future)

ApiService may have its own database for orchestration configuration:

```csharp
// OrchestrationRules table
public class OrchestrationRule
{
    public Guid Id { get; set; }
    public string EventType { get; set; }  // "InventoryQuantityChanged"
    public string Condition { get; set; }  // JSON or expression
    public string Action { get; set; }     // "UpdateStorefront"
    public bool IsEnabled { get; set; }
}

// In consumer:
var rules = await _ruleService.GetRulesForEvent("InventoryQuantityChanged");
foreach (var rule in rules.Where(r => r.IsEnabled))
{
    if (EvaluateCondition(rule.Condition, context.Message))
    {
        await ExecuteAction(rule.Action, context.Message);
    }
}
```

## Authentication & Authorization

ApiService handles auth concerns and delegates user identity as a single Guid:

```csharp
public class SecureEndpoint
{
    public static async Task<IResult> PlaceOrder(
        [FromBody] PlaceOrderPayload payload,
        [FromServices] IStorefrontService storefrontService,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken)
    {
        // Extract user identity from JWT/Auth system
        var userId = httpContextAccessor.HttpContext?.User
            .FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Results.Unauthorized();
        }

        // Pass only the Guid to domain services
        var order = await storefrontService.PlaceOrderAsync(
            customerId: userGuid,
            items: payload.Items,
            cancellationToken: cancellationToken
        );

        return Results.Ok(order);
    }
}
```

**Domains are unaware of authentication mechanisms** - they only receive a `Guid` representing the user.

## Error Handling & Compensation

### When Orchestration Fails

If a consumer call to a domain service fails:

1. **Log the error** with structured logging
2. **Publish compensation event** (if applicable)
3. **Retry with exponential backoff** (MassTransit handles this)
4. **Move to error queue** after max retries

```csharp
public async Task Consume(ConsumeContext<OrderPlaced> context)
{
    try
    {
        await _catalogService.HoldInventory(
            productId: item.ProductId,
            quantity: item.Quantity
        );
    }
    catch (InsufficientStockException ex)
    {
        _logger.LogWarning(ex, "Insufficient stock for Order {OrderId}", context.Message.OrderId);

        // Publish compensation event
        await context.Publish<CancelOrder>(new
        {
            OrderId = context.Message.OrderId,
            Reason = "Insufficient stock",
            Timestamp = DateTimeOffset.UtcNow
        });

        // Don't throw - we handled it by cancelling the order
    }
}
```

## Testing Orchestration Consumers

Consumers are tested using MassTransit Test Harness:

```csharp
[TestFixture]
public class InventoryQuantityChangedConsumer_Tests
{
    private ITestHarness _harness;
    private IConsumerTestHarness<InventoryQuantityChangedConsumer> _consumerHarness;
    private Mock<IStorefrontService> _mockStorefrontService;

    [SetUp]
    public async Task Setup()
    {
        _mockStorefrontService = new Mock<IStorefrontService>();

        _harness = new InMemoryTestHarness();
        _consumerHarness = _harness.Consumer(() =>
            new InventoryQuantityChangedConsumer(
                Mock.Of<ILogger<InventoryQuantityChangedConsumer>>(),
                _mockStorefrontService.Object
            )
        );

        await _harness.Start();
    }

    [Test]
    public async Task When_inventory_quantity_changed_then_updates_storefront()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var evt = new InventoryQuantityChanged
        {
            ProductId = productId,
            StockQuantity = 10
        };

        // Act
        await _harness.InputQueueSendEndpoint.Send(evt);

        // Assert
        Assert.That(await _consumerHarness.Consumed.Any<InventoryQuantityChanged>());

        _mockStorefrontService.Verify(
            x => x.UpdateAvailability(productId, true, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [TearDown]
    public async Task Teardown()
    {
        await _harness.Stop();
    }
}
```

## Current State & Roadmap

### Implemented
- ✅ HTTP endpoints delegate to Catalog services
- ✅ MassTransit infrastructure configured
- ✅ `ProductAvailabilityConsumer` placeholder exists

### Partially Implemented
- ⚠️ Catalog domain publishes events (fully implemented)
- ⚠️ Storefront domain (infrastructure only, no state machines yet)

### Not Yet Implemented
- ❌ Event consumers with orchestration logic
- ❌ Storefront service layer
- ❌ SellableItem state machines
- ❌ Order/Cart workflows
- ❌ Database-driven orchestration rules
- ❌ Compensation/saga patterns

### Next Steps

1. **Implement Storefront Service Layer**: Create service wrappers for Storefront domain (when state machines exist)
2. **Create ProductCreatedConsumer**: React to Catalog product creation
3. **Create InventoryQuantityChangedConsumer**: React to stock level changes
4. **Implement SellableItem State Machine**: In Storefront domain
5. **Create OrderPlacedConsumer**: React to orders and hold inventory
6. **Add Compensation Logic**: Handle failures gracefully
7. **Document Additional Workflows**: As new bounded contexts are added

## Key Architectural Insights

1. **Domains Never Directly Communicate**: All cross-domain coordination happens in ApiService consumers
2. **Correlation via Guid**: Same entity ID across all domains
3. **Services as Building Blocks**: Consumers call service methods, never bypass to databases
4. **Orchestration vs. Choreography**: ApiService orchestrates; domains choreograph internally (Product reacts to Inventory events)
5. **Repeatable Pattern**: This orchestration layer pattern can be applied to future domains (Payment, Fulfillment, etc.)
