# KbStore Architecture Overview

## System Philosophy

KbStore implements a **distributed event-driven architecture** using Domain-Driven Design principles with MassTransit saga state machines as the core domain pattern. The system is designed for:

- **Bounded Context Isolation** - Each domain is self-contained with clear boundaries
- **Event-Driven Communication** - Domains communicate asynchronously via events
- **Saga-Based State Management** - Business logic lives in state machines, not services
- **Vertical Slice Architecture** - Each domain has complete stack from API to database
- **Testability** - Pure domain logic, mockable infrastructure, comprehensive test coverage

## High-Level Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                     HTTP Clients                             │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│              KbStore.ApiService (Gateway)                    │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  HTTP Endpoints (Minimal APIs)                        │  │
│  │  - /products/*    - /inventory/*                      │  │
│  └───────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  Event Consumers (Cross-Domain Orchestration)         │  │
│  │  - ProductCreatedConsumer                             │  │
│  │  - ProductNameUpdatedConsumer                         │  │
│  │  - InventoryQuantityChangedConsumer                   │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────┬───────────────────────────┬───────────────────┘
              │                           │
              │  Service Layer Calls      │  Event Publishing
              │                           │
    ┌─────────▼─────────┐       ┌────────▼──────────┐
    │  Catalog Domain   │       │ Storefront Domain │
    │   (PostgreSQL)    │       │    (MongoDB)      │
    └───────────────────┘       └───────────────────┘
              │                           │
              │                           │
    ┌─────────▼─────────┐       ┌────────▼──────────┐
    │  Service Layer    │       │  Service Layer    │
    │  - Command        │       │  - Command        │
    │  - Query          │       │  - Query          │
    └─────────┬─────────┘       └────────┬──────────┘
              │                           │
              │  MassTransit              │  MassTransit
              │  Request/Response         │  Request/Response
              │                           │
    ┌─────────▼─────────┐       ┌────────▼──────────┐
    │  State Machines   │       │  State Machines   │
    │  - Product        │       │  - SellableItem   │
    │  - Inventory      │       │                   │
    └─────────┬─────────┘       └────────┬──────────┘
              │                           │
    ┌─────────▼─────────┐       ┌────────▼──────────┐
    │  EF Core Context  │       │  MongoDB Client   │
    │  (Sagas + Query)  │       │  (Sagas + Query)  │
    └─────────┬─────────┘       └────────┬──────────┘
              │                           │
    ┌─────────▼─────────┐       ┌────────▼──────────┐
    │   PostgreSQL      │       │     MongoDB       │
    └───────────────────┘       └───────────────────┘
              │                           │
              └───────────────┬───────────┘
                              │
                    ┌─────────▼─────────┐
                    │     RabbitMQ      │
                    │  (Event Bus)      │
                    └───────────────────┘
```

## Core Design Principles

### 1. Repeatable Vertical Stack Pattern

Each bounded context follows identical layering:

```
HTTP/API Layer (ApiService)
    ↕ (HTTP calls + Event consumption)
Service Layer (*.Services)
    ↕ (MassTransit Request/Response)
Pure Domain (State Machines)
    ↕ (Saga persistence)
Database (PostgreSQL/MongoDB)
    ↕ (Event publishing)
Event Bus (RabbitMQ)
```

**Key Point**: This pattern repeats for EVERY domain. Learn it once, apply everywhere.

### 2. Domain Projects Are Algorithmically Pure

Domain projects (`KbStore.Catalog`, `KbStore.Storefront`) contain ONLY:
- State machine logic (business rules)
- Entity definitions
- EF Core/MongoDB configuration
- MassTransit registration

NO:
- HTTP concerns
- Service interfaces/implementations
- Cross-domain orchestration
- External API calls

**Why**: Pure domain logic is easy to test, reason about, and maintain.

### 3. Service Layers Hide Complexity

Service projects (`KbStore.Catalog.Services`, `KbStore.Storefront.Services`) provide:
- Simple awaitable methods
- Clean interfaces for API layer
- Validation before commands
- Exception translation (MassTransit → Domain exceptions)
- Time provider injection for testability

Services do NOT:
- Contain business logic
- Make direct database calls (except queries)
- Call other domains directly
- Publish events

### 4. ApiService Orchestrates Cross-Domain Workflows

`KbStore.ApiService` is the ONLY place where:
- Domains talk to each other (via event consumers)
- Business logic spans multiple bounded contexts
- Authentication/authorization happens (future)
- HTTP → Domain translation occurs

**Critical Rule**: Domains NEVER directly consume events from other domains. All inter-domain communication flows through ApiService consumers.

### 5. Event-Driven Communication

```
Catalog Domain publishes event:
  ProductCreated { ProductId, Sku, Name, ... }
       ↓ (RabbitMQ)
ApiService consumer receives event:
  ProductCreatedConsumer
       ↓ (orchestration logic)
Calls Storefront service:
  ISellableItemCommandService.CreateAsync(...)
       ↓ (MassTransit request)
Storefront state machine:
  SellableItemStateMachine handles CreateSellableItemRequest
       ↓ (saga transition)
Database persistence + event publishing
```

**Key Point**: Events are facts ("ProductCreated"), not commands ("CreateProduct"). They represent state changes that have already occurred.

## Bounded Contexts

### Catalog Domain (FULLY IMPLEMENTED)
**Responsibility**: "What exists physically and how much stock is available"

**Technology**:
- PostgreSQL database
- Entity Framework Core
- MassTransit saga state machines

**Entities**:
- **Product** - Physical product definition (SKU, name, dimensions, quantity)
  - States: Enabled, Disabled, Discontinued
  - Links to Inventory via `InventoryId` (optional)
  - Tracks stock availability and thresholds

- **Inventory** - Stock tracking and warehouse management
  - States: Available, Reserved, Discontinued
  - Tracks stock quantity, holds, releases
  - Publishes quantity change events

**Events Published**:
- `ProductCreated`, `ProductNameUpdated`, `ProductDimensionsUpdated`
- `ProductEnabled`, `ProductDisabled`, `ProductDiscontinued`
- `ProductAvailabilityChanged` (when stock changes)
- `InventoryQuantityChanged`, `InventoryHeld`, `InventoryReleased`
- `InventoryDiscontinued`, `InventoryDeleted`

**Key Files**:
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Catalog/Domains/Products/ProductStateMachine.cs`
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Catalog/Domains/Inventory/InventoryStateMachine.cs`
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Catalog/Persistence/ApplicationDbContext.cs`

### Storefront Domain (PARTIALLY IMPLEMENTED)
**Responsibility**: "What customers can buy and at what price"

**Technology**:
- MongoDB database
- MassTransit saga state machines
- MongoDB saga persistence

**Entities**:
- **SellableItem** - Customer-facing product representation
  - States: Draft, Active, Archived (implementation status: EXISTS)
  - Owns customer pricing (distinct from Catalog cost tracking)
  - Denormalized read model for fast queries
  - Cross-domain correlation via `ProductId` (same Guid as Catalog)

**Events Published** (PLANNED):
- `SellableItemActivated`, `SellableItemPriceChanged`
- `SellableItemArchived`

**Events Consumed** (VIA ApiService):
- `ProductCreated` → Create SellableItem in Draft state
- `ProductNameUpdated` → Update denormalized fields
- `ProductAvailabilityChanged` → Update availability status
- `ProductDiscontinued` → Archive SellableItem

**Implementation Status**:
- State machine: EXISTS but needs review
- Service layer: IN PROGRESS (being implemented)
- API endpoints: NOT CREATED
- Tests: MINIMAL

**Key Files**:
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Storefront/Domains/SellableItems/SellableItemStateMachine.cs`
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Storefront.Services/` (in progress)

### ApiService (Orchestration Layer - PARTIAL)
**Responsibility**: "HTTP gateway + cross-domain coordination"

**Technology**:
- ASP.NET Core Minimal APIs
- MassTransit event consumers
- Service layer clients

**HTTP Endpoints** (IMPLEMENTED):
- `/products/*` - Product CRUD operations
- `/inventory/*` - Inventory operations

**Event Consumers** (ACTIVE):
- `ProductCreatedConsumer` - Creates SellableItem when Product is created
- `ProductNameUpdatedConsumer` - Updates SellableItem name
- `ProductDiscontinuedConsumer` - Archives SellableItem
- More consumers planned as Storefront develops

**Implementation Status**:
- Catalog endpoints: COMPLETE
- Storefront endpoints: NOT CREATED
- Event consumers: ACTIVE for basic flows
- Authentication/Authorization: NOT IMPLEMENTED

**Key Files**:
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.ApiService/Program.cs`
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.ApiService/Endpoints/Catalog/`
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.ApiService/Consumers/Catalog/`

## Aspire Orchestration

The application uses .NET Aspire for local development orchestration.

**Infrastructure Components**:

```csharp
// KbStore.AppHost/Program.cs

var broker = builder.AddRabbitMQ("queue")        // Message broker
    .WithManagementPlugin();                     // UI on port 15672

var pgsql = builder.AddPostgres("pgsql")         // PostgreSQL server
    .WithPgWeb(conf => conf.WithHostPort(5050)); // UI on port 5050

var catalog_db = pgsql.AddDatabase("catalog");   // Catalog database

var mongo = builder.AddMongoDB("mongo")          // MongoDB server
    .WithMongoExpress();                         // UI on default port

var storefront_db = mongo.AddDatabase("storefront"); // Storefront database

// Domain services
builder.AddProject<KbStore_Catalog>("domain-catalog")
    .WithReference(broker)
    .WithReference(catalog_db);

builder.AddProject<KbStore_Storefront>("domain-storefront")
    .WithReference(broker)
    .WithReference(storefront_db);

// API service
builder.AddProject<KbStore_ApiService>("api")
    .WithReference(broker)
    .WithReference(catalog_db)  // For query operations
    .WithExternalHttpEndpoints();
```

**Management UIs**:
- Aspire Dashboard: Auto-launched with orchestration
- RabbitMQ Management: http://localhost:15672 (guest/guest)
- PgWeb (PostgreSQL): http://localhost:5050
- MongoExpress: Auto-assigned port

**Running**:
```bash
dotnet run --project /mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.AppHost
```

This single command starts:
- All domain services
- ApiService
- RabbitMQ container
- PostgreSQL container
- MongoDB container
- All management UIs

## State Machine Pattern Deep Dive

State machines are the HEART of KbStore architecture.

### Why State Machines?

Traditional approach:
```csharp
// BAD: Business logic scattered across services
public class ProductService
{
    public async Task DisableAsync(Guid productId)
    {
        var product = await _repo.GetAsync(productId);
        if (product.State == "Discontinued")
            throw new Exception("Cannot disable discontinued product");

        product.State = "Disabled";
        await _repo.SaveAsync(product);
        await _eventBus.PublishAsync(new ProductDisabled { ... });
    }
}
```

State machine approach:
```csharp
// GOOD: Business logic in state machine
During(Discontinued,
    When(DisableRequested)
        .Then(context => throw new CannotModifyDiscontinuedProduct(...))
);
```

**Benefits**:
1. **Explicit State Transitions** - All valid transitions are visible
2. **Business Rules Enforced** - Invalid operations throw at state machine level
3. **Event Publishing Built-in** - Events are part of transitions
4. **Testable** - Pure logic, easily tested with test harness
5. **Audit Trail** - MassTransit tracks all state changes
6. **Saga Orchestration** - Built-in support for distributed transactions

### State Machine Lifecycle

```
1. Command arrives via MassTransit request:
   CreateProductRequest { Sku, Name, ... }

2. State machine correlates to instance:
   - New instance: SelectId() generates CorrelationId
   - Existing instance: CorrelateById() finds it

3. State machine executes transition:
   Initially(
       When(Created)
           .Then(SetProperties)       // Update entity
           .TransitionTo(Enabled)     // Change state
           .PublishAsync(Event)       // Publish domain event
           .RespondAsync(Response)    // Respond to caller
   )

4. Saga repository persists:
   - Entity state saved to database
   - RowVersion incremented (optimistic concurrency)

5. Event bus publishes events:
   - ProductCreated published to RabbitMQ
   - Consumers in other domains react

6. Response sent:
   - Caller receives ProductModel
```

### Correlation Strategies

**By ID** (most common):
```csharp
Event(() => NameUpdated, e => e
    .CorrelateById(c => c.Message.ProductId)
    .OnMissingInstance(b => b.Execute(c =>
        throw new ProductNotFoundException(c.Message.ProductId)))
);
```

**By Business Key**:
```csharp
Event(() => Created, e => e
    .CorrelateBy((saga, context) => saga.Sku == context.Message.Sku)
    .SelectId(_ => NewId.NextSequentialGuid())
);
```

**By Foreign Key**:
```csharp
Event(() => InventoryQuantityChanged, e => e
    .CorrelateBy((saga, context) => saga.InventoryId == context.Message.InventoryId)
);
```

### Request/Response Pattern

For inter-saga communication:

```csharp
// 1. Define request property in state machine
Request(() => InventoryStatus, s => s.InventoryStatusId, c =>
{
    c.Timeout = TimeSpan.FromSeconds(5);
    c.ClearRequestIdOnFaulted = true;
});

// 2. Send request during state transition
Initially(
    When(Created)
        .IfElse(ctx => ctx.Saga.InventoryId != null,
            then => then
                .TransitionTo(InventoryStatus.Pending)
                .Request(InventoryStatus, c => c.Init<InventoryStatusRequest>(new {
                    c.Saga.InventoryId
                })),
            else => else
                .TransitionTo(Enabled)
        )
);

// 3. Handle response states
During(InventoryStatus.Pending,
    When(InventoryStatus.Completed)
        .Then(ctx => ctx.Saga.StockQuantity = ctx.Message.StockQuantity)
        .TransitionTo(Enabled),

    When(InventoryStatus.Faulted)
        .TransitionTo(Disabled),

    When(InventoryStatus.TimeoutExpired)
        .TransitionTo(Disabled)
);
```

## Cross-Domain Orchestration

**IMPORTANT**: Currently ACTIVE but still evolving as Storefront domain develops.

### How It Works

```
┌─────────────────┐
│ Catalog Domain  │
│  (PostgreSQL)   │
└────────┬────────┘
         │ ProductCreated event
         │ (via RabbitMQ)
         ▼
┌────────────────────────────────┐
│     ApiService Consumer        │
│  ProductCreatedConsumer        │
│                                │
│  - Receives event              │
│  - Idempotency check           │
│  - Orchestration logic         │
│  - Calls Storefront service    │
└────────┬───────────────────────┘
         │ CreateSellableItemRequest
         │ (via MassTransit)
         ▼
┌─────────────────┐
│ Storefront      │
│  SellableItem   │
│  State Machine  │
└─────────────────┘
```

### Current Active Consumers

**ProductCreatedConsumer**:
- Listens for `ProductCreated` events
- Creates `SellableItem` in Draft state
- Uses same `ProductId` for cross-domain correlation
- Implements idempotency check
- Status: ACTIVE

**ProductNameUpdatedConsumer**:
- Listens for `ProductNameUpdated` events
- Updates denormalized name in `SellableItem`
- Status: ACTIVE

**ProductDiscontinuedConsumer**:
- Listens for `ProductDiscontinued` events
- Archives corresponding `SellableItem`
- Status: ACTIVE

### Design Patterns for Consumers

```csharp
public class ProductCreatedConsumer : IConsumer<ProductCreated>
{
    private readonly ISellableItemCommandService _sellableItemService;
    private readonly ISellableItemQueryService _queryService;
    private readonly ILogger _logger;

    public async Task Consume(ConsumeContext<ProductCreated> context)
    {
        var msg = context.Message;

        // 1. Log entry
        _logger.LogInformation("Creating SellableItem for Product {ProductId}", msg.ProductId);

        try
        {
            // 2. Idempotency check (CRITICAL for event processing)
            var existing = await _queryService.GetByIdAsync(msg.ProductId, context.CancellationToken);
            if (existing != null)
            {
                _logger.LogInformation("SellableItem already exists - skipping");
                return;
            }

            // 3. Orchestration logic - business rules spanning domains
            await _sellableItemService.CreateAsync(
                sku: msg.Sku,
                name: msg.Name ?? msg.Sku,
                basePrice: 0m,  // Default per architecture decision
                productId: msg.ProductId,  // Cross-domain correlation
                cancellationToken: context.CancellationToken
            );

            // 4. Success logging
            _logger.LogInformation("Successfully created SellableItem");
        }
        catch (SellableItemConflictException ex)
        {
            // 5. Handle expected conflicts (race conditions, replays)
            _logger.LogWarning(ex, "Conflict - likely event replay");
            // Do NOT rethrow - this is expected
        }
        catch (Exception ex)
        {
            // 6. Handle transient failures
            _logger.LogError(ex, "Failed to create SellableItem");
            throw; // Let MassTransit retry
        }
    }
}
```

**Key Points**:
- Always check for idempotency (events can be replayed)
- Log extensively for debugging distributed systems
- Handle domain conflicts gracefully
- Only rethrow transient failures (MassTransit will retry)
- Business logic goes here, not in domain services

## Data Persistence Patterns

### PostgreSQL (Catalog Domain)

**EF Core Configuration**:
```csharp
// Saga persistence
services.AddSagaStateMachineRepository<ProductEntity>()
    .EntityFrameworkRepository(r =>
    {
        r.ExistingDbContext<ApplicationDbContext>();
        r.UsePostgres();
    });

// Optimistic concurrency
entity.Property(x => x.RowVersion)
    .HasColumnType("xid")  // PostgreSQL transaction ID
    .IsRowVersion();
```

**Migrations**:
- Auto-run on startup: `app.RunMigrationsAsync()`
- Manual: `dotnet ef migrations add Name --project KbStore.Catalog`

### MongoDB (Storefront Domain)

**Saga Persistence**:
```csharp
services.AddSagaStateMachineRepository<SellableItemEntity>()
    .MongoDbRepository(r =>
    {
        r.Connection = connectionString;
        r.DatabaseName = "storefront";
        r.CollectionName = "sellableItemSagas";
    });
```

**Indexes**:
- Created on startup via `app.EnsureMongoDbIndexesAsync()`
- Define in extension methods

## Exception Handling Strategy

### Domain Exceptions

Each domain defines specific exceptions:

```csharp
// Validation errors (400)
public class ProductValidationException : Exception
{
    public static ProductValidationException InvalidStockThreshold(int? value)
        => new($"Stock threshold must be >= 0, but was {value}");
}

// Not found errors (404)
public class ProductNotFoundException : Exception
{
    public Guid ProductId { get; }
    public ProductNotFoundException(Guid productId)
        : base($"Product {productId} not found")
    { }
}

// State errors (409)
public class ProductStateException : Exception
{
    public static ProductStateException AlreadyEnabled(Guid productId)
        => new(productId, $"Product {productId} is already enabled");
}

// Conflict errors (409)
public class ProductConflictException : Exception
{
    public static ProductConflictException DuplicateSku(string sku)
        => new(sku, $"Product with SKU '{sku}' already exists");
}
```

### Exception Translation

Service layer converts MassTransit faults:

```csharp
try
{
    var response = await client.GetResponse<CreateProductResponse>(...);
    return response.Message;
}
catch (RequestFaultException e)
{
    throw e.ToProductException();  // Extension method
}
```

### HTTP Status Mapping (Future)

Exceptions will map to HTTP status codes via middleware:
- `ValidationException` → 400 Bad Request
- `NotFoundException` → 404 Not Found
- `StateException` → 409 Conflict
- `ConflictException` → 409 Conflict
- Other → 500 Internal Server Error

## Testing Infrastructure

### Test Harness Architecture

```csharp
[TestFixture]
public class Product_Create : StateMachine_Tests<ProductStateMachine, ProductEntity>
{
    // Arrange: Setup test dependencies
    protected override void Arrange()
    {
        Client = Harness.Bus.CreateRequestClient<CreateProductRequest>();
    }

    // Act: Execute operation
    protected override async Task Act()
    {
        Response = await Client.GetResponse<CreateProductResponse>(...);
    }

    // Assert: Verify results
    [Test]
    public async Task It_should_create_successfully()
    {
        LastException.ShouldBeNull();
        Response.Message.Sku.ShouldBe("TEST_SKU");

        var saga = SagaHarness.Sagas.Contains(Response.Message.ProductId);
        saga.CurrentState.ShouldBe(ProductStates.Enabled);

        (await Harness.Published.Any<ProductCreated>()).ShouldBeTrue();
    }
}
```

**Test Infrastructure**:
- `EventingTestBase` - Base class with MassTransit test harness
- In-memory transport - No real RabbitMQ needed
- Saga harness - Direct access to saga instances
- Event verification - Check published/consumed events

## Security (PLANNED)

**Current State**: Wide open, no authentication/authorization

**Planned Implementation**:
- JWT-based authentication in ApiService
- Role-based authorization (Admin, Manager, Customer)
- Claims-based authorization for fine-grained control
- API key authentication for service-to-service calls

**Where It Goes**:
- Authentication middleware in ApiService
- Authorization policies on HTTP endpoints
- NO authentication in domain services (trust boundary at API layer)

## Scalability Considerations

### Current Architecture
- All services can scale horizontally
- RabbitMQ handles message distribution
- MassTransit manages consumer instances
- Saga persistence uses optimistic concurrency

### Future Optimizations
- Read replicas for query services
- CQRS with separate read models
- Event sourcing for audit trail
- Redis caching for hot data
- Competing consumer pattern for high throughput

## Migration Guide (Future Domains)

When adding a new bounded context:

1. **Create domain project** following Catalog/Storefront pattern
2. **Define entities** as state machine instances
3. **Implement state machines** with business rules
4. **Create abstractions project** with contracts and interfaces
5. **Build service layer** hiding MassTransit complexity
6. **Add database** (PostgreSQL, MongoDB, or other)
7. **Register in Aspire** orchestration
8. **Create API endpoints** in ApiService
9. **Add event consumers** for cross-domain coordination
10. **Write comprehensive tests** following existing patterns

The architecture is DESIGNED for this - each new domain follows the same playbook.
