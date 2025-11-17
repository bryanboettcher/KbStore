# KbStore.Storefront - Architecture Documentation

## Purpose

The Storefront vertical slice is intended to be the **customer-facing transaction domain** within the KbStore system. This domain is currently in a **placeholder state** with foundational infrastructure but no implemented business logic.

### Intended Responsibilities (Future)

Based on the architectural pattern established in the Catalog domain and the Storefront naming, this service is positioned to own:

1. **Customer-Facing Pricing** - Sell prices, promotions, discounts, dynamic pricing
2. **Shopping Experience** - Customer browsing via denormalized product views, cart management, wishlist functionality
3. **Order Management** - Order placement, order lifecycle, order history
4. **Customer Context** - Customer sessions, preferences, shopping behavior
5. **SellableItem Read Models** - Denormalized copies of Catalog products optimized for customer queries, enriched with pricing

**Key Distinction**: While Catalog tracks "what exists physically" and "how much stock we have," Storefront manages "what customers can buy" and "at what price."

**Correlation Strategy**: Storefront and Catalog use the same **Guid (CorrelationId)** for products. When an order is placed for a SellableItem, Storefront uses this Guid to inform Catalog which inventory to hold via ApiService orchestration.

## Current State

### What EXISTS Today

1. **Infrastructure Foundation**:
   - MassTransit configured with RabbitMQ connection
   - MongoDB database allocation (`storefront` database in AppHost)
   - Service defaults integration (telemetry, health checks)
   - Aspire orchestration registration
   - Test project structure

2. **Project Structure**:
   - `KbStore.Storefront` - Domain service (message bus only, no persistence yet)
   - `KbStore.Storefront.Abstractions` - Empty placeholder
   - `KbStore.Storefront.Tests` - Empty test project

### What DOES NOT EXIST Today

1. State machines or domain entities
2. Contracts (requests, responses, events)
3. Service layer implementations
4. MongoDB DbContext or repository configuration
5. Business logic or domain rules
6. API endpoints
7. Event consumers (Catalog event subscriptions)
8. Domain exceptions

## Architectural Decisions (WHY)

### 1. WHY MongoDB Instead of PostgreSQL?

**Decision**: Storefront uses MongoDB while Catalog uses PostgreSQL.

**Rationale** (Inferred from Architecture):
- **Different Data Patterns**:
  - Catalog domain manages structured, relational data (Products, Inventory with foreign key relationships)
  - Storefront will likely manage document-oriented data (shopping carts, order history, customer sessions)
- **Schema Flexibility**: Shopping carts and orders often have variable structures (different product configurations, custom options, promotional bundles)
- **Read-Heavy Workloads**: Customer browsing and order history queries benefit from MongoDB's denormalized document model
- **Event Sourcing Alternative**: MongoDB's document model naturally stores event-enriched aggregates without complex joins
- **Polyglot Persistence**: Demonstrates architectural choice to use the right database for each bounded context

**Trade-offs**:
- No ACID transactions across Catalog and Storefront (already true in microservices architecture)
- Different ORM patterns (EF Core vs MongoDB driver)
- Team needs to understand two database paradigms

### 2. WHY Separate Domain from Catalog?

**Decision**: Storefront is a separate microservice communicating via events, not a module within Catalog.

**Rationale**:
- **Bounded Context Isolation**: Customer-facing concerns (orders, carts) are fundamentally different from inventory management
- **Independent Scaling**: Customer traffic (browsing, cart updates) has different scale characteristics than catalog updates
- **Deployment Independence**: Storefront UI/UX changes shouldn't require Catalog redeployment
- **Team Autonomy**: Different teams can own customer experience vs. inventory operations
- **Failure Isolation**: Catalog outages shouldn't prevent browsing already-cached product data; Storefront outages shouldn't affect warehouse operations

**Integration Pattern** (When Implemented via ApiService Orchestration):
- ApiService consumes `ProductCreated`, `ProductAvailabilityChanged`, `ProductDiscontinued` from Catalog
- ApiService calls Storefront services to create/update `SellableItem` read models
- Storefront publishes `OrderPlaced`, `OrderCancelled` events
- ApiService consumes Storefront events and calls Catalog services to trigger inventory holds/releases

**Important**: Storefront does NOT directly consume Catalog events. The ApiService orchestration layer handles cross-domain choreography.

### 3. WHY No Saga State Machines Yet?

**Decision**: Storefront infrastructure exists but no state machines are implemented.

**Possible Rationale**:
- **Catalog First**: Establish the state machine pattern in Catalog domain before replicating
- **Complex Workflows**: Order placement might use **Saga Orchestration** (different pattern than Catalog's state machines)
- **Event Sourcing Consideration**: Orders might use event sourcing instead of state machines
- **Awaiting Requirements**: Customer domain rules need to be fully specified before implementing

**When State Machines ARE Added** (Future), Expected Patterns:
- `CartStateMachine` - Active → Abandoned → Converted → Expired
- `OrderStateMachine` - Pending → PaymentAuthorized → Fulfilled → Cancelled → Completed
- Event-driven reactions to Catalog availability changes

## Domain Boundaries

### What This Service WILL OWN (Future)

1. **Customer-Facing Pricing**: Sell prices, promotional pricing, discounts, price calculations
2. **SellableItem Read Models**: Denormalized product views with pricing, optimized for customer queries
3. **Cart Lifecycle**: Create cart, add/remove items, apply promotions, abandon detection
4. **Order Lifecycle**: Place order, payment coordination, fulfillment tracking, cancellation
5. **Session Management**: Anonymous vs. authenticated customer sessions
6. **Purchase Validation**: Pricing verification at checkout, promotional rule application

**Pricing Note**: The `Product.Price` field in Catalog is for internal cost tracking. Storefront's `SellableItem.Price` is the customer-facing sell price that may include margins, promotions, and dynamic pricing logic.

### What This Service WILL PUBLISH (Future)

**Cart Events** (Expected):
- `CartCreated`, `ItemAddedToCart`, `ItemRemovedFromCart`, `CartAbandoned`, `CartConverted`

**Order Events** (Expected):
- `OrderPlaced`, `OrderPaymentAuthorized`, `OrderFulfilled`, `OrderCancelled`, `OrderCompleted`

**Customer Events** (Expected):
- `CustomerSessionStarted`, `ProductViewed`, `SearchPerformed`

### What This Service WILL CONSUME (Future)

**Via ApiService Orchestration** (Not Direct Consumption):
- Catalog events (`ProductCreated`, `ProductAvailabilityChanged`, `ProductDiscontinued`, `ProductDeleted`) flow through ApiService consumers
- ApiService calls Storefront services to update `SellableItem` read models based on Catalog events
- This keeps Storefront decoupled from Catalog implementation details

**From Payment Domain** (If Implemented):
- `PaymentAuthorized`, `PaymentCaptured`, `PaymentFailed`, `PaymentRefunded`

**From Fulfillment Domain** (If Implemented):
- `OrderShipped`, `OrderDelivered`, `OrderReturned`

**Important**: Storefront services are invoked by ApiService orchestration consumers, not by subscribing to events directly.

### What This Service WILL NOT OWN

1. **Physical Inventory Tracking**: Catalog domain owns actual stock quantities and availability calculations
2. **Product Master Data**: Catalog domain owns SKUs, part numbers, dimensions - Storefront maintains denormalized copies
3. **Payment Processing**: Separate Payment domain handles transactions
4. **Shipping/Fulfillment**: Separate Fulfillment domain handles logistics
5. **Customer Identity**: Separate Identity domain handles authentication/authorization (ApiService delegates user identity as Guid)
6. **Internal Cost Tracking**: Catalog's `Product.Price` is for cost; Storefront owns customer-facing pricing

## Integration Points

### Inbound (Commands This Service Will Handle)

**Cart Commands** (Future):
- `CreateCart`, `AddItemToCart`, `RemoveItemFromCart`, `UpdateCartItemQuantity`, `ApplyPromotion`, `Checkout`

**Order Commands** (Future):
- `PlaceOrder`, `CancelOrder`, `GetOrderStatus`, `GetOrderHistory`

### Outbound (Events This Service Will Publish)

**Cart Events**: Inform other systems of shopping behavior (analytics, abandoned cart recovery)

**Order Events**: Trigger workflows in Catalog (inventory holds), Payment (charge), Fulfillment (ship)

### Current Implementation

**Program.cs**:
- Configures ASP.NET Core web application
- Adds Aspire service defaults (telemetry, health checks, service discovery)
- Registers MassTransit with RabbitMQ connection
- No endpoints, no persistence, no consumers

**HostBuilderExtensions.cs**:
- Configures MassTransit with RabbitMQ
- Retrieves connection string from Aspire configuration
- Calls `ConfigureEndpoints` (but no consumers registered yet)
- No saga configuration, no event consumers

**AppHost Configuration**:
- MongoDB instance with data volume and MongoExpress UI
- `storefront` database provisioned
- RabbitMQ message broker reference
- Service runs alongside Catalog and ApiService

## Technology Stack Rationale

- **MassTransit**: Event-driven messaging, request/response, saga orchestration (when implemented)
- **MongoDB**: Document database for flexible, denormalized customer-facing data
- **RabbitMQ**: Message broker for cross-domain event publishing
- **ASP.NET Core**: Web application host (will host API endpoints or background workers)
- **Aspire**: Orchestration for local development, service discovery, observability

## Implementation Roadmap (Suggested)

### Phase 1: Read Model Foundation
1. Configure MongoDB DbContext or Repository pattern
2. Create `ProductReadModel` to store denormalized product views
3. Implement event consumers for `ProductCreated`, `ProductAvailabilityChanged`, `ProductDiscontinued`
4. Build customer-facing product query endpoints

**Why This First**: Establishes the consumer pattern without complex state management; enables API to serve product data independently of Catalog domain.

### Phase 2: Shopping Cart
1. Define cart contracts (`CreateCartRequest`, `AddItemRequest`, etc.)
2. Implement `CartStateMachine` with states: Active, Abandoned, Converted
3. Create cart persistence (MongoDB document)
4. Implement cart service layer
5. Add cart API endpoints

**Why This Second**: Carts are stateful but don't require cross-domain coordination; builds on Phase 1 product data.

### Phase 3: Order Placement
1. Define order contracts (`PlaceOrderRequest`, `OrderPlaced` event, etc.)
2. Implement `OrderStateMachine` or Order Saga
3. Create inventory hold requests to Catalog domain
4. Implement order service layer
5. Add order API endpoints

**Why This Third**: Orders require choreography with Catalog (inventory holds) and future Payment domain; most complex workflow.

### Phase 4: Order Lifecycle
1. Implement payment integration (stubbed or real)
2. Add fulfillment event subscriptions
3. Implement order status tracking
4. Add order history queries
5. Implement cancellation workflows with inventory release

## Common Patterns (When Implemented)

### Expected MongoDB Document Pattern

**Shopping Cart Document**:
```csharp
{
  "_id": Guid,
  "customerId": Guid?,
  "sessionId": string,
  "state": "Active" | "Abandoned" | "Converted",
  "items": [
    {
      "productId": Guid,
      "sku": string,
      "name": string,
      "quantity": int,
      "priceSnapshot": decimal,  // Price at time of add
      "inventoryId": Guid?
    }
  ],
  "createdOn": DateTimeOffset,
  "updatedOn": DateTimeOffset,
  "expiresOn": DateTimeOffset
}
```

**Order Document**:
```csharp
{
  "_id": Guid,
  "orderNumber": string,
  "customerId": Guid,
  "state": "Pending" | "PaymentAuthorized" | "Fulfilled" | "Cancelled",
  "items": [ /* denormalized product snapshots */ ],
  "totalAmount": decimal,
  "paymentDetails": { /* payment metadata */ },
  "fulfillmentDetails": { /* shipping metadata */ },
  "timeline": [
    { "timestamp": DateTimeOffset, "event": "OrderPlaced" },
    { "timestamp": DateTimeOffset, "event": "PaymentAuthorized" }
  ],
  "createdOn": DateTimeOffset,
  "updatedOn": DateTimeOffset
}
```

### Expected Event Consumer Pattern

```csharp
public class ProductAvailabilityConsumer : IConsumer<ProductAvailabilityChanged>
{
    private readonly IMongoCollection<ProductReadModel> _products;

    public async Task Consume(ConsumeContext<ProductAvailabilityChanged> context)
    {
        var update = Builders<ProductReadModel>.Update
            .Set(p => p.IsAvailable, context.Message.IsAvailable)
            .Set(p => p.Quantity, context.Message.Quantity)
            .Set(p => p.UpdatedOn, context.Message.UpdatedOn);

        await _products.UpdateOneAsync(
            p => p.ProductId == context.Message.ProductId,
            update
        );
    }
}
```

## Key Differences from Catalog Domain

| Aspect | Catalog | Storefront |
|--------|---------|------------|
| **Database** | PostgreSQL | MongoDB |
| **Data Model** | Relational (EF Core) | Document-oriented |
| **Primary Pattern** | State Machines (Choreography) | Sagas (Orchestration) + Event Consumers |
| **Concurrency** | Optimistic (Row Versioning) | Document Versioning or Optimistic Locking |
| **Focus** | Inventory Truth | Customer Experience |
| **Consistency** | Strong within aggregate | Eventual from Catalog events |
| **Stakeholders** | Warehouse/Operations | Customers/Sales |

## Next Steps for Implementation

1. **Define Contracts First**: Create `KbStore.Storefront.Abstractions` contracts for cart and order operations
2. **Implement Event Consumers**: Subscribe to Catalog events to build read models
3. **Configure MongoDB Persistence**: Set up MongoDB client and repository/DbContext pattern
4. **Implement Cart State Machine**: Start with cart lifecycle as proof-of-concept
5. **Add Service Layer**: Wrap state machines with service interfaces (follow Catalog pattern)
6. **Create API Endpoints**: Expose cart/order operations via `KbStore.ApiService`
7. **Add Tests**: Follow `EventingTestBase` pattern from Catalog tests

## Key Files Reference (Current)

**Infrastructure**:
- `Program.cs` - Application entry point with MassTransit setup
- `Extensions/HostBuilderExtensions.cs` - MassTransit configuration

**Project Files**:
- `KbStore.Storefront.csproj` - References ServiceDefaults, Abstractions, MassTransit packages

**Orchestration**:
- `../KbStore.AppHost/Program.cs` - Aspire configuration for MongoDB and message broker

## Summary

The Storefront domain has foundational infrastructure in place but awaits business logic implementation. It follows the same architectural patterns as Catalog (event-driven, state machines, bounded contexts) but uses MongoDB for document-oriented customer data. The next phase of development should focus on consuming Catalog events to build customer-facing read models, then implementing cart and order workflows.
