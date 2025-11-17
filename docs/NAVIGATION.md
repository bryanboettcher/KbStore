# Navigation Guide

This document provides a comprehensive map of the KbStore codebase, organized by functionality and developer task.

## Quick Reference Map

### By Feature Area

| Feature | State Machine | Service | API Endpoint | Tests |
|---------|--------------|---------|--------------|-------|
| **Products** | `KbStore.Catalog/Domains/Products/ProductStateMachine.cs` | `KbStore.Catalog.Services/MassTransitProductCommandService.cs` | `KbStore.ApiService/Endpoints/Catalog/ProductEndpoints.cs` | `KbStore.Catalog.Tests/Domains/Products/` |
| **Inventory** | `KbStore.Catalog/Domains/Inventory/InventoryStateMachine.cs` | `KbStore.Catalog.Services/MassTransitInventoryCommandService.cs` | `KbStore.ApiService/Endpoints/Catalog/InventoryEndpoints.cs` | `KbStore.Catalog.Tests/Domains/Inventory/` |
| **Storefront** | NOT IMPLEMENTED | NOT IMPLEMENTED | NOT IMPLEMENTED | NOT IMPLEMENTED |

### By Project Type

| Project | Purpose | Status |
|---------|---------|--------|
| **KbStore.AppHost** | Aspire orchestration - starts all services | IMPLEMENTED |
| **KbStore.ServiceDefaults** | Shared configuration (telemetry, health checks) | IMPLEMENTED |
| **KbStore.Abstractions** | Shared base contracts across all domains | IMPLEMENTED |
| **KbStore.Tests** | Shared test infrastructure (EventingTestBase) | IMPLEMENTED |
| **KbStore.Catalog** | Catalog domain - state machines | IMPLEMENTED |
| **KbStore.Catalog.Abstractions** | Catalog contracts and exceptions | IMPLEMENTED |
| **KbStore.Catalog.Services** | Catalog service layer | IMPLEMENTED |
| **KbStore.Catalog.Tests** | Catalog domain tests | IMPLEMENTED |
| **KbStore.Storefront** | Storefront domain - infrastructure only | PLACEHOLDER |
| **KbStore.Storefront.Abstractions** | Storefront contracts | PLACEHOLDER (empty) |
| **KbStore.Storefront.Tests** | Storefront tests | PLACEHOLDER (empty) |
| **KbStore.ApiService** | HTTP gateway + orchestration | PARTIAL |
| **KbStore.ApiService.Tests** | API integration tests | PARTIAL |

## Project Structure

```
KbStore/
├── KbStore.AppHost/                    # Aspire orchestration entry point
│   └── Program.cs                      # Configures RabbitMQ, PostgreSQL, MongoDB
│
├── KbStore.ServiceDefaults/            # Shared service configuration
│   └── Extensions.cs                   # Health checks, telemetry, service discovery
│
├── KbStore.Abstractions/               # Shared base contracts
│   └── Contracts/                      # Base command/query/event interfaces
│
├── KbStore.Tests/                      # Shared test infrastructure
│   ├── EventingTestBase.cs            # Base class for state machine tests
│   └── CommandService_Tests.cs        # Base class for service layer tests
│
├── KbStore.Catalog/                    # Catalog domain (IMPLEMENTED)
│   ├── Domains/
│   │   ├── Products/
│   │   │   ├── ProductEntity.cs        # State machine entity
│   │   │   ├── ProductStateMachine.cs  # State machine logic
│   │   │   └── ProductSagaMap.cs       # EF Core mapping
│   │   └── Inventory/
│   │       ├── InventoryEntity.cs      # State machine entity
│   │       ├── InventoryStateMachine.cs # State machine logic
│   │       └── InventorySagaMap.cs     # EF Core mapping
│   ├── Infrastructure/
│   │   └── ApplicationDbContext.cs     # EF Core DbContext
│   ├── Migrations/                     # EF Core migrations
│   ├── Extensions/
│   │   └── HostBuilderExtensions.cs    # MassTransit + EF configuration
│   └── Program.cs                      # Domain service entry point
│
├── KbStore.Catalog.Abstractions/       # Catalog contracts (IMPLEMENTED)
│   ├── Contracts/
│   │   ├── Product/                    # Product commands/responses/events
│   │   └── Inventory/                  # Inventory commands/responses/events
│   ├── Exceptions/                     # Domain-specific exceptions
│   └── Services/                       # Service interfaces
│
├── KbStore.Catalog.Services/           # Catalog service layer (IMPLEMENTED)
│   ├── MassTransitProductCommandService.cs    # Product commands
│   ├── DbContextProductQueryService.cs        # Product queries
│   ├── MassTransitInventoryCommandService.cs  # Inventory commands
│   ├── DbContextInventoryQueryService.cs      # Inventory queries
│   └── Extensions/
│       ├── ServiceCollectionExtensions.cs     # DI registration
│       ├── ProductRequestFaultExceptionExtensions.cs   # Exception conversion
│       └── InventoryRequestFaultExceptionExtensions.cs # Exception conversion
│
├── KbStore.Catalog.Tests/              # Catalog tests (IMPLEMENTED)
│   └── Domains/
│       ├── Products/
│       │   ├── Product_Create.cs       # Product creation tests
│       │   ├── Product_Update*.cs      # Product update tests
│       │   └── Product_Delete.cs       # Product deletion tests
│       └── Inventory/
│           ├── Inventory_Create.cs     # Inventory creation tests
│           └── Inventory_*.cs          # Other inventory operation tests
│
├── KbStore.Storefront/                 # Storefront domain (PLACEHOLDER)
│   ├── Extensions/
│   │   └── HostBuilderExtensions.cs    # MongoDB + MassTransit config
│   ├── Program.cs                      # Domain service entry point
│   └── ARCHITECTURE.md                 # Design documentation
│
├── KbStore.Storefront.Abstractions/    # Storefront contracts (EMPTY)
├── KbStore.Storefront.Tests/           # Storefront tests (EMPTY)
│
├── KbStore.ApiService/                 # HTTP gateway + orchestration (PARTIAL)
│   ├── Endpoints/
│   │   ├── Catalog/
│   │   │   ├── ProductEndpoints.cs     # Product HTTP endpoints
│   │   │   └── InventoryEndpoints.cs   # Inventory HTTP endpoints
│   │   ├── ResponseExtensions.cs       # HTTP response helpers
│   │   └── WebApplicationExtensions.cs # Endpoint registration
│   ├── Consumers/
│   │   └── Catalog/
│   │       └── ProductAvailabilityConsumer.cs  # PLACEHOLDER (empty)
│   ├── Extensions/
│   │   └── HostBuilderExtensions.cs    # MassTransit consumer registration
│   ├── Program.cs                      # API service entry point
│   ├── INTEGRATION.md                  # Cross-domain orchestration plans
│   └── ARCHITECTURE.md                 # API service design
│
├── KbStore.ApiService.Tests/           # API tests (MINIMAL)
│
├── docs/                               # Documentation
│   ├── NAVIGATION.md                   # This file
│   ├── PATTERNS.md                     # Implementation patterns
│   ├── DATAFLOWS.md                    # Data flow diagrams
│   ├── adr/                            # Architecture Decision Records
│   └── requirements/                   # Business requirements
│
├── CLAUDE.md                           # Claude Code instructions
├── Directory.Packages.props            # Central package management
└── KbStore.sln                         # Solution file
```

## Key Files by Developer Task

### Starting the Application

**File**: `KbStore.AppHost/Program.cs`

```bash
# Run from solution root
dotnet run --project KbStore.AppHost
```

This starts:
- RabbitMQ (with management UI at http://localhost:15672)
- PostgreSQL (with PgWeb UI at http://localhost:5050)
- MongoDB (with MongoExpress UI)
- Catalog domain service
- Storefront domain service (infrastructure only, no functionality)
- ApiService (HTTP gateway)

### Adding a New State Machine Command

**Files to modify**:
1. `KbStore.Catalog.Abstractions/Contracts/{Entity}/{Command}Request.cs` - Define request
2. `KbStore.Catalog.Abstractions/Contracts/{Entity}/{Command}Response.cs` - Define response
3. `KbStore.Catalog/Domains/{Entity}/{Entity}StateMachine.cs` - Add event and transition
4. `KbStore.Catalog.Abstractions/Services/I{Entity}CommandService.cs` - Add interface method
5. `KbStore.Catalog.Services/MassTransit{Entity}CommandService.cs` - Implement method
6. `KbStore.Catalog.Tests/Domains/{Entity}/{Entity}_{Command}.cs` - Add tests

**Example**: See `ProductStateMachine.cs` lines 29-36 for event definitions, lines 89-94 for transition logic.

### Creating a New HTTP Endpoint

**Files to modify**:
1. `KbStore.ApiService/Endpoints/Catalog/{Entity}Endpoints.cs` - Add endpoint method
2. `KbStore.ApiService/Endpoints/Catalog/{Entity}Endpoints.cs` (MapTo method) - Register route

**Example**: See `ProductEndpoints.cs` lines 14-34 for endpoint method, lines 195-219 for route registration.

### Understanding State Machine Behavior

**Primary files**:
- `KbStore.Catalog/Domains/Products/ProductStateMachine.cs` (lines 44-262) - Full product lifecycle
- `KbStore.Catalog/Domains/Inventory/InventoryStateMachine.cs` (lines 33-182) - Full inventory lifecycle

**Key patterns**:
- `Initially()` - Handles initial creation events
- `During(State)` - Defines behavior while in specific state
- `DuringAny()` - Handles events in any state
- `TransitionTo()` - Changes state
- `PublishAsync()` - Publishes domain events
- `RespondAsync()` - Sends response to request

### Debugging Test Failures

**Test infrastructure**: `KbStore.Tests/EventingTestBase.cs`

**Test base classes**:
- `StateMachine_Tests<TStateMachine, TEntity>` - For direct state machine testing
- `CommandService_Tests<TService>` - For service layer testing

**Pattern**: Tests use Arrange-Act-Assert
```csharp
protected override void Arrange() { /* Setup */ }
protected override async Task Act() { /* Execute */ }
[Test] public void Should_* { /* Assert */ }
```

**Example**: `KbStore.Catalog.Tests/Domains/Inventory/Inventory_Create.cs`

### Tracking Database Changes

**Catalog domain**:
- **Context**: `KbStore.Catalog/Infrastructure/ApplicationDbContext.cs`
- **Migrations**: `KbStore.Catalog/Migrations/`
- **Running migrations**: Automatic on startup via `app.RunMigrationsAsync()` in `Program.cs`

**Adding migration**:
```bash
dotnet ef migrations add <MigrationName> --project KbStore.Catalog --context ApplicationDbContext
```

### Understanding Message Routing

**MassTransit configuration**:
- **Catalog domain**: `KbStore.Catalog/Extensions/HostBuilderExtensions.cs`
- **ApiService consumers**: `KbStore.ApiService/Extensions/HostBuilderExtensions.cs`

**Key concepts**:
- State machines auto-register request handlers
- Consumers are explicitly registered via `AddConsumer<T>()`
- Events are published to RabbitMQ topic exchanges
- Requests use request/response pattern with timeout handling

### Viewing Published Events

**Tools**:
1. **RabbitMQ Management UI**: http://localhost:15672 (guest/guest)
   - Navigate to Exchanges → Find `KbStore.Catalog.Abstractions:ProductAvailabilityChanged`
   - View bindings and message rates

2. **Code locations**:
   - Product events: `ProductStateMachine.cs` - search for `.PublishAsync()`
   - Inventory events: `InventoryStateMachine.cs` - search for `.PublishAsync()`

### Finding Contract Definitions

**All contracts follow naming convention**: `{Entity}{Operation}{Type}.cs`

**Locations**:
- **Commands (requests)**: `KbStore.Catalog.Abstractions/Contracts/{Entity}/{Command}Request.cs`
- **Responses**: `KbStore.Catalog.Abstractions/Contracts/{Entity}/{Command}Response.cs`
- **Events**: `KbStore.Catalog.Abstractions/Contracts/{Entity}/{Event}.cs`
- **Models (read)**: `KbStore.Catalog.Abstractions/Contracts/{Entity}/{Entity}Model.cs`

**Example**: `CreateProductRequest`, `CreateProductResponse`, `ProductCreated`, `ProductModel`

### Understanding Exception Handling

**Exception definitions**: `KbStore.Catalog.Abstractions/Exceptions/`
- `ProductNotFoundException.cs`
- `ProductStateException.cs`
- `ProductValidationException.cs`
- `ProductConflictException.cs`
- `InventoryNotFoundException.cs`
- `InventoryStateException.cs`
- `InventoryValidationException.cs`

**Exception conversion** (MassTransit faults → domain exceptions):
- `KbStore.Catalog.Services/Extensions/ProductRequestFaultExceptionExtensions.cs`
- `KbStore.Catalog.Services/Extensions/InventoryRequestFaultExceptionExtensions.cs`

**Pattern**: Service layer catches `RequestFaultException` and converts via extension methods

### Cross-Domain Event Flow (Planned)

**Current state**: Events are published but NO consumers exist yet.

**Published events** (from Catalog domain):
- `ProductCreated`, `ProductAvailabilityChanged`, `ProductDiscontinued`, etc.
- `InventoryQuantityChanged`, `InventoryDiscontinued`, etc.

**Planned consumers** (in ApiService):
- `KbStore.ApiService/Consumers/Catalog/ProductAvailabilityConsumer.cs` - Currently empty placeholder

**See**: `KbStore.ApiService/INTEGRATION.md` for detailed orchestration plans

## Configuration Files

| File | Purpose |
|------|---------|
| `Directory.Packages.props` | Central package version management |
| `KbStore.AppHost/appsettings.json` | Aspire orchestration settings |
| `KbStore.Catalog/appsettings.json` | Catalog domain configuration |
| `KbStore.ApiService/appsettings.json` | API service configuration |
| `KbStore.sln` | Solution structure |

## Common Search Patterns

### Find all state machines
```bash
# Using glob
**/*StateMachine.cs
```

### Find all command services
```bash
# Using glob
**/*CommandService.cs
```

### Find all HTTP endpoints
```bash
# Using glob
**/Endpoints/**/*.cs
```

### Find all test files for a specific entity
```bash
# Using glob
**/{Entity}_*.cs
```

### Find where an event is published
```bash
# Using grep
grep -r "PublishAsync.*ProductCreated" --include="*.cs"
```

### Find where an event is consumed
```bash
# Using grep
grep -r "IConsumer<ProductCreated>" --include="*.cs"
# (Currently returns no results - no consumers implemented yet)
```

## Dependency Graph

```
KbStore.ApiService
    ├── depends on → KbStore.ServiceDefaults
    ├── depends on → KbStore.Catalog.Services
    ├── depends on → KbStore.Catalog.Abstractions
    └── depends on → KbStore.Storefront.Services (future)

KbStore.Catalog
    ├── depends on → KbStore.ServiceDefaults
    └── depends on → KbStore.Catalog.Abstractions

KbStore.Catalog.Services
    └── depends on → KbStore.Catalog.Abstractions

KbStore.Catalog.Tests
    ├── depends on → KbStore.Tests
    ├── depends on → KbStore.Catalog
    └── depends on → KbStore.Catalog.Abstractions

KbStore.Storefront (future implementation)
    ├── depends on → KbStore.ServiceDefaults
    └── depends on → KbStore.Storefront.Abstractions

KbStore.Tests
    └── depends on → KbStore.Abstractions

KbStore.AppHost
    └── orchestrates → all service projects
```

## Related Documentation

- **CLAUDE.md** - Claude Code instructions and architectural overview
- **docs/PATTERNS.md** - Implementation patterns and templates
- **docs/DATAFLOWS.md** - Data flow and lifecycle diagrams
- **KbStore.ApiService/INTEGRATION.md** - Cross-domain orchestration patterns
- **KbStore.Storefront/ARCHITECTURE.md** - Storefront domain design
- **KbStore.Catalog/ARCHITECTURE.md** - Catalog domain design (if exists)
- **docs/adr/** - Architecture Decision Records
