# KbStore.ApiService - Architecture Documentation

## Purpose

**KbStore.ApiService** serves two critical roles:

### 1. HTTP Gateway
Exposes HTTP endpoints that translate external requests into domain service calls.

### 2. Orchestration Layer
**This is the high-level system that ties together well-implemented building blocks of functionality.** ApiService contains event consumers that coordinate workflows across bounded contexts (Catalog, Storefront, etc.).

```
┌──────────────────────────────────────────────────────┐
│              KbStore.ApiService                      │
│                                                       │
│  HTTP Endpoints          Event Consumers             │
│  (thin delegates)        (orchestration logic)       │
│       ↓                         ↓                    │
│  ┌──────────────────────────────────────────┐       │
│  │  Catalog.Services  ←→  Storefront.Services│      │
│  └──────────────────────────────────────────┘       │
└──────────────────────────────────────────────────────┘
```

**What ApiService DOES**:
1. **HTTP Endpoints**: Thin facades that delegate to domain services
2. **Cross-Domain Orchestration**: Event consumers that coordinate between Catalog and Storefront
3. **Authentication/Authorization**: API-specific concerns (delegates user identity as Guid)
4. **Business Logic Spanning Domains**: Rules that require coordination between multiple bounded contexts
5. **Infrastructure Integration**: Aspire observability, health checks, problem details

**What ApiService DOES NOT DO**:
- ❌ Domain logic (handled by Catalog/Storefront state machines)
- ❌ Direct database mutation (always delegates to service layers)
- ❌ Cross-cutting concerns within domains (email, notifications - domains handle their own)

## Architectural Decisions

### Why Minimal APIs Over Controllers

The project uses **ASP.NET Core Minimal APIs** rather than traditional MVC controllers. This choice reflects:

- **Vertical slice architecture**: Each endpoint group (Inventory, Products) is self-contained in its own static class
- **Explicit dependencies**: Services are injected per-endpoint via `[FromServices]` attributes, making dependencies obvious
- **Route co-location**: HTTP routes are defined alongside their handlers (`MapTo` methods), improving discoverability
- **Reduced ceremony**: No controller base classes, no attribute routing scattered across methods
- **Testability**: Static methods with explicit parameters are easier to unit test in isolation

### Why Payload Validation at the API Boundary

Payload objects (`CreateInventoryPayload`, `CreateProductPayload`) implement `IsValid()` methods that perform **structural validation only**:

- **PartNumber/SKU cannot be null or whitespace** - these are identity fields
- **StockThreshold must be >= 0 if provided** - negative thresholds are nonsensical
- **LeadTime must be positive if provided** - negative time is invalid
- **Description can be empty but not null for Inventory** - domain allows blank descriptions

**Why validation here and not in the domain?**

The API layer validates that requests are **well-formed** before invoking domain services. The domain layer validates **business rules**. This separation means:

- Invalid HTTP payloads return `400 Bad Request` immediately without domain service calls
- Domain exceptions (duplicate SKUs, state violations) are thrown from the domain layer and handled via exception middleware
- The API layer doesn't duplicate domain business logic

### Why Thin Endpoints

Each endpoint method follows a consistent pattern:

1. **Validate input** (structural checks only)
2. **Call domain service** (single method call)
3. **Return result** (via `Results.Ok()` or `Results.BadRequest()`)

This deliberate thinness ensures:

- **Single Responsibility**: Endpoints orchestrate, they don't implement
- **Testing focus**: Domain services contain testable logic; endpoints test HTTP contract compliance
- **Low coupling**: API layer can evolve independently from domain layer

### Why No Error Mapping for Domain Exceptions

Endpoints **do not catch domain exceptions**. Instead:

- ASP.NET Core's exception middleware (`app.UseExceptionHandler()`) handles all exceptions globally
- This middleware converts exceptions to `ProblemDetails` responses
- Tests verify endpoints throw expected exceptions, not that they return specific HTTP codes

**Rationale**: Centralizing error handling prevents inconsistent error responses across endpoints and reduces boilerplate.

### Why MassTransit Integration at the API Layer (Orchestration)

The API service configures **MassTransit** for message bus integration:

```csharp
services.AddMassTransit(bus =>
{
    bus.AddConsumers(typeof(Program).Assembly);
    bus.UsingRabbitMq((ctx, cfg) => { ... });
});
```

**Why here?**

ApiService is the **orchestration layer** that coordinates workflows across domains:

- **Domain Events Flow Here**: Catalog publishes `InventoryQuantityChanged` → ApiService consumer receives it
- **Cross-Domain Coordination**: ApiService consumer calls Storefront services to update `SellableItem` availability
- **Business Rules Spanning Domains**: Orchestration consumers can implement rules like "when stock drops below threshold AND item is on promotion, trigger notification"
- **Database-Driven Rules** (Future): Consumers may read configuration tables to determine routing logic

**Pattern**:
```csharp
public class InventoryQuantityChangedConsumer : IConsumer<InventoryQuantityChanged>
{
    private readonly IStorefrontService _storefrontService;

    public async Task Consume(ConsumeContext<InventoryQuantityChanged> context)
    {
        // Orchestration logic: Catalog stock changed → update Storefront
        await _storefrontService.UpdateAvailability(
            productId: context.Message.ProductId,  // Guid correlation
            isAvailable: context.Message.StockQuantity > 0,
            cancellationToken: context.CancellationToken
        );
    }
}
```

**Note**: As of this analysis, `ProductAvailabilityConsumer` is a placeholder. Full event choreography will be documented in **INTEGRATION.md**.

### Why Aspire Service Defaults

The API integrates with .NET Aspire via `builder.AddServiceDefaults()`:

- **OpenTelemetry**: Automatic tracing, metrics, and logging for observability
- **Service Discovery**: Simplifies communication with other services in distributed deployments
- **Health Checks**: `/health` and `/alive` endpoints for container orchestration
- **Resilience**: Standard retry/circuit breaker patterns for HTTP clients

**Rationale**: Aspire provides production-ready patterns without custom infrastructure code.

## Business Rules Enforced at the API Layer

These rules are **structural constraints**, not domain logic. They prevent invalid data from entering the system.

### Inventory Creation

- `PartNumber` is required and cannot be whitespace
- `Description` must not be null (but can be empty or whitespace)
- `StockQuantity` must be >= 0

### Product Creation

- `Sku` is required and cannot be whitespace
- `Name` is optional (can be null)
- `Dimensions` are optional
- `InventoryId` is optional (products can exist without linked inventory)
- `StockThreshold` must be >= 0 if provided
- `LeadTime` must have positive duration if provided

### Quantity Operations (Inventory)

- **Increase/Decrease**: Quantity parameter must be > 0 (zero or negative values are rejected)
- This prevents accidental no-ops and ensures intentional changes

### Description Updates (Inventory)

- `Description` cannot be null or whitespace when updating
- This ensures updates are meaningful

### Stock Threshold Updates (Products)

- `StockThreshold` must be >= 0 when provided
- Negative thresholds would break low-stock alerts

### Lead Time Updates (Products)

- `LeadTime` must have non-negative duration if provided
- Negative lead times are logically invalid

## Domain Boundaries

### What This Layer Does NOT Do

- **Business logic**: Creating aggregates, enforcing state transitions, calculating availability
- **Persistence**: No direct database access; delegates to domain services
- **Event publishing**: Domain services publish events; API consumes them
- **Complex validation**: SKU uniqueness, inventory state rules, product lifecycle enforcement - all in domain layer

### Clear Separation: Command vs Query

The API layer consumes:

- **Command Services** (`IInventoryCommandService`, `IProductCommandService`): For mutations
- **Query Services** (`IInventoryQueryService`, `IProductQueryService`): For read-only operations

This CQRS-lite pattern ensures:

- Writes go through domain aggregates
- Reads can optimize for specific use cases without domain model constraints
- Clear intent: `GetAll` uses query service, `Create` uses command service

## Integration Points

### Catalog Services

The API depends on:

- `KbStore.Catalog.Abstractions` - Service contracts and domain models
- `KbStore.Catalog.Services` - Actual implementations (registered via `AddCatalogServices()`)

**Service Registration**: `Extensions/WebApplicationBuilderExtensions.cs`

### Message Bus (MassTransit)

- Configured to use **RabbitMQ** via connection string `"queue"`
- Auto-discovers consumers in `KbStore.ApiService` assembly
- Endpoints auto-configured via `cfg.ConfigureEndpoints(ctx)`

**Current Consumers**: None implemented (placeholder exists)

### Observability (OpenTelemetry)

- Traces: ASP.NET Core requests, HTTP client calls
- Metrics: Runtime metrics, HTTP instrumentation
- Logs: Structured logging with scopes

**Integration**: Via `ServiceDefaults`

### Health Checks

- `/health`: All registered checks must pass
- `/alive`: Only "live"-tagged checks must pass (liveness probe for orchestrators)

**Default Check**: "self" check always returns healthy

## Endpoint Organization

### Route Grouping

Endpoints are organized by aggregate:

- `/inventory/*` - Inventory operations
- `/products/*` - Product operations

Each group uses **route prefixes** to establish clear boundaries.

### HTTP Verb Mapping

- `POST /inventory` - Create new inventory item
- `GET /inventory/{id}` - Get single item
- `GET /inventory` - Paginated search
- `PATCH /inventory/{id}/increase/{quantity}` - Increase stock
- `PATCH /inventory/{id}/description` - Update description
- `PATCH /inventory/{id}/hold` - Place hold
- `DELETE /inventory/{id}` - Discontinue item

**Pattern**:
- `POST` for creation
- `GET` for retrieval
- `PATCH` for partial updates (task-based, not resource replacement)
- `DELETE` for logical deletion

### Why PATCH for State Changes

Operations like "hold", "release", "enable", "disable" use **PATCH** rather than **PUT**:

- These are **commands**, not resource replacements
- They represent business operations (not just field updates)
- PATCH semantics align with task-based APIs

### Pagination Strategy

Both inventory and product queries support pagination via:

- `Page` (zero-indexed)
- `Size` (default: 25 items)

Returns `PaginatedResponse<T>` containing:
- `TotalItems`: Total count across all pages
- `Page`, `Size`: Echo request parameters
- `Results`: `IAsyncEnumerable<T>` for efficient streaming

**Why IAsyncEnumerable?**

- Supports **streaming large result sets** without loading everything into memory
- Integrates with EF Core's async query execution
- Allows clients to consume results incrementally

## OpenAPI Integration

The API exposes OpenAPI documentation via:

- `app.MapOpenApi()`: Generates OpenAPI JSON
- `app.MapScalarApiReference()`: Interactive API documentation UI

**Metadata**: Endpoints use `[ProducesResponseType]` attributes to document possible responses:

- `200` with model type - Success
- `400` with `ProblemDetails` - Validation failure
- `404` with `ProblemDetails` - Resource not found (Products)
- `409` with `ProblemDetails` - Conflict (duplicate SKU/PartNumber)
- `500` with `ProblemDetails` - Unexpected error

## Testing Strategy

### Test Hierarchy

All endpoint tests inherit from `Endpoints_Tests` base class, which provides:

- **Mock resolution**: `MockOf<TService>()` creates NSubstitute mocks
- **Endpoint execution**: `Execute(handler, params)` invokes delegates with dependency injection
- **Exception capture**: Tests can verify thrown exceptions without try/catch blocks

**Test Pattern (AAA)**:

```csharp
public class When_creating_successfully : Create_Tests
{
    protected override void Arrange()
        => MockOf<IService>().Method().Returns(value);

    protected override async Task Act()
        => Output = await Execute(Endpoint.Handler, payload);

    [Test]
    public void It_should_be_successful()
        => Output.ShouldBeOfType<Ok<Model>>();
}
```

### What Tests Verify

1. **Happy path**: Endpoint returns `200 OK` with correct model type
2. **Validation failures**: Invalid payloads return `400 Bad Request` without calling domain services
3. **Exception propagation**: Domain exceptions bubble up to exception middleware (tests verify they throw)
4. **Service invocation**: Endpoints call expected service methods with correct parameters

**What tests do NOT verify**:

- HTTP status codes for exceptions (handled by middleware)
- Serialization/deserialization (integration tests)
- Actual database state (domain service tests)

## Key Design Patterns

### Vertical Slice Architecture

Each aggregate (Inventory, Products) is self-contained:

- Endpoints in `Endpoints/Catalog/{Aggregate}Endpoints.cs`
- Tests in `ApiService.Tests/Endpoints/Catalog/{Aggregate}/*_Tests.cs`
- Routes registered via static `MapTo(app)` method

**Benefits**: Adding a new aggregate doesn't require modifying existing endpoint classes.

### Static Endpoint Handlers

Endpoints are **static methods** rather than instance methods:

```csharp
public static async Task<IResult> Create(
    [FromBody] CreateInventoryPayload payload,
    [FromServices] IInventoryCommandService commandService,
    CancellationToken cancellationToken)
```

**Why?**

- No hidden dependencies (everything is explicit)
- No shared state between requests
- Easier to reason about concurrency
- Simpler unit testing

### Dependency Injection via FromServices

Each parameter is explicitly attributed:

- `[FromBody]` - Request payload
- `[FromRoute]` - URL path segment
- `[FromServices]` - Injected dependency
- `[AsParameters]` - Complex parameter binding (pagination queries)

**Rationale**: Explicit binding prevents ambiguity and makes API contracts clear.

## Evolution Notes

### Placeholder Implementations

- `ProductAvailabilityConsumer`: Empty class waiting for event consumption logic
- `RequestClientExtensions`: Empty static class (likely for MassTransit request/response patterns)

These indicate **future integration points** for event-driven workflows.

### Expected Future Changes

1. **Event Consumers**: React to `ProductAvailabilityChanged`, `InventoryQuantityChanged` events
2. **Request/Response Messaging**: Use MassTransit's request client for synchronous RPC patterns
3. **Search Filtering**: Extend `PaginatedQuery` types with filter properties (SKU, status, etc.)
4. **Authentication/Authorization**: Currently no auth; likely addition of JWT bearer tokens
5. **Rate Limiting**: Not present; production deployments may add

## Key Files Reference

**Endpoints**:
- `Endpoints/Catalog/InventoryEndpoints.cs`
- `Endpoints/Catalog/ProductEndpoints.cs`

**Configuration**:
- `Program.cs` - Application entry point
- `Extensions/WebApplicationBuilderExtensions.cs` - Service registration

**Consumers**:
- `Consumers/ProductAvailabilityConsumer.cs` (placeholder)

## Cross-Reference Notes

- For event wiring details, see: **INTEGRATION.md** (to be written)
- For domain service internals, see: **../KbStore.Catalog.Services/ARCHITECTURE.md**
- For state machine logic, see: **../KbStore.Catalog/ARCHITECTURE.md**
