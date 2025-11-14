# Restart Prompt: KbStore Phase 2 - Correlation Strategy Fix

Use this to continue Phase 2 implementation after discovering critical correlation issue.

## Context

**Project:** KbStore - .NET 9 distributed e-commerce application with Aspire orchestration
**Current Branch:** `feature/storefront-services-implementation`
**Architecture:** Event-driven with MassTransit saga state machines, CQRS, DDD bounded contexts

## Session Summary

### What We Accomplished

**Phase 2A: Storefront ProductId Field (COMPLETE ✅)**
- Added `ProductId` field to `SellableItemEntity` (Guid?, optional)
- Updated contracts (`CreateSellableItemRequest`, `SellableItemResponse`) to include ProductId
- Updated service layer (`ISellableItemCommandService`, `SellableItemCommandService`) with productId parameter
- Updated state machine to store ProductId from request
- All Storefront projects build successfully

**Phase 2B: ApiService Orchestration Consumers (COMPLETE ✅)**
- Added `KbStore.Storefront.Services` reference to ApiService
- Registered Storefront services in DI via `AddStorefrontServices()`
- Implemented `ProductCreatedConsumer` - creates SellableItem when Product created
- Implemented `ProductNameUpdatedConsumer` - updates SellableItem name (with create-if-missing)
- Implemented `ProductDiscontinuedConsumer` - discontinues SellableItem when Product discontinued
- Removed empty `ProductAvailabilityConsumer` placeholder
- All ApiService projects build successfully

**Architectural Analysis:**
- Consulted `systems-architect` - designed event flow and consumer mapping
- Consulted `masstransit-expert` (twice):
  - Answered SelectId() optimization question (no null-based skip optimization exists)
  - Recommended separate ProductId field vs. same CorrelationId pattern
- Consulted `code-review` - found CRITICAL correlation bug

### What We Discovered - CRITICAL ISSUE

**Code review identified fundamental correlation bug:**

**Current Implementation (BROKEN):**
```csharp
// State Machine - generates NEW Guid for SellableItem
Event(() => Created, e =>
{
    e.CorrelateBy((saga, context) => saga.SKU == context.Message.SKU);
    e.SelectId(_ => NewId.NextSequentialGuid());  // NEW Guid!
    // ...
});

// Consumer - queries by ProductId (which is NOT the CorrelationId)
var existing = await _sellableItemQueryService.GetByIdAsync(msg.ProductId, ...);
if (existing != null) return;  // Never finds anything - wrong ID!
```

**Result:**
- SellableItem gets `CorrelationId = Guid-456` (auto-generated)
- ProductId is stored as a field: `ProductId = Guid-123`
- Consumer queries for `Guid-123` but entity is keyed by `Guid-456`
- Idempotency checks never work
- Duplicate SellableItems can be created on event replay

### The Root Cause Analysis

**MassTransit Expert Recommendation:**
- Use **separate ProductId field** (not same CorrelationId)
- Each saga maintains own identity
- ProductId is just a reference field for queries

**Backend Engineer Implementation:**
- Followed expert's entity design (separate field)
- But wrote consumer code as if ProductId WAS the CorrelationId
- Created hybrid that doesn't work

**User's Insight (CORRECT):**
> "GetByProductId is probably a query-only operation. Consumers should send commands with SKU correlation, not manual idempotency checks."

**The Real Solution:**
- Remove manual `GetByIdAsync` checks from consumers
- Trust MassTransit's built-in correlation via SKU
- State machine already has: `e.CorrelateBy((saga, context) => saga.SKU == context.Message.SKU)`
- MassTransit automatically:
  - Looks for existing saga by SKU
  - Routes to existing instance (idempotent!)
  - Creates new instance if not found

## Current Codebase State

**Files Modified (10 files):**

**Storefront Domain:**
- `KbStore.Storefront/Domains/SellableItems/SellableItemEntity.cs` - Added ProductId field
- `KbStore.Storefront.Abstractions/Contracts/SellableItems.cs` - Added ProductId to request/response
- `KbStore.Storefront.Abstractions/Interfaces/ISellableItemCommandService.cs` - Added productId parameter
- `KbStore.Storefront.Services/SellableItemCommandService.cs` - Pass ProductId through to request
- `KbStore.Storefront/Domains/SellableItems/SellableItemStateMachine.cs` - Store ProductId in entity
- `KbStore.Storefront.Services/SellableItemQueryService.cs` - Include ProductId in responses

**ApiService:**
- `KbStore.ApiService/KbStore.ApiService.csproj` - Added Storefront.Services reference
- `KbStore.ApiService/Extensions/WebApplicationBuilderExtensions.cs` - Registered Storefront services
- `KbStore.ApiService/Consumers/Catalog/ProductCreatedConsumer.cs` - NEW (has bug)
- `KbStore.ApiService/Consumers/Catalog/ProductNameUpdatedConsumer.cs` - NEW (has bug)
- `KbStore.ApiService/Consumers/Catalog/ProductDiscontinuedConsumer.cs` - NEW (has bug)

**Files Removed:**
- `KbStore.ApiService/Consumers/Catalog/ProductAvailabilityConsumer.cs` - Empty placeholder deleted

**Build Status:**
- ✅ All Storefront projects build
- ✅ All ApiService projects build
- ⚠️ Code review FAILED - CRITICAL issues found

## The Fix Required

### Option A: Remove Manual Idempotency Checks (RECOMMENDED)

**Trust MassTransit correlation - remove GetByIdAsync calls:**

```csharp
// ProductCreatedConsumer - BEFORE (WRONG)
var existing = await _sellableItemQueryService.GetByIdAsync(msg.ProductId, ...);
if (existing != null) return;
await _sellableItemService.CreateAsync(...);

// ProductCreatedConsumer - AFTER (CORRECT)
try
{
    await _requestClient.GetResponse<SellableItemResponse>(new CreateSellableItemRequest
    {
        ProductId = msg.ProductId,
        SKU = msg.Sku,
        Name = msg.Name ?? msg.Sku,
        BasePrice = 0m,
        ItemType = "Product",
        Payload = new Dictionary<string, object?>
        {
            ["CatalogSku"] = msg.Sku,
            ["Dimensions"] = msg.Dimensions,
            // ...
        }
    });
}
catch (RequestFaultException ex) when (ex.Message.Contains("SKU") && ex.Message.Contains("already exists"))
{
    // SKU conflict - saga already exists, this is idempotent
    _logger.LogInformation("SellableItem already exists for SKU {SKU}, event is idempotent", msg.Sku);
}
```

**Why this works:**
- State machine correlates by SKU: `e.CorrelateBy((saga, context) => saga.SKU == context.Message.SKU)`
- If saga exists with same SKU → MassTransit routes to existing instance
- If saga doesn't exist → MassTransit calls SetSagaFactory to create new
- No manual idempotency checks needed

**Changes needed:**
1. Inject `IRequestClient<CreateSellableItemRequest>` instead of `ISellableItemCommandService`
2. Remove `GetByIdAsync` calls
3. Send command directly via request client
4. Handle `RequestFaultException` for SKU conflicts

### Option B: Add GetByProductIdAsync Method (NOT RECOMMENDED)

Keep manual checks but add query method:

```csharp
// Add to ISellableItemQueryService
Task<SellableItemResponse?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken);

// Consumer uses it
var existing = await _sellableItemQueryService.GetByProductIdAsync(msg.ProductId, ...);
if (existing != null) return;
```

**Why NOT recommended:**
- Duplicates MassTransit's built-in idempotency
- Adds TOCTOU race conditions
- More complex than trusting the framework
- GetByProductId is for read-model queries, not orchestration

## Architectural Decisions

### Q1-Q4 Answers (From Phase 2 Planning)

**Q1: When should SellableItems be created?**
- **Answer:** Immediately when ProductCreated fires, in Draft state, BasePrice=0
- **Reasoning:** Safe defaults, manual curation control, zero data loss

**Q2: Where does BasePrice come from?**
- **Answer:** Defaults to 0, set via UpdatePriceAsync later
- **Reasoning:** Storefront owns customer-facing pricing

**Q3: How to handle Product deletion?**
- **Answer:** Soft-delete (discontinue) to preserve order history
- **Reasoning:** SellableItems may be referenced by orders/invoices

**Q4: How to handle race conditions?**
- **Answer:** MassTransit defensive programming patterns
- **Reasoning:** Trust framework correlation, catch expected exceptions

### Correlation Strategy

**Final Decision (after expert consultation):**
- **ProductId is a REFERENCE FIELD** - not the CorrelationId
- **SKU is the CORRELATION KEY** - for event routing
- **CorrelationId is AUTO-GENERATED** - each saga has own identity

**Use Cases:**
- **SKU correlation:** Routing events to correct saga instance
- **ProductId field:** Cross-domain queries (read-model only)
- **CorrelationId:** SellableItem-specific commands (PublishAsync, UpdatePriceAsync, etc.)

## Next Steps

### 1. Fix Consumer Implementations

**Delegate to `dotnet-backend-engineer`:**
- Remove manual `GetByIdAsync` checks from all 3 consumers
- Change from `ISellableItemCommandService` to `IRequestClient<TRequest>`
- Send commands via MassTransit request/response pattern
- Handle `RequestFaultException` for SKU conflicts (expected, idempotent)
- Handle `RequestFaultException` for validation failures (log error, don't retry)

**Expected changes:**
- `ProductCreatedConsumer.cs` - Replace service with request client
- `ProductNameUpdatedConsumer.cs` - Replace service with request client, remove create-if-missing logic
- `ProductDiscontinuedConsumer.cs` - Replace service with request client

### 2. Add Tests

**Delegate to `integration-test-engineer`:**
- Test ProductCreatedConsumer idempotency (duplicate events)
- Test ProductNameUpdatedConsumer with existing SellableItem
- Test ProductDiscontinuedConsumer with missing SellableItem
- Test SKU conflict handling
- Test cross-domain correlation (Product → SellableItem)

### 3. Code Review Again

**After fixes, run `code-review` again to verify:**
- CRITICAL issues resolved
- Idempotency works correctly
- No manual query-before-command patterns
- MassTransit patterns followed correctly

### 4. Commit & Document

**After code review passes:**
- Run `changelog-manager` to generate docs
- Run `git-workflow-manager` to create commit
- Push to remote

## Git Status

**Current state (before commit):**
- Last commit: `ee8a6ca` - "WIP: feat(storefront): Implement SellableItem domain with polymorphic architecture"
- Branch: `feature/storefront-services-implementation`
- Uncommitted changes: Phase 2A + Phase 2B implementation (10 files modified, 3 new consumers)
- NOT pushed to remote yet

**About to commit:**
- WIP commit of Phase 2 implementation
- Known issues documented in this restart prompt
- Next session will fix correlation bugs then finalize

## Key Files to Reference

**Consumers to Fix:**
- `KbStore.ApiService/Consumers/Catalog/ProductCreatedConsumer.cs`
- `KbStore.ApiService/Consumers/Catalog/ProductNameUpdatedConsumer.cs`
- `KbStore.ApiService/Consumers/Catalog/ProductDiscontinuedConsumer.cs`

**Catalog Events (to consume):**
- `KbStore.Catalog.Abstractions/Contracts/Products.cs` - ProductCreated, ProductNameUpdated, ProductDiscontinued

**Storefront Contracts (to send):**
- `KbStore.Storefront.Abstractions/Contracts/SellableItems.cs` - CreateSellableItemRequest, UpdateSellableItemNameRequest, DiscontinueSellableItemRequest

**State Machine (correlation logic):**
- `KbStore.Storefront/Domains/SellableItems/SellableItemStateMachine.cs` - Lines 16-26 (Created event config)

## Important Learnings

### MassTransit Patterns

1. **SelectId() doesn't accept nullable Guid** - must return non-null Guid
2. **InsertOnInitial = true is the real optimization** - not null CorrelationId
3. **Separate ProductId field is idiomatic** - verified from MassTransit test cases
4. **CorrelateBy handles idempotency** - don't manually check before sending commands
5. **Trust the framework** - MassTransit handles correlation, routing, and idempotency

### Orchestration Workflow

1. **systems-architect for design** - analyze integration points before implementation
2. **Domain experts for decisions** - masstransit-expert answered technical questions
3. **code-review catches issues** - found critical bug backend engineer missed
4. **User insights are valuable** - "GetByProductId is query-only" was the key realization

### What Went Wrong

1. **Conflated two patterns** - expert recommended separate field, engineer queried as if same ID
2. **Didn't trust MassTransit** - added manual idempotency checks instead of using CorrelateBy
3. **Misunderstood correlation** - thought ProductId needed to be CorrelationId for cross-domain
4. **Skipped testing early** - would have caught correlation bug immediately

## Commands to Resume Work

```bash
# Verify current state
git status
git log --oneline -5

# Start fixing consumers
# Use dotnet-backend-engineer to:
# 1. Remove manual idempotency checks
# 2. Switch to IRequestClient<TRequest> pattern
# 3. Trust MassTransit SKU correlation

# After fixes:
# 1. Run code-review again
# 2. Add integration tests
# 3. Run changelog-manager
# 4. Run git-workflow-manager
# 5. Push to remote
```

## Success Criteria for Phase 2 (Updated)

✅ ProductId field added to Storefront domain
✅ ApiService references Storefront.Services
✅ 3 consumers implemented (ProductCreated, ProductNameUpdated, ProductDiscontinued)
❌ Consumers use MassTransit request/response pattern (NOT service layer directly)
❌ Idempotency via SKU correlation (NOT manual GetByIdAsync checks)
❌ Code review passes with no CRITICAL/HIGH issues
⚠️ `dotnet build` succeeds (currently true but implementation wrong)
❌ Integration tests verify event flow
❌ Committed via git-workflow-manager
❌ Documented via changelog-manager

## Ready to Fix

You're now ready to fix the correlation bug. The path is clear:

1. **Remove manual idempotency checks** - trust MassTransit CorrelateBy
2. **Use IRequestClient pattern** - send commands directly via MassTransit
3. **Handle expected exceptions** - SKU conflicts are normal (idempotent)
4. **Test thoroughly** - verify idempotency works with duplicate events

**Trust MassTransit. It already does what you need.**
