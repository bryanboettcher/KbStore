# KbStore.Catalog.Services Architecture

## Purpose

This service layer exists to **wrap the self-contained, algorithmically pure Catalog domain** with a clean interface for external consumers. It is part of the **repeatable vertical stack pattern** used across all domains:

```
Service Layer (this project)
    ↓
Pure Domain (KbStore.Catalog - state machines, business rules)
    ↓
Database (PostgreSQL)
    ↓
Events (MassTransit/RabbitMQ)
```

The service layer provides:

1. **Simple awaitable methods** - External consumers call straightforward async methods without understanding state machines or messaging
2. **Exception translation** - MassTransit `RequestFaultException` instances become strongly-typed domain exceptions
3. **Pre-validation** - Input validation at the boundary prevents invalid requests from reaching the domain
4. **CQRS separation** - Commands use state machines; queries bypass them for read optimization
5. **Stable contracts** - The interface remains consistent even if domain implementation changes

## Architectural Decisions

### WHY the Wrapper Pattern?

**Decision**: Wrap MassTransit state machine interactions behind domain service interfaces.

**Rationale**:
- State machines are an internal implementation detail of the Catalog bounded context
- External consumers (API endpoints, other services) should depend on stable abstractions, not MassTransit-specific contracts
- Enables future architectural changes (different messaging system, different persistence) without breaking consumers
- Provides a natural location for cross-cutting concerns (validation, logging, authorization)

### WHY Exception Translation?

**Decision**: Convert `RequestFaultException` to domain-specific exceptions using extension methods.

**Rationale**:
- MassTransit exceptions are infrastructure concerns, not business concerns
- Domain exceptions carry structured metadata (ProductId, SKU, CurrentState) that consumers need
- Exception translation happens in one place (`*RequestFaultExceptionExtensions`), ensuring consistency
- Consumers can use typed catch blocks (`ProductNotFoundException`, `InventoryStateException`) instead of parsing exception messages
- The `Data` dictionary in domain exceptions allows reconstruction of strongly-typed exceptions after crossing the message boundary

**Implementation Pattern**:
```csharp
// Service wraps MassTransit interaction
try {
    var response = await client.GetResponse<CreateProductResponse>(request);
    return response.Message;
} catch (RequestFaultException e) {
    throw e.ToProductException();  // Translate to domain exception
}
```

### WHY Pre-Validation at Service Boundary?

**Decision**: Validate business rules in the service layer before dispatching messages.

**Rationale**:
- **Fail-fast principle**: Invalid requests should not consume messaging resources (serialization, network, state machine loading)
- **Consistent error experience**: All validation errors are synchronous, before the async message dispatch
- **Message bus protection**: Prevents malformed messages from entering the system
- **State machine simplicity**: State machines handle state transitions and business logic; services handle input validation

**Validation Types**:
- **Parameter validation**: `Guid.Empty`, null strings, whitespace-only strings
- **Business rule validation**: Negative quantities, invalid lead times, negative stock thresholds
- These validations are **duplicated** between service and state machine - service validates early, state machine validates defensively

### WHY CQRS Separation?

**Decision**: Command services use MassTransit; query services use DbContext directly.

**Rationale**:
- **Commands modify state**: Must interact with state machines to ensure state transitions are valid and events are published
- **Queries read state**: Bypassing the state machine for reads improves performance and reduces messaging overhead
- **Read model optimization**: Query services use EF Core projection to load only necessary fields, avoiding state machine hydration costs
- **Consistency model**: Commands are eventually consistent (state machine processes request); queries are read-committed (direct database read)

**Query Service Pattern**:
```csharp
// Direct database access, optimized projections
var serverQuery = _context.Products
    .Skip(query.Page * query.Size)
    .Take(query.Size)
    .Select(entity => new { /* only needed fields */ })
    .AsAsyncEnumerable();
```

### WHY Injected Time Provider?

**Decision**: `Func<DateTimeOffset>` is injected into command services.

**Rationale**:
- **Testability**: Tests can control time (see `CommandService_Tests` base class with `Now` and `Later` constants)
- **State machine consistency**: Timestamp is sent in the message, ensuring the state machine uses the same timestamp for all operations in a transaction
- **Audit trail accuracy**: `CreatedOn` and `UpdatedOn` reflect when the command was initiated, not when the state machine processed it

## Business Rules

### Inventory Domain

**Creation Rules**:
- PartNumber: Required, cannot be null, empty, or whitespace
- Description: Required, cannot be null or empty
- StockQuantity: Must be >= 0 (zero quantity is allowed for new items)
- Duplicate PartNumber: **Allowed** - creates existing item (idempotent), returns existing state with later UpdatedOn timestamp but original CreatedOn

**Quantity Rules**:
- Increase: Only positive whole numbers, cannot increase while OnHold or Discontinued
- Decrease: Only positive whole numbers, cannot decrease below zero, cannot decrease while OnHold or Discontinued
- When Available: Decrease by more than StockQuantity throws `InsufficientStock` validation exception

**State Transition Rules**:
- **Available → OnHold**: Hold prevents quantity modifications; description updates still allowed
- **OnHold → Available**: Release returns to normal operations
- **Available/OnHold/Backordered → Discontinued**: Delete marks as discontinued (soft delete)
- **Discontinued → Available**: Calling Create with same PartNumber resurrects the item
- **Discontinued → (final)**: Second Delete call finalizes (hard delete)
- **Backordered → Available**: Increasing quantity when Backordered returns to Available

**Protected Operations**:
- OnHold: Cannot increase/decrease quantity, cannot create, cannot hold again
- Backordered: Cannot decrease quantity, cannot hold, cannot create (but can increase to restock)
- Discontinued: Cannot modify at all (except Delete to finalize, or Create to resurrect)

### Product Domain

**Creation Rules**:
- SKU: Required, cannot be null, empty, or whitespace
- Name: Required, cannot be null, empty, or whitespace
- Quantity: Must be > 0 (products must start with at least 1 unit)
- StockThreshold: Must be >= 0 if provided
- LeadTime: Must be >= 0 if provided (negative TimeSpan is invalid)
- Dimensions: Optional
- InventoryId: Optional (products may or may not link to inventory)

**Update Rules**:
- Name: Cannot be null, empty, or whitespace
- Quantity: Must be > 0
- StockThreshold: Must be >= 0 if provided
- LeadTime: Must be >= 0 if provided
- All updates: Cannot modify Discontinued products

**State Transition Rules**:
- **Enabled → Disabled**: Disables product, makes unavailable for sale
- **Disabled → Enabled**: Re-enables product
- **Enabled/Disabled → Discontinued**: Delete marks as discontinued
- **Discontinued**: Terminal state - no further modifications allowed (including Enable/Disable)

**Business Invariants**:
- `IsAvailable = IsStocked && IsEnabled` (computed property)
- Product cannot be deleted while Enabled (must be Disabled first)
- SKU uniqueness enforced by state machine (throws `ProductConflictException` on duplicate)

## Domain Boundaries

### What This Layer IS Responsible For:

1. **Input validation** - Parameter checking, basic business rule validation
2. **Message construction** - Building MassTransit request messages with correct structure
3. **Exception translation** - Converting infrastructure exceptions to domain exceptions
4. **Read model projection** - Transforming database entities into domain models for queries
5. **Timestamp injection** - Providing consistent timestamps for audit trails

### What This Layer IS NOT Responsible For:

1. **State machine logic** - Handled by `InventoryStateMachine` and `ProductStateMachine` in `KbStore.Catalog`
2. **Event publishing** - State machines publish domain events (`InventoryCreated`, `ProductEnabled`, etc.)
3. **State transition enforcement** - State machines validate which operations are allowed in which states
4. **Persistence** - MassTransit saga persistence handles state machine storage
5. **Business workflows** - Multi-step processes are orchestrated by sagas, not services

### Bounded Context Interface:

```
External World (API Endpoints, Consumers)
         ↓
    [Service Layer] ← You are here
         ↓
    [MassTransit Message Bus]
         ↓
    [State Machines] (Internal implementation)
         ↓
    [Persistence & Events]
```

## Integration Points

### Upstream Dependencies (What Services Depend On):

1. **KbStore.Catalog.Abstractions** - Service interfaces, domain models, exceptions
2. **MassTransit** - `IClientFactory` for creating request clients
3. **ApplicationDbContext** - Direct database access for query services
4. **Time provider** - `Func<DateTimeOffset>` for consistent timestamps

### Downstream Consumers (What Depends On Services):

1. **ApiService HTTP Endpoints** - `KbStore.ApiService/Endpoints/Catalog/*` use command/query services to handle HTTP requests
2. **ApiService Event Consumers** - Orchestration consumers in ApiService use these services to coordinate cross-domain workflows
   - Example: `InventoryQuantityChangedConsumer` may call query services to check product state before updating Storefront
3. **Background Jobs** - Scheduled tasks may use services for batch operations

### State Machine Interaction:

**Command Flow**:
```
1. API Endpoint calls service method
2. Service validates input (fast-fail)
3. Service creates MassTransit request client
4. Service sends request message with timestamp
5. State machine processes message, validates state transitions
6. State machine responds with updated model
7. State machine publishes domain event (async, fire-and-forget)
8. Service returns model to caller
```

**Query Flow**:
```
1. API Endpoint calls query service
2. Query service builds EF Core query with pagination
3. Query service projects database entities to anonymous types (server-side)
4. Query service materializes results as IAsyncEnumerable
5. Query service transforms to domain models (client-side)
6. Query service returns paginated response
```

### Message Contracts:

Services interact with these message types (defined in `KbStore.Catalog.Abstractions.Contracts`):

**Inventory Commands**: `CreateInventoryRequest`, `IncreaseInventoryQuantityRequest`, `DecreaseInventoryQuantityRequest`, `UpdateInventoryDescriptionRequest`, `HoldInventoryRequest`, `ReleaseInventoryRequest`, `DeleteInventoryRequest`, `InventoryStatusRequest`

**Product Commands**: `CreateProductRequest`, `UpdateProductNameRequest`, `UpdateProductQuantityRequest`, `UpdateProductDimensionsRequest`, `UpdateProductStockThresholdRequest`, `UpdateProductLeadTimeRequest`, `EnableProductRequest`, `DisableProductRequest`, `DeleteProductRequest`, `ProductStatusRequest`

**Response Types**: `CreateInventoryResponse`, `UpdateInventoryResponse`, `HoldInventoryResponse`, `ReleaseInventoryResponse`, `DeleteInventoryResponse`, `InventoryStatusResponse` (and corresponding Product responses)

**Domain Events** (published by state machines, NOT by services): `InventoryCreated`, `InventoryQuantityIncreased`, `InventoryQuantityDecreased`, `InventoryDescriptionUpdated`, `InventoryHeld`, `InventoryReleased`, `InventoryDiscontinued`, `ProductCreated`, `ProductNameUpdated`, etc.

### Extension Registration:

Services are registered using `ServiceCollectionExtensions.AddCatalogServices()`:
- Registers `ApplicationDbContext` with Npgsql connection string
- Registers command services as Scoped (MassTransit request clients are scoped)
- Registers query services as Scoped (DbContext is scoped)
- Registers `Func<DateTimeOffset>` as Transient (returns `DateTimeOffset.UtcNow`)

## Key Architectural Insight

This service layer is a **strategic boundary** that isolates the Catalog domain's implementation choices (MassTransit state machines, saga persistence) from the rest of the system. It's the public API of the Catalog bounded context.

**Repeatable Pattern**: This same vertical stack pattern (Service → Domain → Database → Events) is replicated across bounded contexts:
- Catalog.Services wraps Catalog domain (PostgreSQL)
- Storefront.Services will wrap Storefront domain (MongoDB)
- Future domains follow the same pattern

**Orchestration**: The ApiService layer consumes these services to:
- Handle HTTP requests (thin endpoints delegate to services)
- Orchestrate cross-domain workflows (event consumers call services)
- Tie together well-implemented building blocks of functionality

## Key Files Reference

**Service Implementations**:
- `MassTransitInventoryCommandService.cs`
- `MassTransitProductCommandService.cs`
- `DbContextInventoryQueryService.cs`
- `DbContextProductQueryService.cs`

**Exception Translation**:
- `Extensions/InventoryRequestFaultExceptionExtensions.cs`
- `Extensions/ProductRequestFaultExceptionExtensions.cs`

**Registration**:
- `Extensions/ServiceCollectionExtensions.cs`
