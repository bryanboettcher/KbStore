# KbStore.Catalog - Architecture Documentation

## Purpose

The Catalog vertical slice is the **authoritative source** for physical inventory tracking and stock management within the KbStore system. It owns two distinct but related domains:

1. **Products** - Catalog items that can be sold (SKU-based), representing physical or digital goods
2. **Inventory** - Physical stock tracking for parts/components (PartNumber-based)

**Key Responsibility**: Catalog tracks **"what exists physically"** and **"how much stock is available"**. It does NOT own customer-facing pricing or merchandising concerns - those belong to the Storefront domain.

This service is designed as a **self-contained, algorithmically pure domain** using state machines to enforce business rules. While it uses MassTransit infrastructure for event publishing, it focuses solely on inventory domain logic without cross-cutting concerns (emails, notifications, etc.).

Products can optionally link to Inventory items to track stock levels, but non-inventory products (digital goods, services) are also supported.

**Note on Product.Price Field**: The `Product` entity includes a `Price` field for internal cost tracking, but this is NOT the customer-facing price. The Storefront domain owns customer-facing pricing, promotions, and merchandising logic.

## Architectural Decisions (WHY)

### 1. WHY State Machines Instead of Traditional Domain Models?

**Decision**: Use MassTransit Saga State Machines for both Product and Inventory aggregates.

**Rationale**:
- **Explicit State Modeling**: Product lifecycle (Enabled/Disabled/Discontinued) and Inventory lifecycle (Available/OnHold/Backordered/Discontinued) have explicit state transitions with clear business rules
- **Event Sourcing Benefits**: All state changes emit domain events, enabling downstream systems to react without tight coupling
- **Concurrency Handling**: Optimistic concurrency through database row versioning prevents race conditions when multiple operations occur simultaneously
- **Temporal Decoupling**: Request/response patterns allow for asynchronous validation (e.g., Product creation validates Inventory availability via request)
- **Clear Boundaries**: State machine transitions enforce which operations are valid in each state, making illegal states unrepresentable

**Trade-off**: Increased complexity compared to CRUD operations, but this complexity is essential for enforcing business invariants in a distributed system.

### 2. WHY Separate Product and Inventory Domains?

**Decision**: Products and Inventory are separate aggregates with different identifiers and lifecycles.

**Rationale**:
- **Different Business Concerns**:
  - Products represent what customers buy (SKU = Stock Keeping Unit)
  - Inventory tracks physical stock of parts/components (PartNumber = manufacturer part identifier)
- **Many-to-One Relationships**: Multiple products can reference the same inventory item (e.g., product bundles, different retail packages of same component)
- **Independent Lifecycles**: A product can exist without inventory (digital goods, made-to-order), and inventory can exist without products (components not yet cataloged)
- **Different Stakeholders**: Products are managed by merchandising/sales teams; Inventory is managed by warehouse/operations teams
- **Event Choreography**: Inventory state changes (QuantityChanged, Held, Discontinued) are published as events that Products subscribe to, enabling reactive availability updates

**Evidence from Tests**: `Product_Create` tests show products can be created without `InventoryId`, and `Product_InventoryEvents` tests demonstrate how products react to inventory lifecycle events.

### 3. WHY Two-Phase Deletion (Discontinued → Deleted)?

**Decision**: Deletion requires two operations: first marks as Discontinued, second actually removes the entity (finalization).

**Rationale**:
- **Audit Trail**: Preserves historical data for reporting and compliance
- **Undo Capability**: Discontinued items can be restored (Inventory allows re-creation of discontinued items back to Available state)
- **Cascading Changes**: First deletion transitions dependent entities (e.g., Product transitions to Discontinued when Inventory is discontinued), second deletion finalizes cleanup
- **Event Notification**: Different events for different phases:
  - `ProductDiscontinued` / `InventoryDiscontinued` - Item no longer available but data preserved
  - `ProductDeleted` / `InventoryDeleted` - Permanent removal, trigger cleanup in other systems

**Evidence from Tests**:
- `Product_Delete.When_deleting_discontinued_product` shows saga finalization after second delete
- `Inventory_Delete.When_deleting_discontinued_item` demonstrates the two-phase pattern

### 4. WHY Request-Based Inventory Validation During Product Creation?

**Decision**: Product creation with `InventoryId` sends an asynchronous `InventoryStatusRequest` to validate the inventory exists and get current stock.

**Rationale**:
- **Temporal Consistency**: Inventory state can change between product creation request and saga start; request ensures current state
- **Graceful Degradation**: Product creation succeeds even if inventory validation fails or times out, but product is created in Disabled state (fail-safe)
- **No Distributed Transactions**: Avoids two-phase commit; each aggregate maintains its own consistency
- **Timeout = 0**: Immediate timeout configured because inventory response should be near-instant (same database); timeout/fault both result in Disabled product

**Evidence from Code**:
- `ProductStateMachine` lines 20-24 configure the request with 0-second timeout
- `Product_Create.When_creating_with_inventory_link_faulted` and `When_creating_with_inventory_link_timeout` both result in Disabled products

### 5. WHY Optimistic Concurrency for Sagas but Pessimistic for Jobs?

**Decision**: State machines use optimistic concurrency (row versioning), job sagas use pessimistic locking.

**Rationale**:
- **Saga Concurrency**: Business operations are infrequent and unlikely to conflict; optimistic concurrency allows high throughput without locks
- **Row Versioning**: PostgreSQL `xid` type provides automatic versioning; conflicts are rare and can be retried by MassTransit
- **Job Semantics**: Job execution requires exactly-once guarantee; pessimistic locking prevents duplicate job execution
- **Performance**: Most operations succeed on first attempt; conflicts are exceptional cases

**Evidence from Code**: `HostBuilderExtensions.cs` lines 72, 78, 92 show different concurrency modes.

### 6. WHY Event Hierarchy (BaseEvent → Specific Events)?

**Decision**: Contracts define inheritance: `ProductUpdated : BaseProductEvent`, `ProductAvailabilityChanged : ProductUpdated`, etc.

**Rationale**:
- **Selective Subscription**: Consumers can subscribe to broad categories (`ProductUpdated`) or specific events (`ProductNameUpdated`)
- **Reduced Noise**: Intermediate events like `ProductAvailabilityChanged` aggregate multiple causes (stock threshold change, inventory held, quantity change) into single event type
- **Polymorphic Handling**: Handlers can process all product updates generically or handle specific cases
- **Event Evolution**: Adding new event types doesn't break existing consumers subscribed to base types

**Evidence from Contracts**: `Products.cs` lines 78-79, 114-116 show the hierarchy.

## Business Rules and Domain Constraints

### Product Domain Rules

1. **SKU Uniqueness**: SKU must be unique across all products (enforced by database unique index)
   - Duplicate creation attempts throw `ProductConflictException.DuplicateSku`
   - Evidence: `Product_Create.When_creating_with_duplicate_sku`

2. **State-Based Operation Restrictions**:
   - **Enabled State**: Can be updated, disabled, or deleted
   - **Disabled State**: Can be updated or enabled (not available for purchase but can be modified)
   - **Discontinued State**: Cannot be modified; only operation allowed is second delete (finalization)
   - Evidence: `ProductStateMachine` lines 230-262 reject all update operations when Discontinued

3. **IsAvailable Calculation**: `IsAvailable = IsStocked && IsEnabled`
   - Product must be both stocked and enabled to be available for purchase
   - Non-inventory products are always considered "stocked" unless disabled
   - Evidence: `ProductEntity.cs` lines 35-42

4. **IsStocked Logic**:
   - Products without `InventoryId`: Always stocked (e.g., digital products, services)
   - Products with Inventory:
     - Inventory status must be Available (not Held, Backordered, or Discontinued)
     - Stock quantity must be >= StockThreshold (or >= Quantity if no threshold set)
   - Evidence: `ProductStateMachine.UpdateStockQuantity` lines 334-350

5. **Inventory Status Resolution**:
   - **Success**: Product transitions to Enabled with current stock quantity
   - **Fault/Timeout**: Product transitions to Disabled with StockQuantity = 0
   - Product creation always succeeds, but state reflects inventory validation result
   - Evidence: `Product_Create` tests for successful, faulted, and timeout scenarios

6. **Inventory Hold Behavior**: When inventory is held, product becomes unstocked (unavailable) even if quantity is sufficient
   - Evidence: `Product_InventoryEvents.When_inventory_held` shows `IsStocked = false` despite quantity

7. **Cascading Discontinuation**: When linked inventory is discontinued, product automatically transitions to Discontinued state
   - Evidence: `Product_InventoryEvents.When_inventory_discontinued`

8. **Cascading Deletion**: When linked inventory is deleted, product saga is finalized (removed)
   - Evidence: `Product_InventoryEvents.When_inventory_deleted`

### Inventory Domain Rules

1. **PartNumber Uniqueness**: PartNumber must be unique (enforced by database unique index)
   - Duplicate creation is idempotent: returns existing item without error or event
   - Evidence: `Inventory_Create.When_creating_with_duplicate`

2. **Resurrection Pattern**: Creating a Discontinued inventory item transitions it back to Available state
   - Allows recovery from accidental discontinuation
   - Emits `InventoryCreated` event (not update event)
   - Evidence: `Inventory_Create.When_creating_discontinued_item`

3. **State-Based Operation Matrix**:

   | Operation | Available | OnHold | Backordered | Discontinued |
   |-----------|-----------|--------|-------------|--------------|
   | IncreaseQuantity | ✓ | ✗ | ✓ (→Available) | ✗ |
   | DecreaseQuantity | ✓ | ✗ | ✗ | ✗ |
   | UpdateDescription | ✓ | ✓ | ✓ | ✗ |
   | Hold | ✓ | ✗ | ✗ | ✗ |
   | Release | ✗ | ✓ | ✗ | N/A |
   | Delete | ✓ (→Disc) | ✓ (→Disc) | ✓ (→Disc) | ✓ (→Deleted) |

   Evidence: `InventoryStateMachine` state transition definitions

4. **Insufficient Stock Prevention**: Decreasing quantity below zero throws `InventoryValidationException.InsufficientStock`
   - Precondition check in Available state (line 53)
   - Evidence: `Inventory_DecreaseQuantity` tests

5. **Hold Restrictions**:
   - Can only hold Available items
   - Cannot hold already-held, backordered, or discontinued items
   - Evidence: `Inventory_Hold` tests for each invalid state

6. **Backorder Auto-Recovery**: Increasing quantity of Backordered item transitions it back to Available
   - Enables automatic restocking workflow
   - Evidence: `InventoryStateMachine` lines 158-163

## Domain Boundaries

### What This Service OWNS

1. **Product Lifecycle**: Create, enable/disable, update attributes, discontinue/delete products
2. **Inventory Lifecycle**: Create, track quantity, hold/release, discontinue/delete inventory
3. **Availability Calculation**: Determines if products are available based on inventory status and thresholds
4. **SKU & PartNumber Uniqueness**: Enforces business identifiers
5. **Stock Threshold Management**: Defines when products become unavailable due to low stock

### What This Service PUBLISHES (Integration Events)

**Product Events**:
- `ProductCreated` - New product added to catalog
- `ProductNameUpdated`, `ProductDimensionsUpdated`, `ProductQuantityUpdated` - Attribute changes
- `ProductAvailabilityChanged` - Availability status changed (multiple causes)
- `ProductStockThresholdUpdated`, `ProductLeadTimeUpdated` - Operational parameter changes
- `ProductEnabled`, `ProductDisabled` - Availability toggles
- `ProductDiscontinued`, `ProductDeleted` - Lifecycle termination

**Inventory Events**:
- `InventoryCreated` - New inventory item tracked
- `InventoryQuantityChanged`, `InventoryQuantityIncreased`, `InventoryQuantityDecreased` - Stock level changes
- `InventoryDescriptionUpdated` - Metadata changes
- `InventoryHeld`, `InventoryReleased` - Hold status changes
- `InventoryDiscontinued`, `InventoryDeleted` - Lifecycle termination

### What This Service CONSUMES

**Inventory Events** (Internal Choreography):
- Products subscribe to inventory events to maintain consistency
- `InventoryQuantityChanged`, `InventoryHeld`, `InventoryReleased`, `InventoryDiscontinued`, `InventoryDeleted`

**External Events** (Orchestrated via ApiService):
- Order placement events from Storefront → ApiService → Catalog to trigger inventory holds
- Order cancellation events from Storefront → ApiService → Catalog to trigger inventory releases
- Warehouse receiving events → ApiService → Catalog to trigger quantity increases

**Note**: Catalog does not directly consume events from other domains. The ApiService orchestration layer handles cross-domain choreography.

### What This Service DOES NOT OWN

1. **Customer-Facing Pricing**: Storefront domain owns pricing, promotions, and merchandising logic
2. **Orders**: Does not manage customer orders (Storefront domain)
3. **Warehouse Operations**: Does not manage physical locations or picking/packing
4. **Search/Discovery**: Provides data but doesn't own search indexes or recommendation engines
5. **Cross-Cutting Concerns**: No email notifications, external integrations, or orchestration logic (handled by ApiService)

## Integration Points

### Inbound (Commands This Service Handles)

Via `IProductCommandService`:
- Create, update, enable/disable, delete products
- All operations return `ProductModel` representing current state

Via `IInventoryCommandService`:
- Create, update quantity, update description, hold/release, delete inventory
- All operations return `InventoryModel` representing current state

### Outbound (Events This Service Publishes)

All events inherit from base models (`ProductModel`, `InventoryModel`) providing full entity snapshot at time of event. This enables consumers to build read models without querying back.

### Service Layer Pattern

**Separation of Concerns**:
- **KbStore.Catalog**: Domain logic (state machines, entities) and infrastructure (database, messaging)
- **KbStore.Catalog.Abstractions**: Contracts (events, requests, responses) and service interfaces
- **KbStore.Catalog.Services**: Service implementations that wrap state machines with validation and exception translation

**Why This Separation**:
- Clients depend only on Abstractions (contracts), not implementation
- Allows multiple implementations (MassTransit messaging, direct database, mocks for testing)
- `MassTransitProductCommandService` translates domain exceptions into typed exceptions consumers understand
- Enables API gateway to reference only Abstractions package

**Evidence from Code**: `MassTransitProductCommandService` catches `RequestFaultException` and calls `.ToProductException()` to translate MassTransit faults into domain exceptions.

## Data Consistency Patterns

### Within Aggregate Consistency

**Strong Consistency**: Each saga (Product, Inventory) is transactionally consistent within its own boundary.
- Entity Framework transaction wraps state machine transition and event publishing
- Optimistic concurrency (row version) prevents lost updates
- Events are published in same transaction (outbox pattern ensures delivery)

### Cross-Aggregate Eventual Consistency

**Choreography**: Products react to Inventory events asynchronously
- Inventory publishes `InventoryQuantityChanged` → Product updates `StockQuantity` and recalculates `IsStocked`
- Gap between inventory change and product availability update is acceptable (seconds)
- No distributed transactions; each aggregate advances independently

### Idempotency Patterns

1. **Duplicate Product Creation**: Throws exception (SKU is business key, duplicates are errors)
2. **Duplicate Inventory Creation**: Returns existing item (PartNumber is business key, duplicates are idempotent)
3. **Duplicate Events**: MassTransit deduplication ensures events processed once
4. **Enable/Disable Toggles**: Throws exception if already in target state (not idempotent by design, indicates caller error)

**Why Different Approaches**:
- SKU duplicates indicate integration errors (two systems trying to create same product)
- PartNumber duplicates are expected (multiple sources may report same part exists)
- Evidence: `Product_Create.When_creating_with_duplicate_sku` throws exception vs `Inventory_Create.When_creating_with_duplicate` succeeds

## Technology Stack Rationale

- **MassTransit**: Provides saga framework, request/response semantics, outbox pattern, job scheduling
- **PostgreSQL**: Supports `xid` row versioning for optimistic concurrency, robust transactional semantics
- **RabbitMQ**: Message broker for event publishing and saga request/response
- **Entity Framework Core**: ORM with MassTransit integration for saga persistence
- **Hangfire**: Background job processing (job sagas use pessimistic concurrency)

## Common Patterns

### State Machine Event Configuration Pattern

All state machine events follow this configuration pattern:
```csharp
Event(() => EventName, ConfigureEvent);

private static void ConfigureEvent<TMessage>(IEventCorrelationConfigurator<Entity, TMessage> conf)
{
    conf.CorrelateById(s => s.Message.Id);
    conf.OnMissingInstance(b => b.ExecuteAsync(c => throw new NotFoundException()));
}
```

**Why**: Centralizes correlation logic and missing instance handling; ensures consistent error behavior.

### Response Message Builder Pattern

State machines use helper methods to build response messages:
```csharp
private static Task<SendTuple<TMessage>> Message<TMessage>(BehaviorContext<Entity> context)
    => context.Init<TMessage>(new { /* properties from saga state */ });
```

**Why**: Ensures all events carry complete entity snapshot; consumers don't need to query for current state.

### Extension Method Organization

- `HostBuilderExtensions`: Infrastructure setup (Hangfire, MassTransit, saga configuration)
- `WebApplicationExtensions`: Middleware and pipeline configuration
- `ServiceCollectionExtensions`: Service registration
- `*RequestFaultExceptionExtensions`: Exception translation from MassTransit faults to domain exceptions

**Why**: Keeps Program.cs clean; extension methods are discoverable and testable.

### Test Naming Convention

Tests follow `{Aggregate}_{Operation}.When_{scenario}` pattern:
- `Product_Create.When_creating_with_inventory_link_successful`
- `Inventory_Hold.When_holding_available_item`

**Why**: Self-documenting; easy to find test coverage for specific scenarios; groups related tests.

## Key Files Reference

**State Machines**:
- `Domains/Products/ProductStateMachine.cs`
- `Domains/Inventory/InventoryStateMachine.cs`

**Entities**:
- `Domains/Products/ProductEntity.cs`
- `Domains/Inventory/InventoryEntity.cs`

**Infrastructure**:
- `Extensions/HostBuilderExtensions.cs` - MassTransit and Hangfire configuration
- `Persistence/ApplicationDbContext.cs` - EF Core context with saga mapping
