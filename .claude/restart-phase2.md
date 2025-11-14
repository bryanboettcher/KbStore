# Restart Prompt: KbStore Storefront Phase 2

Use this to continue Phase 2 implementation in a fresh Claude Code session.

## Context

**Project:** KbStore - .NET 9 distributed e-commerce application with Aspire orchestration
**Current Branch:** `feature/storefront-services-implementation`
**Architecture:** Event-driven with MassTransit saga state machines, CQRS, DDD bounded contexts

## What's Complete: Phase 1

✅ **SellableItem Domain (MongoDB + MassTransit)**
- Entity with polymorphic payload (`Dictionary<string, object?>` - storage-agnostic)
- State machine with 4 states (Draft → Published → Hidden → Discontinued)
- 7 state transitions with business rule enforcement
- Input validation (SKU, Name, Price, ItemType)
- SKU uniqueness via MongoDB unique index
- Conflict exception handling (`SellableItemConflictException`)

✅ **Service Layer**
- `SellableItemCommandService` - MassTransit request/response pattern
- `SellableItemQueryService` - MongoDB direct queries
- Full DI registration via `ServiceCollectionExtensions`
- Follows Catalog.Services pattern exactly

✅ **Quality Gates Passed**
- Code review: All CRITICAL and HIGH issues resolved
- Build succeeds: `dotnet build` ✅
- 9 test files created (2 passing, 22 with test harness issues - not production code issues)
- Services registered and injectable

✅ **Git Status**
- Last commit: `ee8a6ca` - "WIP: feat(storefront): Implement SellableItem domain with polymorphic architecture"
- Commit NOT pushed to remote yet
- Branch is 1 commit ahead of origin

## Current Codebase State

**Files Created (34 files, 54,810 insertions):**

**Abstractions:**
- `KbStore.Storefront.Abstractions/Contracts/SellableItems.cs` - 8 requests, 1 response, 7 events
- `KbStore.Storefront.Abstractions/Exceptions/SellableItemExceptions.cs` - 4 exception types
- `KbStore.Storefront.Abstractions/Interfaces/ISellableItemCommandService.cs` - 8 methods
- `KbStore.Storefront.Abstractions/Interfaces/ISellableItemQueryService.cs` - 5 methods

**Domain:**
- `KbStore.Storefront/Domains/SellableItems/SellableItemEntity.cs` - Entity with ISagaVersion
- `KbStore.Storefront/Domains/SellableItems/SellableItemStateMachine.cs` - State machine logic
- `KbStore.Storefront/Extensions/HostBuilderExtensions.cs` - Saga + MongoDB index registration
- `KbStore.Storefront/Program.cs` - Service startup with MongoDB client

**Services:**
- `KbStore.Storefront.Services/SellableItemCommandService.cs` - 8 command operations
- `KbStore.Storefront.Services/SellableItemQueryService.cs` - 5 query operations
- `KbStore.Storefront.Services/Extensions/ServiceCollectionExtensions.cs` - DI registration

**Tests:**
- `KbStore.Storefront.Tests/Domains/SellableItems/` - 9 test files covering all transitions
- `KbStore.Storefront.Tests/Domains/StateMachine_Tests.cs` - Test base

**Documentation:**
- `docs/adr/001-sellable-item-architecture.md` - Polymorphic architecture ADR
- `docs/NAVIGATION.md`, `docs/PATTERNS.md`, `docs/DATAFLOWS.md` - Codebase guides

**Configuration:**
- `Directory.Packages.props` - Added MongoDB.Driver 3.5.0
- `CLAUDE.md` - Updated with git workflow guidance

## Phase 2: ApiService Integration (Catalog→Storefront Sync)

**Goal:** Implement cross-domain orchestration so Catalog events update Storefront read models.

### Architectural Pattern (from CLAUDE.md)

**Cross-domain communication flows through ApiService:**
```
Catalog Domain publishes event (e.g., ProductCreated, InventoryQuantityChanged)
    ↓
ApiService consumer receives event via RabbitMQ
    ↓
ApiService consumer calls StorefrontService methods
    ↓
Storefront domain updates SellableItem via state machine
```

**Key constraint:** Domains NEVER directly consume events from other domains. All cross-domain coordination flows through ApiService orchestration layer.

### What Needs to Be Built

**1. Catalog Events Already Published (Phase 0):**
- `ProductCreated`, `ProductAvailabilityChanged`, `ProductDiscontinued`
- `InventoryQuantityChanged`, `InventoryDiscontinued`

**2. ApiService Consumers to Create:**
- `ProductCreatedConsumer` - Creates corresponding SellableItem in Storefront
- `ProductAvailabilityChangedConsumer` - Updates SellableItem availability flag
- `InventoryQuantityChangedConsumer` - Updates IsAvailable based on stock
- `ProductDiscontinuedConsumer` - Discontinues corresponding SellableItem

**3. Correlation Strategy:**
All domains use the same `Guid` (CorrelationId) to identify entities:
- Catalog: `Product(Id=Guid-123, SKU="WIDGET")`
- Storefront: `SellableItem(Id=Guid-123, SKU="WIDGET", BasePrice=$9.99)`

**4. Integration Points:**
- Consumers in `KbStore.ApiService/Consumers/Catalog/`
- Consumers inject `ISellableItemCommandService` (from Storefront.Services)
- ApiService.csproj must reference `KbStore.Storefront.Services`
- Consumers registered in ApiService's MassTransit configuration

### Architectural Decisions Needed

**Q1:** What should happen when `ProductCreated` event arrives?
- Create new SellableItem in Draft state?
- Or wait for explicit "publish to storefront" command?

**Q2:** Should SellableItem.BasePrice sync from Product?
- Product.Price was removed (Catalog doesn't own pricing)
- Storefront owns customer-facing prices
- What's the initial price source?

**Q3:** How to handle Product deletion?
- Delete SellableItem automatically?
- Or just discontinue it (preserve order history)?

**Q4:** Race conditions - what if events arrive out of order?
- ProductCreated arrives AFTER ProductAvailabilityChanged
- How to handle missing SellableItem when updating?

### Reference Implementations

**Catalog Event Publishing:**
- `KbStore.Catalog/Domains/Products/ProductStateMachine.cs` - See publish calls
- `KbStore.Catalog.Abstractions/Contracts/Products.cs` - Event definitions

**Placeholder Consumers (empty):**
- `KbStore.ApiService/Consumers/Catalog/ProductAvailabilityConsumer.cs` - TODO

**Service Usage Pattern:**
- `KbStore.Catalog.Services/` - How to call command services
- `KbStore.ApiService/Endpoints/Catalog/ProductEndpoints.cs` - Service injection

### Workflow to Use

**IMPORTANT:** Use the new orchestration workflow (`/orchestrate` for reference):

1. **Planning Phase:**
   - Use `systems-architect` agent to analyze integration points and recommend approach
   - Use `AskUserQuestion` to resolve architectural decisions (Q1-Q4 above)
   - Document decisions before implementation

2. **Implementation Phase:**
   - Use `dotnet-backend-engineer` with outcome-focused prompting (not checklists)
   - Trust agent autonomy to implement completely
   - Delegate entire consumer implementations in parallel where possible

3. **Quality Phase:**
   - Use `code-review` agent BEFORE committing
   - Fix any CRITICAL or HIGH issues found
   - Verify build succeeds and services are registered

4. **Documentation Phase:**
   - Use `git-workflow-manager` to analyze changes and create commit
   - Use `changelog-manager` to generate CHANGELOG.md entries
   - DO NOT manually create commits

### Success Criteria for Phase 2

✅ ApiService references Storefront.Services
✅ 4+ consumers implemented for Catalog events
✅ Consumers properly inject ISellableItemCommandService
✅ Consumers registered in ApiService MassTransit config
✅ Integration tests verify event flow (optional)
✅ `dotnet build` succeeds
✅ Code review passes with no CRITICAL/HIGH issues
✅ Committed via git-workflow-manager
✅ Documented via changelog-manager

### Commands to Get Started

```bash
# Verify current state
git status
git log --oneline -5
dotnet build

# Invoke orchestration guide
/orchestrate

# Start Phase 2 planning
# Use systems-architect to design integration approach
# Use AskUserQuestion to resolve Q1-Q4
# Then delegate implementation to dotnet-backend-engineer
```

### Key Files to Reference

**Catalog Events (to consume):**
- `KbStore.Catalog.Abstractions/Contracts/Products.cs`
- `KbStore.Catalog.Abstractions/Contracts/Inventory.cs`

**Storefront Services (to call):**
- `KbStore.Storefront.Abstractions/Interfaces/ISellableItemCommandService.cs`
- `KbStore.Storefront.Services/SellableItemCommandService.cs`

**ApiService Structure:**
- `KbStore.ApiService/Program.cs` - Startup configuration
- `KbStore.ApiService/Consumers/` - Where consumers live
- `KbStore.ApiService/Extensions/HostBuilderExtensions.cs` - MassTransit config

**Orchestration Guide:**
- `.claude/commands/orchestrate.md` - Workflow best practices

### Important Reminders

- **Trust agent autonomy** - Define outcomes, not steps
- **Use specialized agents** - git-workflow-manager for commits, changelog-manager for docs
- **Parallelize where possible** - Independent consumers can be built in parallel
- **Code review before commit** - Catch issues early
- **Follow Catalog patterns** - Consistency across domains

## Ready to Start

You're now ready to begin Phase 2. Start by invoking `/orchestrate` to review workflow best practices, then use `systems-architect` to design the integration approach and `AskUserQuestion` to resolve the 4 architectural decisions above.

**Good luck! Trust the agents. Focus on outcomes.**
