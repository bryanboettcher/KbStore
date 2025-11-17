# KbStore Quick Reference

**Last Updated**: 2025-11-14

## Project Root
```
/mnt/c/users/bryan/source/bryanboettcher/KbStore
```

## Critical Commands

### Build & Run
```bash
# Build entire solution
dotnet build /mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.sln

# Run with Aspire orchestration (recommended)
dotnet run --project /mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.AppHost

# Run individual services
dotnet run --project /mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Catalog
dotnet run --project /mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.ApiService
dotnet run --project /mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Storefront
```

### Testing
```bash
# Run all tests
dotnet test /mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.sln

# Run specific domain tests
dotnet test /mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Catalog.Tests
dotnet test /mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.ApiService.Tests

# Run with filter
dotnet test --filter "FullyQualifiedName~Product_Create"
dotnet test --filter "FullyQualifiedName~Inventory"

# List all tests
dotnet test --list-tests
```

### Database Migrations (Catalog)
```bash
# Add migration
cd /mnt/c/users/bryan/source/bryanboettcher/KbStore
dotnet ef migrations add MigrationName --project KbStore.Catalog --context ApplicationDbContext

# Apply migrations (auto-runs on startup via app.RunMigrationsAsync())
dotnet ef database update --project KbStore.Catalog --context ApplicationDbContext
```

## Project Structure Overview

```
KbStore/
├── KbStore.AppHost/                    # Aspire orchestration
├── KbStore.ServiceDefaults/            # Shared service configuration
├── KbStore.ApiService/                 # HTTP gateway + orchestration
│   ├── Endpoints/Catalog/              # Product & Inventory HTTP endpoints
│   └── Consumers/Catalog/              # Cross-domain event consumers
├── KbStore.Abstractions/               # Shared abstractions
├── KbStore.Tests/                      # Shared test infrastructure
│
├── [CATALOG DOMAIN - FULLY IMPLEMENTED]
├── KbStore.Catalog/                    # Pure domain (PostgreSQL)
│   ├── Domains/Products/               # Product state machine + entity
│   ├── Domains/Inventory/              # Inventory state machine + entity
│   └── Persistence/                    # EF Core DbContext + migrations
├── KbStore.Catalog.Abstractions/       # Contracts, interfaces, exceptions
│   ├── Contracts/                      # Request/response/event messages
│   ├── Exceptions/                     # Domain-specific exceptions
│   └── Services/                       # Service interfaces
├── KbStore.Catalog.Services/           # Service implementations
│   ├── MassTransitProductCommandService.cs
│   ├── MassTransitInventoryCommandService.cs
│   ├── DbContextProductQueryService.cs
│   └── DbContextInventoryQueryService.cs
├── KbStore.Catalog.Tests/              # Domain tests
│   └── Domains/                        # Product & Inventory test suites
│
├── [STOREFRONT DOMAIN - PARTIALLY IMPLEMENTED]
├── KbStore.Storefront/                 # Domain infrastructure (MongoDB)
│   └── Domains/SellableItems/          # SellableItem state machine (EXISTS)
├── KbStore.Storefront.Abstractions/    # Contracts (MOSTLY COMPLETE)
├── KbStore.Storefront.Services/        # Service layer (EXISTS)
└── KbStore.Storefront.Tests/           # Tests (MINIMAL)
```

## Key File Locations

### State Machines (Core Domain Logic)
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Catalog/Domains/Products/ProductStateMachine.cs`
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Catalog/Domains/Inventory/InventoryStateMachine.cs`
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Storefront/Domains/SellableItems/SellableItemStateMachine.cs`

### Service Interfaces
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Catalog.Abstractions/Services/IProductCommandService.cs`
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Catalog.Abstractions/Services/IInventoryCommandService.cs`
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Storefront.Abstractions/Interfaces/ISellableItemCommandService.cs`

### API Endpoints
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.ApiService/Endpoints/Catalog/ProductEndpoints.cs`
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.ApiService/Endpoints/Catalog/InventoryEndpoints.cs`

### Event Consumers (Orchestration)
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.ApiService/Consumers/Catalog/ProductCreatedConsumer.cs`
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.ApiService/Consumers/Catalog/ProductNameUpdatedConsumer.cs`
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.ApiService/Consumers/Catalog/ProductDiscontinuedConsumer.cs`

### Configuration & Startup
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.AppHost/Program.cs` - Aspire orchestration
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Catalog/Program.cs` - Catalog domain startup
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Storefront/Program.cs` - Storefront domain startup
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.ApiService/Program.cs` - API service startup

### MassTransit Configuration
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Catalog/Extensions/HostBuilderExtensions.cs`

### Test Infrastructure
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Tests/EventingTestBase.cs`
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Catalog.Tests/Domains/Products/Product_Create.cs` (example)

### Documentation
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/CLAUDE.md` - Primary developer guide
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/docs/PATTERNS.md` - Implementation patterns
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/docs/NAVIGATION.md` - Codebase navigation
- `/mnt/c/users/bryan/source/bryanboettcher/KbStore/docs/DATAFLOWS.md` - Data flow diagrams

## Technology Stack

### Framework & Runtime
- **.NET 9.0** - Core framework
- **C# 12** - Language version

### Orchestration & Infrastructure
- **Aspire 9.4.1** - Distributed app orchestration
- **RabbitMQ** - Message broker (via Aspire container)
- **PostgreSQL** - Catalog domain database (via Aspire container)
- **MongoDB** - Storefront domain database (via Aspire container)

### Messaging & State Machines
- **MassTransit 8.5.5** - Message bus & saga orchestration
  - RabbitMQ transport
  - EF Core saga persistence (PostgreSQL)
  - MongoDB saga persistence

### Data Access
- **Entity Framework Core 9.0.8** - ORM for PostgreSQL (Catalog domain)
- **Npgsql 9.0.4** - PostgreSQL provider
- **MongoDB.Driver 3.5.0** - MongoDB driver (Storefront domain)

### Background Jobs
- **Hangfire 1.8.21** - Background job processing
- **Hangfire.PostgreSql 1.20.12** - PostgreSQL storage

### API & HTTP
- **ASP.NET Core Minimal APIs** - HTTP endpoints
- **Scalar 2.6.9** - OpenAPI documentation UI
- **Microsoft.AspNetCore.OpenApi 9.0.8** - OpenAPI generation

### Testing
- **NUnit 4.4.0** - Test framework
- **MassTransit.TestFramework 8.5.5** - State machine testing
- **Shouldly 4.3.0** - Assertion library
- **NSubstitute 5.3.0** - Mocking framework

### Telemetry & Observability
- **OpenTelemetry 1.12.0** - Distributed tracing
  - AspNetCore instrumentation
  - HTTP instrumentation
  - Runtime instrumentation

## Domain Status Matrix

| Domain | Infrastructure | State Machines | Services | API Endpoints | Tests | Status |
|--------|----------------|----------------|----------|---------------|-------|--------|
| **Catalog** | PostgreSQL + EF Core | Product, Inventory | Command + Query | CRUD operations | Comprehensive | **COMPLETE** |
| **Storefront** | MongoDB | SellableItem | Partial | None | Minimal | **IN PROGRESS** |
| **ApiService** | N/A (orchestrator) | N/A | N/A | Catalog only | Partial | **PARTIAL** |

## Current Implementation Phase

**Phase**: Storefront Services Implementation (In Progress)

### What Works:
- Catalog domain fully functional (Products + Inventory)
- HTTP API for Catalog operations
- Event publishing from Catalog domain
- Cross-domain consumers in ApiService (active)

### What's Being Built:
- Storefront service layer implementations
- Storefront query operations
- Event consumer logic for ProductCreated, ProductUpdated
- Tests for Storefront services

### What's Next:
- Storefront HTTP endpoints
- Complete cross-domain orchestration
- Authentication/authorization
- Advanced querying and filtering

## Common Patterns

### Adding a New State Machine
1. Create `{Entity}Entity.cs` inheriting `SagaStateMachineInstance`
2. Create `{Entity}StateMachine.cs` inheriting `MassTransitStateMachine<{Entity}Entity>`
3. Create `{Entity}SagaMap.cs` for EF Core configuration
4. Define contracts in `*.Abstractions/Contracts/{Entity}.cs`
5. Register in `HostBuilderExtensions.AddMassTransit()`
6. Create migration: `dotnet ef migrations add Add{Entity}`
7. Create tests following `{Entity}_{Operation}.cs` pattern

### Adding a New Command to Existing State Machine
1. Define request/response in `*.Abstractions/Contracts/`
2. Add `Event<TRequest>` property to state machine
3. Configure event correlation in constructor
4. Add `When(Event)` handler in appropriate `During()` block
5. Update service interface in `*.Abstractions/Services/`
6. Implement in `MassTransit{Entity}CommandService`
7. Add API endpoint if needed
8. Write tests

### Adding a New API Endpoint
1. Create method in `{Entity}Endpoints.cs`
2. Inject `I{Entity}CommandService` or `I{Entity}QueryService`
3. Validate payload
4. Call service method
5. Return `IResult` (Ok, BadRequest, NotFound, etc.)
6. Register route in `MapTo()` method
7. Add `[ProducesResponseType]` attributes
8. Write integration tests

## Troubleshooting Quick Checks

### Build Failures
```bash
# Clean solution
dotnet clean /mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.sln

# Restore packages
dotnet restore /mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.sln

# Rebuild
dotnet build /mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.sln
```

### Test Failures
- Check MassTransit test harness timeout settings in `EventingTestBase`
- Verify saga state using `SagaHarness.Exists()` and `.Sagas.Contains()`
- Check event publishing with `Harness.Published.Any<TEvent>()`
- Look for `LastException` in test assertions

### State Machine Issues
- Verify event correlation is configured
- Check `OnMissingInstance()` handlers
- Ensure `RowVersion` is configured for optimistic concurrency
- Verify state transitions use `TransitionTo()`
- Check that events publish AFTER state changes

### Database Issues
- Verify connection strings in Aspire orchestration
- Check migrations are up to date
- Ensure `app.RunMigrationsAsync()` is called in Catalog Program.cs
- For PostgreSQL: Check PgWeb UI on port 5050
- For MongoDB: Check MongoExpress UI

### MassTransit Issues
- Check RabbitMQ is running via Aspire
- Verify consumer registration in `HostBuilderExtensions`
- Check message correlation IDs match across domains
- Look for `RequestFaultException` and convert to domain exceptions
- Verify request timeout settings

## Naming Conventions

### Files
- State machines: `{Entity}StateMachine.cs`
- Entities: `{Entity}Entity.cs`
- EF mappings: `{Entity}SagaMap.cs`
- Services: `MassTransit{Entity}CommandService.cs`, `DbContext{Entity}QueryService.cs`
- Endpoints: `{Entity}Endpoints.cs`
- Tests: `{Entity}_{Operation}.cs`
- Consumers: `{EventName}Consumer.cs`

### Classes
- Interfaces: `I{Entity}CommandService`, `I{Entity}QueryService`
- Exceptions: `{Entity}ValidationException`, `{Entity}NotFoundException`, etc.
- Contracts: `{Operation}{Entity}Request`, `{Operation}{Entity}Response`
- Events: `{Entity}{PastTenseVerb}` (e.g., `ProductCreated`, `InventoryQuantityChanged`)

### Namespaces
- Domain: `KbStore.{Domain}.Domains.{Entity}`
- Abstractions: `KbStore.{Domain}.Abstractions.{Category}`
- Services: `KbStore.{Domain}.Services`
- Tests: `KbStore.{Domain}.Tests.Domains.{Entity}`
- API: `KbStore.ApiService.Endpoints.{Domain}`

## Git Workflow

### Branches
- **main** - Production-ready code
- **develop** - Active development
- **feature/** - New features
- **fix/** - Bug fixes
- **docs/** - Documentation updates

### Commit Message Style
```
Add SellableItem state machine for Storefront domain

- Implement state machine with Draft, Active, Archived states
- Configure MongoDB persistence with saga repository
- Add event handlers for ProductCreated events
- Create comprehensive test suite

🤖 Generated with Claude Code
Co-Authored-By: Claude <noreply@anthropic.com>
```

## Support Resources

### Documentation
- Primary guide: `/mnt/c/users/bryan/source/bryanboettcher/KbStore/CLAUDE.md`
- Patterns: `/mnt/c/users/bryan/source/bryanboettcher/KbStore/docs/PATTERNS.md`
- Navigation: `/mnt/c/users/bryan/source/bryanboettcher/KbStore/docs/NAVIGATION.md`

### External Resources
- MassTransit docs: https://masstransit.io/documentation/concepts
- Aspire docs: https://learn.microsoft.com/en-us/dotnet/aspire/
- EF Core docs: https://learn.microsoft.com/en-us/ef/core/
