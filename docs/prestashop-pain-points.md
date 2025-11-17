# PrestaShop Operational Pain Points
## Lessons Learned from Production Issues

**Document Purpose:** This document captures the operational failures and architectural problems discovered in the KB3D PrestaShop deployment. These serve as anti-patterns to explicitly avoid when architecting KbStore.

**Key Insight:** PrestaShop's issues stem from synchronous, tightly-coupled operations that block user experience during expensive calculations. The module ecosystem forces "square peg in round hole" integrations that compound performance problems.

---

## Critical Performance Issues

### Pain Point #1: Pack Stock Calculation Blocking Checkout

**The Problem:**
Customers experience 2-4 minute delays on the "buy now" button during checkout when purchasing product bundles (packs). This is caused by synchronous stock reconciliation that runs during order validation.

**Root Cause Analysis:**

PrestaShop's pack system bundles multiple inventory items together (e.g., "2x Item A + 1x Item B = Pack C"). When the pack is first created, PrestaShop correctly calculates how many packs are available based on constituent item stock. However, when constituent item stock changes through any means (purchase, manual adjustment, restocking), PrestaShop does NOT automatically recalculate pack availability.

A third-party module attempts to fix this deficiency by running a hook during checkout that recalculates all pack quantities. This hook implementation performs the following anti-patterns:

**Anti-Pattern 1: N+1 Query Problem**
```sql
-- The module loops through EACH pack individually:
FOR EACH pack IN all_packs:
  UPDATE ps_stock_available 
  SET reserved_quantity = (
    SELECT SUM(od.product_quantity - od.product_quantity_refunded)
    FROM ps_orders o 
    INNER JOIN ps_order_detail od ON od.id_order = o.id_order
    INNER JOIN ps_order_state os ON os.id_order_state = o.current_state
    WHERE /* complex filtering */
    AND sa.id_product = od.product_id 
    AND sa.id_product_attribute = od.product_attribute_id
  )
```

This creates O(n) database round-trips where n is the number of packs in the system. Each query performs full table scans due to missing indexes.

**Anti-Pattern 2: Synchronous Execution in Critical Path**

The module runs this expensive operation during the checkout validation hook. The customer's HTTP request is held open waiting for this operation to complete before they receive confirmation.

**Observed Impact:**
- Checkout completion time: 2-4 minutes (measured)
- Customer abandonment during long "buy now" button hang
- Database lock contention during high-traffic periods
- Module created 64MB log file from thousands of queries during a single pack save operation

**Missing Database Indexes:**

The slow query log revealed full table scans on:
- `ps_orders(id_shop, valid, current_state)`
- `ps_order_detail(id_order, product_id, product_attribute_id)`
- `ps_stock_available(id_shop, id_product, id_product_attribute)`

Adding these indexes reduced query time but did not address the fundamental architectural flaw of synchronous execution during checkout.

**Why This Architecture Failed:**

PrestaShop's design assumes pack stock is relatively static and can be calculated on-demand. The module ecosystem forces developers to use hooks for extending behavior, but hooks execute synchronously in the HTTP request lifecycle. There's no clean way to defer expensive operations to background workers without significantly altering PrestaShop's core.

**KbStore Design Principle:**

Pack availability (or any bundle/configuration availability) must be calculated asynchronously when constituent inventory changes occur. The checkout process should use pre-calculated availability with optimistic locking, falling back to pessimistic validation only if the optimistic check indicates staleness. Under no circumstances should checkout be blocked waiting for complex inventory calculations.

---

### Pain Point #2: Module Integration Conflicts

**The Problem:**

KB3D accumulated numerous third-party modules over 5+ years of operation to add functionality PrestaShop doesn't provide natively. Modules were selected based on individual feature needs without consideration for how they would interact with each other. This created a fragile ecosystem where:

- Module updates frequently broke other modules
- Performance problems were impossible to attribute to a single source
- Simple features required combining multiple modules in unexpected ways
- Bug fixes in one module could cascade into failures elsewhere

**Example Conflicts:**

The pack stock calculation module conflicted with the product configurator module, requiring special exclusion logic to prevent the configurator's dynamic packs from being recalculated. This exclusion was hard-coded and fragile.

**Why This Architecture Failed:**

PrestaShop's module system provides hooks and overrides but lacks any formal contract for how modules should behave or communicate. Modules can silently observe events, modify data structures, or intercept database queries, creating implicit dependencies that aren't documented or enforceable.

The only way to discover module conflicts is through runtime failures in production.

**KbStore Design Principle:**

Domain boundaries must be explicit and enforced. Communication between domains happens exclusively through well-defined message contracts on the bus. Dependencies are explicit and compile-time checkable. A domain cannot silently observe or modify another domain's internal state.

---

### Pain Point #3: Stale Platform Version Locked by Customization

**The Problem:**

KB3D runs a PrestaShop version that is 5+ years out of date. The platform is locked at this version because:

1. The sheer volume of modules makes compatibility testing prohibitively expensive
2. Custom modifications to core PrestaShop files would be overwritten by updates
3. Module vendors don't maintain compatibility with newer PrestaShop versions consistently
4. The cost/benefit analysis favors staying on the known-working version

**Why This Architecture Failed:**

PrestaShop's architecture forces a choice: stay current with platform updates and lose customizations, or maintain customizations and fall behind on security and features. There's no clean separation between framework code and business logic that would allow independent evolution.

**KbStore Design Principle:**

Business logic must live in domain services that are completely independent of web framework versions. An Aspire host version bump or a migration from ASP.NET Core 8 to 9 should not require touching any business logic code. The only code that should reference framework APIs is the thin adapter layer at domain boundaries.

---

## Architectural Anti-Patterns Identified

### Anti-Pattern: Synchronous Processing in HTTP Request Lifecycle

**What PrestaShop Does:**

Expensive operations (stock calculations, complex validations) execute synchronously during web requests. Users wait for operations to complete before receiving responses.

**What KbStore Should Do:**

All expensive operations should execute asynchronously outside the request lifecycle. HTTP endpoints should:
1. Validate the request can be processed
2. Publish a command to the message bus
3. Return immediately with a correlation ID
4. Allow clients to poll for completion or subscribe to completion events

For checkout specifically, use optimistic validation with pre-calculated availability, then queue the actual order processing.

---

### Anti-Pattern: Tight Coupling Through Shared Database Tables

**What PrestaShop Does:**

All modules share the same database schema. Modules can directly read/write any table, creating invisible dependencies. Schema migrations require coordination across all modules.

**What KbStore Should Do:**

Each domain owns its data exclusively. Cross-domain queries happen through query services that abstract the underlying storage. If Domain A needs data from Domain B, it either:
1. Subscribes to relevant events from Domain B and maintains a local projection
2. Queries Domain B's query service explicitly
3. Includes necessary data in command responses

---

### Anti-Pattern: Implicit Event Handling Through Hooks

**What PrestaShop Does:**

Modules register hooks that get invoked by PrestaShop core at specific lifecycle points. These hooks can modify data, execute side effects, or even prevent operations from completing. The order of hook execution is undefined when multiple modules register for the same hook.

**What KbStore Should Do:**

Events are published to a message bus. Consumers subscribe to event types explicitly. Event handlers are:
- Idempotent (safe to process the same event multiple times)
- Order-independent (don't rely on execution sequence)
- Isolated (failures in one consumer don't affect others)

---

### Anti-Pattern: Negative Inventory Quantities

**What PrestaShop Does:**

Inventory quantities can go negative if multiple orders are placed concurrently for the last items in stock. The "reserved quantity" system attempts to handle this but creates complexity and doesn't prevent the race condition.

**What KbStore Should Do:**

Inventory quantity is a hard constraint that cannot be violated. Concurrent order processing uses database locks (via MassTransit consumer concurrency) to ensure only one order at a time can decrement a specific inventory item. If the inventory check fails, the customer is notified immediately and can choose an alternative or backorder.

---

## Performance Bottlenecks Discovered

### Database Query Patterns

PrestaShop's ORM (ObjectModel) generates inefficient queries that:
- Perform table scans instead of using indexes
- Execute separate queries for relationships (N+1 problem)
- Don't utilize query plan caching effectively

**Specific Example:**

The stock reservation query joins orders → order_detail → order_state → stock_available and aggregates across potentially thousands of rows, running this query separately for each product in a pack.

**KbStore Approach:**

Query services should use:
- Explicit SQL with proper indexing strategy
- Batch operations to reduce round-trips
- Read-through caching with explicit invalidation
- Projections pre-calculated via event handlers (eventual consistency acceptable for non-critical reads)

---

## Module Ecosystem Lessons

### Essential Features Identified Through Module Usage

Over 5 years, KB3D identified these features as essential (each currently requires a separate module):

**Core Commerce:**
- Advanced product configurator/customizer
- Quote/invoice generation system
- Wishlist functionality
- Customer support desk integration

**Operations:**
- Multi-carrier shipping integration with real-time rates
- Automated image optimization (WebP/AVIF conversion)
- SEO tools and redirect management
- Advanced search with filtering

**Marketing:**
- Page builder for landing pages
- Promotional pricing rules
- Email campaign integration
- Product recommendation engine

**Technical:**
- Performance monitoring
- Advanced caching layer
- Backup/restore utilities
- Developer debugging tools

**KbStore Implication:**

Rather than bolting these on through a module system, KbStore should architect for these capabilities from the start. This doesn't mean implementing everything at launch, but ensuring the domain model and event architecture can accommodate these features without requiring invasive changes later.

---

## Security Incidents

### Attack Pattern: Query String Exploitation

**Incident:**

KB3D experienced recurring attacks that presented as module malfunctions. Attackers exploited query string parameters to trigger expensive database queries, causing server performance degradation. The attacks would stop for weeks or months, then resume.

**Example Pattern:**
```
RewriteCond %{QUERY_STRING} q=Availability- [NC,OR]
```

Attackers crafted URLs that triggered availability recalculation for many products simultaneously, effectively DDoSing the site through legitimate but expensive operations.

**Why This Succeeded:**

PrestaShop's URL routing and query parameter handling allowed arbitrary parameters to trigger backend operations without rate limiting or authentication. The expensive pack recalculation module could be invoked directly through crafted URLs.

**KbStore Design Principle:**

All mutating operations must:
1. Require authentication tokens
2. Be rate-limited per user/IP
3. Execute asynchronously (attacks can fill queues but can't block the web tier)
4. Have circuit breakers that shed load under pressure

Read operations should:
1. Use cached results whenever possible
2. Have separate rate limits from writes
3. Fail fast if database pressure is high
4. Never trigger expensive background work as a side effect

---

## Maintenance Burden

### Version Lock Paralysis

**Current State:**

KB3D runs PrestaShop 1.7.x from approximately 2020. Module compatibility makes updates risky and expensive to test. Security patches can't be applied without regression testing the entire module ecosystem.

**Human Cost:**

Brett's operational notes indicate significant time spent:
- Debugging module conflicts
- Working around PrestaShop limitations
- Applying temporary fixes that become permanent
- Triaging whether problems are core PrestaShop, module bugs, or integration issues

**KbStore Design Principle:**

The cost of operating the system should decrease over time, not increase. This requires:
- Clean domain boundaries that prevent cross-contamination of issues
- Comprehensive automated testing that makes updates low-risk
- Clear ownership of each component's behavior
- Explicit contracts that can be validated at compile-time

---

## Summary: Architectural Lessons for KbStore

1. **Never block HTTP requests** waiting for expensive operations
2. **Domain isolation** prevents module-style integration hell
3. **Explicit contracts** over implicit hooks and observations
4. **Async-first** for all non-trivial operations
5. **Optimistic concurrency** for read-heavy workflows
6. **Database locks** for write conflicts (via MassTransit consumer concurrency)
7. **Event-driven** state changes with idempotent handlers
8. **Pre-calculated projections** for complex queries
9. **Negative constraints** (like inventory quantity ≥ 0) enforced at the domain level
10. **Framework independence** for business logic

PrestaShop's architecture accumulated technical debt through:
- Allowing synchronous complexity in the request path
- Sharing state through a common database
- Implicit coupling through hooks and database queries
- No mechanism to defer work to background processing

KbStore's architecture explicitly prevents these patterns through:
- Message bus separating command initiation from execution
- Domain-owned data with explicit cross-domain contracts
- Event publication for all state changes
- MassTransit for background processing with concurrency control
