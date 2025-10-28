# KbStore Technical Architecture Patterns
## Implementation Patterns and Design Decisions

**Document Purpose:** This document describes the technical architecture patterns, technologies, and design decisions that form KbStore's implementation approach. This is distinct from domain boundaries (covered in Domain Architecture) and business needs (covered in Business Requirements).

**Target Audience:** Technical implementation teams who need to understand the architectural patterns and why certain technical decisions were made.

---

## Foundational Technology Stack

### .NET and C# Foundation

**Decision:** KbStore is built using C# and .NET (specifically ASP.NET Core Web API).

**Rationale:**
Bryan's primary expertise is as a C# backend developer with 30 years of software development experience. Building KbStore in his native stack allows him to focus on architectural decisions rather than learning a new language.

**Additional Context:**
This project serves as both a commercial application and a resume builder. Using C# demonstrates expertise in enterprise-grade technology while producing an open-source reference implementation that other developers can learn from.

---

### Aspire Application Framework

**Decision:** Each domain is an independent Aspire application.

**What Aspire Provides:**
Aspire is Microsoft's framework for building cloud-native, distributed applications. It provides orchestration, service discovery, and observability out of the box.

**Architectural Benefit:**
Domains can be deployed and scaled independently. If the Inventory domain experiences high load (frequent stock updates), it can scale separately from the Storefront domain (which may be read-heavy but write-light).

**Operational Benefit:**
Domain failures are isolated. If the Inventory domain crashes, customers can still browse products (from Storefront's cached data) even though they cannot complete purchases until Inventory recovers.

**Development Benefit:**
Development teams can work on different domains without coordination. Each domain has its own deployment pipeline and can be updated independently.

---

### MassTransit Message Bus

**Decision:** All cross-domain communication uses MassTransit with RabbitMQ as the transport.

**What MassTransit Provides:**
MassTransit is a .NET message bus abstraction that handles message routing, retries, error handling, and consumer management. It supports multiple transport mechanisms (RabbitMQ, Azure Service Bus, Amazon SQS, etc.).

**Why Message Bus Over Direct Calls:**
Direct service-to-service calls create tight coupling. If Service A calls Service B directly, Service A must wait for Service B to respond, and Service A must know Service B's endpoint. Using a message bus:
- Publishers don't know who consumes their messages
- Consumers don't know who published messages they receive
- Operations can be retried automatically on failure
- New consumers can be added without modifying publishers

**Consumer Concurrency Pattern:**
MassTransit runs multiple consumer instances in parallel. When a consumer processes a message, it can acquire database locks to prevent race conditions. Multiple consumers can process different messages simultaneously, but when they target the same resource (like the same inventory item), database locks serialize access automatically.

**Result:** High throughput with guaranteed consistency.

---

## Domain Communication Patterns

### Command Services via Message Bus

**Pattern:** All mutating operations use MassTransit to publish commands.

**Example:**
```
ApiService publishes: CreateInventoryItemCommand
    ↓ (message bus routes to)
Inventory domain consumer receives command
    ↓ (processes command)
Inventory domain publishes: InventoryItemCreated event
```

**Why Even Synchronous-Looking Operations Use the Bus:**
Even operations like `GetById` go through the message bus architecture. This ensures consistency in how operations are handled and makes all operations observable through message logging.

**Idempotency Requirement:**
Commands include correlation IDs. Consumers can check "Have I already processed this correlation ID?" to prevent double-processing if messages are retried.

---

### Query Services via Direct Database Access

**Pattern:** Read operations bypass the message bus and query databases directly.

**Why Direct Access for Queries:**
Read operations don't change state, so they don't need the guarantees that the message bus provides. Direct database access is significantly faster for queries, improving response time for customer-facing pages.

**Projection Pattern:**
Query services return optimized read models (projections) rather than full domain entities. A read model contains exactly the data needed for a specific UI view, reducing data transfer and simplifying frontend code.

**Example:**
```csharp
// Domain entity might have 20+ properties
public class Product 
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    // ... 15 more internal properties ...
}

// Query service returns minimal view model
public class ProductListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
}
```

**Future Evolution Plan:**
Query services will eventually be backed by Redis caches maintained by event handlers. This migration will be transparent to consumers because the query service interface remains unchanged.

---

### Event Publishing for State Changes

**Pattern:** Every mutating operation publishes granular events describing what changed.

**Why Granular Events:**
Rather than publishing generic `InventoryUpdated` events, publish specific events like:
- `InventoryQuantityIncreased`
- `InventoryQuantityDecreased`
- `InventoryBackorderThresholdReached`
- `InventoryRestocked`

Granular events allow subscribers to react to specific changes without parsing a generic update event to determine what actually changed.

**Event Naming Convention:**
Events are past-tense (describing what happened) while commands are imperative (describing what should happen):
- Command: `CreateInventoryItem`
- Event: `InventoryItemCreated`
- Command: `DecreaseInventoryQuantity`
- Event: `InventoryQuantityDecreased`

---

### Orchestration in ApiService

**Pattern:** Business rules that span multiple domains live in the ApiService project, not buried inside domain logic.

**Why Separate Orchestration:**
Domain logic should focus on domain rules (like "inventory quantity cannot be negative"). Cross-domain workflows (like "when inventory is created, also create a product") represent business processes, not domain constraints.

**Implementation Approach:**
The ApiService contains orchestration logic implemented as MassTransit sagas or simple event handlers. These orchestrations are configurable, potentially through a UI, allowing business users to enable or disable specific workflows without code changes.

**Example Orchestration:**
```
ApiService orchestration handler:
    When: InventoryItemCreated event received
    Then: Publish CreateProductCommand with default configuration
```

This makes the business rule explicit and visible rather than hidden inside the Inventory domain.

---

## Data Ownership and Persistence

### Each Domain Owns Its Data

**Pattern:** Domains have exclusive ownership of their data. No other domain directly queries another domain's database.

**Why Exclusive Ownership:**
Shared database access creates invisible coupling. If Domain A queries Domain B's tables, Domain B cannot refactor its schema without coordinating with Domain A. Exclusive ownership allows domains to evolve independently.

**How Domains Share Data:**
Domains share data through three mechanisms:

1. **Event Payloads:** Events include relevant data. `InventoryQuantityDecreased` includes the new quantity, so subscribers don't need to query for it.

2. **Query Services:** If Domain A needs data from Domain B, Domain A calls Domain B's query service explicitly. This creates visible, traceable dependencies.

3. **Local Projections:** If Domain A frequently needs Domain B's data, Domain A can maintain a local projection. When Domain B publishes events, Domain A's event handler updates the local projection.

---

### Database Technology Choices

**Pattern:** Each domain makes its own database technology choice.

**Rationale:**
Different domains have different data characteristics:
- Inventory: Transactional data requiring ACID guarantees → SQL database (likely PostgreSQL or SQL Server)
- Carts: Ephemeral data with fast access requirements → Redis with TTL
- Products: Read-heavy catalog data → Could use document database or SQL with aggressive caching
- Events: Append-only event log → Could use event store or simple append-only table

Domains are free to choose the appropriate technology for their needs without being constrained by a one-size-fits-all decision.

**Current Known Decisions:**
- **Carts:** Explicitly using Redis with 30-minute TTL and automatic expiration
- **Other domains:** Likely SQL databases but specific choices not finalized in conversations

---

## Caching Strategy

### Current Implementation: No Caching

**Status:** Query services currently hit databases directly for every request.

**Performance Characteristics:**
Database queries are fast enough for current needs, and query optimization (proper indexes, efficient SQL) provides acceptable performance.

---

### Planned Evolution: Event-Driven Cache Updates

**Future Pattern:** Query services will be backed by Redis caches maintained by event handlers.

**How It Works:**
```
1. Inventory domain publishes: InventoryQuantityDecreased event

2. Inventory query service event handler receives event
   └─> Updates Redis cache: inventory:{id} = { new data }

3. Customer queries: "What's available for product X?"
   └─> Storefront query service checks Redis cache
       └─> If hit: Return cached data (microseconds)
       └─> If miss: Query database, populate cache, return data
```

**Why This Approach:**
Caching is complex. Starting without caching ensures correctness and allows identifying actual bottlenecks. Cache invalidation strategy can be refined based on real usage patterns rather than premature optimization.

**Migration Strategy:**
The query service interface remains unchanged. Implementation can switch from database to cache without affecting consumers. This allows gradual rollout with feature flags to verify cache consistency before full deployment.

---

## Concurrency and Consistency Patterns

### Optimistic Concurrency for Reads

**Pattern:** Customer-facing availability displays use optimistic concurrency.

**How It Works:**
When a customer views a product, the displayed availability comes from a cached or recently-calculated value. The system optimistically assumes this value is current and displays it immediately without expensive real-time recalculation.

**Trade-off:**
Availability might be stale by seconds or minutes, but customers get instant responses. For most products, brief staleness is acceptable.

---

### Pessimistic Concurrency for Writes

**Pattern:** Inventory decreases use pessimistic locking via database transactions.

**How It Works:**
When an order attempts to decrease inventory, the consumer acquires a database lock on that inventory row before checking availability. This prevents race conditions where two customers simultaneously purchase the last item.

**Implementation Detail:**
MassTransit consumer concurrency combined with database row locking provides this guarantee. Multiple consumers can process different inventory items in parallel, but access to the same item is serialized through database locks.

---

### Eventual Consistency Across Domains

**Pattern:** Cross-domain data is eventually consistent rather than immediately consistent.

**Example:**
```
1. Inventory domain decreases quantity
   └─> Publishes: InventoryQuantityDecreased event

2. Product domain receives event (milliseconds later)
   └─> Recalculates product availability
   └─> Publishes: ProductQuantityUpdated event

3. Storefront domain receives event (milliseconds later)
   └─> Updates SellableItem availability
   └─> Customer sees updated availability
```

Total delay: Typically under 1 second. For most scenarios, this level of eventual consistency is acceptable.

**When Eventual Consistency Is Not Acceptable:**
During checkout, the system performs pessimistic validation immediately before finalizing the order. This prevents "checkout disappointment" where a customer proceeds through payment only to discover an item is no longer available.

---

## Error Handling and Resilience

### Automatic Retry with Exponential Backoff

**Pattern:** MassTransit automatically retries failed message processing with increasing delays.

**How It Works:**
If a consumer throws an exception while processing a message, MassTransit:
1. Immediately retries (transient errors)
2. Waits 1 second, retries
3. Waits 2 seconds, retries
4. Waits 4 seconds, retries
5. After max retries, moves message to error queue (dead letter queue)

**Why This Helps:**
Transient failures (database connection timeout, temporary network blip) often resolve themselves. Automatic retries handle these without human intervention.

---

### Dead Letter Queue for Permanent Failures

**Pattern:** Messages that fail after all retries move to a dead letter queue for manual investigation.

**Operational Workflow:**
1. Operations team monitors dead letter queue
2. Investigates why message failed
3. Fixes root cause (bug, data corruption, etc.)
4. Replays message from dead letter queue

**Why This Approach:**
Permanent failures (like a bug in consumer code) require human intervention. Moving failed messages to a separate queue prevents them from blocking other messages while preserving the ability to replay them after fixes.

---

### Circuit Breaker Pattern (Future)

**Not Currently Implemented But Anticipated:**
If a downstream service is consistently failing, circuit breakers can temporarily stop sending messages to that service, allowing it to recover rather than overwhelming it with traffic.

---

## Development and Deployment Patterns

### Domain Independence

**Pattern:** Each domain can be developed, tested, and deployed independently.

**Benefits:**
- **Development:** Teams can work on different domains without coordination
- **Testing:** Domains can be tested in isolation with mock message bus
- **Deployment:** Deploying Inventory changes doesn't require redeploying Storefront
- **Scaling:** High-traffic domains scale independently of low-traffic domains

**Trade-off:**
Coordination is required when changing message contracts. If a publisher changes the structure of an event, all subscribers must be updated to handle the new structure (or maintain backward compatibility).

---

### Contract-First Development

**Pattern:** Message contracts are defined first, before implementation.

**Why This Matters:**
Message contracts are the API between domains. Defining them upfront ensures both publishers and consumers agree on structure before writing code.

**Example Contract:**
```csharp
public interface InventoryQuantityDecreased
{
    Guid InventoryId { get; }
    int PreviousQuantity { get; }
    int NewQuantity { get; }
    Guid OrderId { get; } // For correlation
    DateTime Timestamp { get; }
}
```

Publishers must include all required fields. Consumers can depend on these fields being present. Contract changes require versioning strategy (V1, V2, etc.) to maintain compatibility.

---

## Testing Strategy

### Unit Tests for Domain Logic

**Scope:** Domain entities and business rules in isolation.

**Approach:** Test domain logic without message bus, without database, without external dependencies. Pure business logic testing.

**Example:**
Test that `Inventory.DecreaseQuantity(amount)` throws exception when amount exceeds available quantity.

---

### Integration Tests for Message Flows

**Scope:** Verify message publishers and consumers interact correctly.

**Approach:** Use MassTransit's test harness to verify messages are published with correct structure and consumed correctly.

**Example:**
Verify that when `CreateInventoryItem` command is published, `InventoryItemCreated` event is eventually published with correct data.

---

### End-to-End Tests for Critical Workflows

**Scope:** Entire workflows from API call through domain processing to final state.

**Approach:** Start with HTTP request to ApiService, verify message flow through domains, check final state in databases.

**Example:**
POST to `/api/orders/checkout` → Verify inventory decreased → Verify order created → Verify customer notified.

**Challenge:**
End-to-end tests are slow and fragile. Use sparingly for critical paths (checkout, payment processing) rather than every workflow.

---

## Observability and Debugging

### Message Tracing

**Capability:** Every message includes correlation ID and causation ID.

**Correlation ID:** Groups all messages related to a single user action.

**Causation ID:** Identifies which message caused another message to be published.

**Debugging Workflow:**
When investigating an issue, operations team can query message logs by correlation ID to see the complete message flow for a specific order or customer action.

**Example:**
```
Customer clicks "Checkout" → Correlation ID: abc-123

Messages with correlation abc-123:
1. CheckoutCommand published by ApiService
2. DecreaseInventoryQuantity published (caused by CheckoutCommand)
3. InventoryQuantityDecreased published (caused by DecreaseInventoryQuantity)
4. OrderLineItemCreated published (caused by InventoryQuantityDecreased)
...
```

This message chain reveals exactly what happened and where failures occurred.

---

### Structured Logging

**Pattern:** All logs include structured data (JSON or similar) rather than free-text strings.

**Why Structured Logging:**
Operations teams can query logs by specific fields (like customer ID, order ID, inventory ID) rather than parsing text strings.

**Example:**
```json
{
  "timestamp": "2025-10-28T10:30:00Z",
  "level": "INFO",
  "domain": "Inventory",
  "event": "InventoryQuantityDecreased",
  "inventoryId": "550e8400-e29b-41d4-a716-446655440000",
  "previousQuantity": 100,
  "newQuantity": 95,
  "orderId": "abc-123",
  "correlationId": "xyz-789"
}
```

Operations can query: "Show me all inventory decreases for item X in the last hour" without parsing text.

---

## Security Considerations

### Authentication at API Gateway

**Pattern:** All external requests authenticate at the ApiService (acting as gateway).

**Why Gateway Authentication:**
Domains don't need to implement authentication repeatedly. They trust that any command arriving via the message bus has already been authenticated by the ApiService.

**Internal Communication:**
Domain-to-domain communication via message bus is trusted. Messages on the internal bus don't require authentication because they never originate from untrusted sources.

---

### Authorization per Domain

**Pattern:** Domains enforce their own authorization rules.

**Example:**
The Inventory domain might restrict certain operations to admin users. The ApiService includes user role information in commands, and the Inventory domain checks roles before processing.

**Why Domain-Level Authorization:**
Different domains have different authorization requirements. Centralizing all authorization in the ApiService would create a monolithic authorization service that knows about every domain's rules.

---

## Migration Strategy from PrestaShop

### One-Time Data Export

**Approach:** A specialized application extracts data from PrestaShop's database and exports it as a custom object model.

**Why Custom Export:**
PrestaShop's database schema is optimized for PrestaShop's architecture, not for KbStore's event-driven model. Direct database migration would preserve PrestaShop's coupling and complexity.

**Export Process:**
1. Read PrestaShop tables
2. Map to KbStore domain concepts
3. Generate domain commands (CreateInventoryItem, CreateProduct, etc.)
4. Export as JSON or similar format

---

### Import via REST API

**Approach:** The exported data is re-imported by calling KbStore's REST endpoints.

**Why REST Import:**
Using the public API ensures all business logic, validation, and event publishing happens correctly. Directly inserting into KbStore databases would bypass domain logic and create inconsistent state.

**Import Process:**
1. Read exported data
2. POST to `/api/inventory/items` for each inventory item
3. POST to `/api/products` for each product
4. POST to `/api/orders` for historical orders (if preserving order history)
5. All normal domain logic executes, events are published

**Result:**
KbStore starts with a clean, consistent state based on PrestaShop's data but structured according to KbStore's domain model.

---

### Hard Cutover Strategy

**Approach:** No gradual migration or dual operation. PrestaShop is turned off, KbStore is turned on.

**Why Hard Cutover:**
Running both systems simultaneously creates enormous complexity: synchronizing inventory between systems, handling orders placed in either system, reconciling customer accounts, etc. The effort required for dual operation exceeds the cost of a short maintenance window.

**Cutover Process:**
1. Announce maintenance window to customers
2. Turn PrestaShop into read-only mode (customers can browse but not purchase)
3. Export final data from PrestaShop
4. Import into KbStore
5. Verify import completeness
6. Point DNS to KbStore
7. Re-enable purchasing

**Estimated Downtime:** Not discussed in conversations, but likely measured in hours rather than days.

---

## Open Architecture Questions

### Tag-Based Product Organization

**Mentioned But Not Detailed:**
Bryan described a "blockchain of entity tags" using GUIDs where relationships are discovered through shared tag chains rather than enforced through database schema.

**What's Unclear:**
- How are tags assigned and managed?
- Where does tag logic live (Catalog domain? Storefront domain? Separate domain)?
- How do customers browse by tags?
- How do tags interact with traditional category hierarchies?

This concept was mentioned as a powerful pattern Bryan is excited about, but implementation details weren't discussed. This needs further architectural design.

---

### Complex Product Configuration

**Known But Not Detailed:**
KB3D uses an advanced product configurator for building custom assemblies. The configurator validates component compatibility and calculates pricing.

**What's Unclear:**
- How complex are compatibility rules? (Simple "not compatible" or complex dependency trees?)
- Where do configuration rules live?
- How are configurations priced? (Sum of parts? Custom pricing logic?)
- Can configurations be saved and reused?

The conversation confirmed configurators exist and are important, but didn't detail their complexity.

---

### Soft Reservations During Checkout

**Not Discussed:**
Should inventory be soft-reserved when items are in a cart (before order completion)?

**Trade-off:**
- **Pro:** Prevents checkout disappointment when another customer completes their order first
- **Con:** Ties up inventory for abandoned carts, reducing availability for other customers

Bryan's current design doesn't include soft reservations, opting for the simpler model where inventory is only decreased when orders are placed. This might be revisited based on business needs.

---

## Summary: Technical Architecture Principles

**Message-Driven Communication:**
All cross-domain interaction uses MassTransit message bus for commands and events. Direct service calls are avoided.

**Domain Independence:**
Each domain owns its data, makes its own technology choices, and can be deployed independently.

**Eventual Consistency:**
Cross-domain data is eventually consistent (typically within seconds), with pessimistic validation at critical points (checkout).

**Observable Operations:**
All operations are traceable through correlation IDs and structured logging.

**Resilience:**
Automatic retries, dead letter queues, and isolated domain failures prevent cascading failures.

**Clear Boundaries:**
Orchestration lives in ApiService, not hidden in domain logic. Authorization is domain-specific. Data ownership is explicit.

These patterns address the anti-patterns discovered in PrestaShop (tight coupling, synchronous blocking, hidden dependencies) while providing a foundation for long-term evolution of the platform.
