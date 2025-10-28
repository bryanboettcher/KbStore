# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

KbStore is a .NET 9 distributed application built with .NET Aspire orchestration. It implements an event-driven architecture using MassTransit Saga State Machines to manage domain entities through state transitions. The application is organized around Domain-Driven Design (DDD) principles with separate bounded contexts.

## Architecture

### Core Design Principles

**Repeatable Vertical Stack Pattern**: Each bounded context follows the same layered structure:

```
ApiService (Orchestration Layer)
    ↓
Service Layer (.Services project) - Simple awaitable methods
    ↓
Pure Domain (state machines, business rules)
    ↓
Database (PostgreSQL, MongoDB, etc.)
    ↓
Events (MassTransit/RabbitMQ)
```

**Key Characteristics**:
- **Domain Projects** are self-contained, algorithmically pure implementations focused on business logic
- **Service Layers** wrap domains with simple interfaces, hiding MassTransit complexity
- **ApiService** is the orchestration layer that ties domains together via event consumers
- **Domains never directly communicate** - ApiService coordinates cross-domain workflows

### Aspire Orchestration

The application uses .NET Aspire for local development orchestration, configured in `KbStore.AppHost/Program.cs`. The orchestration defines:

- **RabbitMQ** (`queue`): Message broker for MassTransit communication
- **PostgreSQL** (`pgsql`): Database server with PgWeb UI on port 5050
  - `catalog` database: Used by Catalog domain
- **MongoDB** (`mongo`): Document database with MongoExpress UI
  - `storefront` database: Used by Storefront domain

**Running the application:**
```bash
dotnet run --project KbStore.AppHost
```

This starts all services, databases, and management UIs via Aspire.

### Domain Structure

The solution is organized into domain-specific vertical slices under the `Domains/` solution folder:

#### Catalog Domain (Physical Inventory Tracking)
**Responsibility**: Tracks "what exists physically" and "how much stock is available"

- **KbStore.Catalog**: Pure domain - state machines for Product and Inventory entities (PostgreSQL)
- **KbStore.Catalog.Abstractions**: Contracts, exceptions, and service interfaces
- **KbStore.Catalog.Services**: Service wrapper layer exposing simple awaitable methods
- **KbStore.Catalog.Tests**: Domain tests

**Key Point**: Catalog does NOT own customer-facing pricing. `Product.Price` is for internal cost tracking.

#### Storefront Domain (Customer-Facing Transactions)
**Responsibility**: Manages "what customers can buy" and "at what price"

- **KbStore.Storefront**: Pure domain - state machines for SellableItem, Cart, Order entities (MongoDB) - **Placeholder**
- **KbStore.Storefront.Abstractions**: Contracts and interfaces - **Placeholder**
- **KbStore.Storefront.Tests**: Domain tests

**Key Point**: Storefront owns customer-facing pricing, promotions, and denormalized product read models.

#### ApiService (Orchestration Layer)
**Responsibility**: HTTP gateway + cross-domain orchestration

- **KbStore.ApiService**:
  - **HTTP Endpoints**: Thin delegates to domain services
  - **Event Consumers**: Orchestration logic that coordinates workflows across domains
  - **Authentication/Authorization**: API-specific concerns, delegates user identity as Guid
  - **Business Logic Spanning Domains**: Rules requiring coordination between Catalog and Storefront
- **KbStore.ApiService.Tests**: API integration tests

**Key Point**: ApiService contains event consumers that call domain service layers to coordinate cross-domain workflows. Domains do not directly communicate.

### State Machine Pattern

This codebase uses **MassTransit Saga State Machines** as the core domain pattern. State machines manage entity lifecycle and enforce business rules through state transitions.

**Key characteristics:**
- Entities inherit from `SagaStateMachineInstance` (not traditional DDD aggregates)
- State machines are defined in `*StateMachine.cs` files (e.g., `ProductStateMachine.cs`, `InventoryStateMachine.cs`)
- State transitions are triggered by commands sent via MassTransit's request/response
- Each state machine defines:
  - States (e.g., `Enabled`, `Disabled`, `Discontinued`)
  - Events (e.g., `Created`, `NameUpdated`, `Deleted`)
  - Transitions between states based on events
  - Business logic executed during transitions

**Example: Product State Machine**
Located in `KbStore.Catalog/Domains/Products/ProductStateMachine.cs`:
- States: `Enabled`, `Disabled`, `Discontinued`
- Handles commands like `CreateProductRequest`, `UpdateProductNameRequest`, `DeleteProductRequest`
- Reacts to inventory events like `InventoryQuantityChanged`, `InventoryDiscontinued`
- Enforces rules (e.g., cannot modify discontinued products)

**Entity structure:**
```csharp
public class ProductEntity : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }      // Primary key
    public uint RowVersion { get; set; }          // Optimistic concurrency
    public int CurrentState { get; set; }         // State machine state
    // ... domain properties
}
```

**Adding a new state machine:**
1. Create entity class inheriting `SagaStateMachineInstance`
2. Create `*SagaMap` for EF Core configuration
3. Create state machine class inheriting `MassTransitStateMachine<TEntity>`
4. Register in `HostBuilderExtensions.AddMassTransit()` with EF repository
5. Create migration for the new entity

### Service Layer Pattern

Services interact with state machines using MassTransit's request/response pattern:

```csharp
public interface IProductCommandService
{
    Task<ProductModel> CreateAsync(...);
    Task<ProductModel> UpdateNameAsync(Guid productId, string? name);
    // ... other operations
}
```

Services are located in `KbStore.Catalog.Services` and send messages to state machines:
- Command messages trigger state transitions
- State machines respond with current state
- Events are published for inter-domain communication

### Cross-Domain Orchestration Pattern

**ApiService acts as the orchestration layer** that coordinates workflows across bounded contexts.

**How it works**:

1. **Catalog domain** publishes event: `InventoryQuantityChanged`
2. **ApiService consumer** receives event via RabbitMQ
3. **ApiService consumer** calls `StorefrontService.UpdateAvailability()` to coordinate
4. **Storefront domain** updates `SellableItem` read model via state machine

```csharp
// In KbStore.ApiService/Consumers/
public class InventoryQuantityChangedConsumer : IConsumer<InventoryQuantityChanged>
{
    private readonly IStorefrontService _storefrontService;

    public async Task Consume(ConsumeContext<InventoryQuantityChanged> context)
    {
        // Orchestration logic here - business rules spanning domains
        await _storefrontService.UpdateAvailability(
            productId: context.Message.ProductId,  // Guid correlation
            isAvailable: context.Message.StockQuantity > 0,
            cancellationToken: context.CancellationToken
        );
    }
}
```

**Correlation Strategy**: All domains use the same `Guid` (CorrelationId) to identify entities across boundaries.
- Catalog: `Product(Id=Guid-123, SKU="WIDGET")`
- Storefront: `SellableItem(Id=Guid-123, SKU="WIDGET", Price=$9.99)`

**Important Boundaries**:
- Domains **never directly consume** events from other domains
- All cross-domain coordination flows through ApiService consumers
- ApiService consumers **never directly access databases** - always call service layers

For detailed event choreography patterns, see `KbStore.ApiService/INTEGRATION.md`.

## Development Workflow with Claude Code

### Orchestrator Role

When working with this codebase, Claude Code operates as a **top-level orchestrator** rather than directly implementing changes. The primary responsibilities are:

1. **Guide feature implementation** - Help scope requirements, ensure proper design, coordinate work across multiple agents
2. **Delegate to specialized agents** - Use sub-agents for most file operations (implementation-engineer, quality-analyst, doc-writer, etc.)
3. **Manage workflow phases** - Ensure changes follow: scope → document → implement → test → commit
4. **Control scope creep** - Keep features focused on actual requirements, prevent feature explosion
5. **Ensure completeness** - Verify changes are adequately captured, documented, tested, and committed via Git

### Delegation Strategy

**When to delegate (most operations):**
- Implementing new features or substantial changes → `implementation-engineer`
- Reviewing code quality and testing → `quality-analyst`
- Writing documentation → `doc-writer`
- Exploring codebase structure → `Explore` agent
- Architectural planning → `architect-planner`
- Bulk file operations → `task-gopher`

**When to handle directly (trivial operations):**
- Small edits (adding a few lines, fixing typos)
- Quick context gathering (reading 1-2 files for decision-making)
- Git operations (status, diff, commit, push)
- Running test/build commands to verify status
- Simple grep/glob searches for quick lookups

**Use parallel agent invocation when possible** - if tasks are independent, launch multiple agents in a single message to maximize efficiency.

### Natural Thresholds

The exact boundary between "trivial enough to handle directly" and "delegate to agent" will be discovered through practice. When in doubt, delegate to maintain the orchestrator role and leverage specialized agents.

## Build, Test, and Development Commands

### Build
```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build KbStore.Catalog
```

### Test
```bash
# Run all tests
dotnet test

# Run tests for specific project
dotnet test KbStore.Catalog.Tests

# Run specific test by filter
dotnet test --filter "FullyQualifiedName~Product_Create"

# List all tests without running
dotnet test --list-tests
```

### Database Migrations

Migrations are managed per domain using EF Core:

```bash
# Add new migration for Catalog domain
dotnet ef migrations add <MigrationName> --project KbStore.Catalog --context ApplicationDbContext

# Apply migrations (or use app startup migration via RunMigrationsAsync())
dotnet ef database update --project KbStore.Catalog --context ApplicationDbContext
```

**Important:** The Catalog domain runs migrations automatically on startup via `app.RunMigrationsAsync()` in `Program.cs`.

### Running Services Independently

```bash
# Run Catalog domain service
dotnet run --project KbStore.Catalog

# Run API service
dotnet run --project KbStore.ApiService

# Run Storefront domain service
dotnet run --project KbStore.Storefront
```

**Note:** Services require infrastructure (RabbitMQ, PostgreSQL, MongoDB) to be running. Use Aspire orchestration for local development.

## Testing Infrastructure

Tests use `EventingTestBase` from `KbStore.Tests`, which provides:
- MassTransit Test Harness for testing state machines
- In-memory message transport
- Arrange-Act-Assert structure via abstract methods:
  - `Arrange()`: Setup test dependencies and state
  - `Act()`: Execute the operation under test
  - Test methods contain assertions

**Test structure example:**
```csharp
[TestFixture]
public class Product_Create : CommandService_Tests<IProductCommandService>
{
    protected override void Arrange()
    {
        base.Arrange();
        // Setup test data
    }

    protected override async Task Act()
    {
        await Subject.CreateAsync(...);
    }

    [Test]
    public void Should_create_product()
    {
        // Assertions
    }
}
```

Tests are organized by domain entity and operation:
- `KbStore.Catalog.Tests/Domains/Products/Product_*.cs`
- `KbStore.Catalog.Tests/Domains/Inventory/Inventory_*.cs`

## Project Dependencies

The solution uses **Central Package Management** via `Directory.Packages.props`:

**Key dependencies:**
- **.NET Aspire 9.4.1**: Orchestration and service defaults
- **MassTransit 8.5.2**: Message bus and saga state machines
- **Hangfire 1.8.21**: Background job processing
- **Entity Framework Core 9.0.8**: ORM for PostgreSQL
- **NUnit 4.4.0**: Test framework
- **NSubstitute 5.3.0**: Mocking framework

To update package versions, modify `Directory.Packages.props`.

## API Endpoints

API endpoints are defined in `KbStore.ApiService/Endpoints/` using minimal APIs:
- `Catalog/ProductEndpoints.cs`: Product CRUD operations
- `Catalog/InventoryEndpoints.cs`: Inventory operations

Endpoints delegate to service layer implementations from `KbStore.Catalog.Services`.

**API Documentation:** The API uses Scalar for OpenAPI documentation, available when running the API service.

## Code Conventions

- **State Machines**: Use `During()` blocks to define behavior per state
- **Events**: Configure correlation using `CorrelateById()` or `CorrelateBy()`
- **Optimistic Concurrency**: Enabled on all saga entities via `RowVersion` (PostgreSQL `xid` type)
- **Exception Handling**: Domain-specific exceptions in `*.Abstractions/Exceptions/`
- **Contracts**: Request/response/event messages in `*.Abstractions/Contracts/`
- **Async/Await**: All service methods and state machine actions are async
- **Test Naming**: `{Entity}_{Operation}.cs` for test files

## Common Patterns

### Sending Commands to State Machines

Services use MassTransit's request client:
```csharp
var response = await requestClient.GetResponse<TResponse>(new TRequest { ... });
return response.Message;
```

### Inter-Domain Communication

State machines publish events that other domains consume:
- Product publishes `ProductCreated`, `ProductAvailabilityChanged`, etc.
- Inventory publishes `InventoryQuantityChanged`, `InventoryDiscontinued`, etc.
- Domains subscribe to relevant events via MassTransit consumers

### Adding New Commands to Existing State Machines

1. Define request/response contracts in `*.Abstractions/Contracts/`
2. Add event to state machine class: `public Event<TRequest> EventName { get; }`
3. Configure event correlation in constructor: `Event(() => EventName, ConfigureEvent)`
4. Add transition logic in appropriate `During()` block
5. Update service interface and implementation
6. Add tests following `EventingTestBase` pattern

## Configuration

- **Connection Strings**: Injected by Aspire orchestration
- **Service Defaults**: `KbStore.ServiceDefaults` configures telemetry, health checks, and service discovery
- **MassTransit**: Configured per domain in `Extensions/HostBuilderExtensions.cs`

## Git Workflow

- **Main Branch**: `main`
- **Development Branch**: `develop`
- **Feature Branches**: Create from `develop` for substantial changes
- Use meaningful commit messages describing the change

### When to Create a Feature Branch

**Always create a feature branch for:**
- New domain implementations (entire vertical stacks)
- Multi-file refactorings affecting multiple projects
- Breaking changes or architectural modifications
- Any work that requires multiple commits to complete
- Features that may need review before merging

**Single commit directly to develop is acceptable for:**
- Bug fixes isolated to 1-2 files
- Documentation updates
- Minor refactorings within a single file
- Configuration changes

**Naming Convention:**
- Feature: `feature/short-description`
- Bug fix: `fix/short-description`
- Documentation: `docs/short-description`

**Example:** `feature/storefront-services-implementation`
