# KbStore Domain Architecture
## Event-Driven E-Commerce Platform Design

**Document Purpose:** This document describes the domain-driven, event-sourced architecture for KbStore. Each domain is a bounded context with exclusive ownership of its data, communicating through an event bus using MassTransit.

**Architectural Philosophy:** Pure domain implementations that stand alone, orchestrated through explicit events and commands. Business rules live in the API service layer, not buried in domain logic.

---

## Core Architectural Principles

### Principle 1: Domain Purity and Isolation

Each domain project implements a reference-quality solution for its bounded context. Domains make their own architectural decisions about dependencies (databases, caching, etc.) and expose functionality exclusively through command/query services.

**What This Means:**
- The Inventory domain is the definitive implementation of inventory management, period.
- If another team needed inventory management, they could use KbStore.Inventory directly
- Domains never directly reference each other's internals
- A domain can be tested in complete isolation

### Principle 2: Commands via Message Bus, Queries via Direct Access

**Command Services:**
All mutating operations use MassTransit to publish commands to the message bus. Even operations that appear synchronous like `GetById` go through the bus architecture. This ensures:
- All state changes are observable through events
- Operations can be retried with idempotency guarantees
- Consumer concurrency prevents race conditions through database locking
- Failed operations can be automatically retried or dead-lettered

**Query Services:**
Read operations bypass the message bus and query the underlying database directly for performance. The query service returns optimized read models (projections) rather than full domain entities.

**Future Evolution:**
Query services will eventually migrate to Redis-backed read caches. Event handlers will intercept `XxxUpdated` events and maintain these caches, but this migration will be invisible to consumers since they still use the same query service interface.

### Principle 3: Granular Events for Operational Hooks

Every mutating operation publishes fine-grained events. Rather than publishing `InventoryUpdated` with all changes, publish specific events like:
- `InventoryQuantityDecreased`
- `InventoryQuantityIncreased`
- `InventoryBackorderThresholdReached`
- `InventoryRestocked`

This allows subscribers to react to specific changes without parsing a generic update event to determine what actually changed.

### Principle 4: Orchestration in API Service, Not in Domains

Business rules that span multiple domains live in the API service layer, not buried inside domain logic. Examples of orchestration:

**Good (Explicit Orchestration):**
```
ApiService receives: CreateInventoryItemCommand
├─> Publishes to Inventory domain: CreateInventoryItem
├─> Waits for: InventoryItemCreated event
└─> Publishes to Catalog domain: CreateProductFromInventory
    └─> Resulting in ProductCreated event
```

**Bad (Implicit Domain Coupling):**
```
Inventory domain receives: CreateInventoryItem
└─> Inventory domain directly calls Product domain
    └─> Creates hidden dependency between domains
```

The first pattern makes the business rule visible and testable. The second buries it in domain internals and creates implicit coupling.

---

## Domain Boundaries

### Catalog Domain

**Responsibility:** Backend management of physical products and inventory

**Sub-Domains:**
- **Inventory**: Physical items on shelves, quantity tracking, backorder management
- **Products**: Makes inventory sellable by defining sale units, configurations, weights, and dimensions

**Key Characteristics:**
- Does NOT care about pricing (that's Storefront's concern)
- Does NOT care about customers or carts
- Focused purely on "what exists" and "what can be sold"
- Tags may live here for product categorization

**Data Ownership:**
- Inventory quantities and locations
- Product specifications (weight, dimensions, SKUs)
- Product-to-inventory relationships
- Backorder thresholds and rules

---

### Storefront Domain

**Responsibility:** Customer-facing product catalog and purchasing experience

**Sub-Domains:**
- **SellableItems**: Customer-visible products with pricing and availability
- **Carts**: Temporary shopping containers (Redis-backed)

**Key Characteristics:**
- Has an arbitrary number of SellableItems, primarily created from Catalog events
- Can create SellableItems NOT backed by Products (gift cards, PDFs, virtual products)
- Responsible for the "read" part of using the site
- Enables maintenance mode by staying read-only while Catalog is offline

**Data Ownership:**
- Product pricing and discounts
- Product descriptions and marketing content
- Product images and media
- Customer-visible availability
- Active shopping carts

**Cart-Specific Design:**

Carts live exclusively in Redis with automatic expiration:
- Visitor arrives → Assigned accountId (random GUID if guest, real ID if logged in)
- Cart created in Redis with 30-minute TTL
- Any cart modification resets the 30-minute timer
- Background pings keep cart alive while customer browses
- On checkout, cart persists to long-term storage
- Abandoned carts automatically expire after 30 minutes with no maintenance needed

**Benefits:**
- Zero database load for abandoned carts
- Lightning-fast cart operations
- No cleanup jobs needed
- Scales horizontally through Redis clustering

---

### Order Domain

**Responsibility:** Processing and fulfilling customer purchases

**Key Characteristics:**
- Orders are immutable once created (any changes create new versions)
- Order processing happens asynchronously after checkout
- Integrates with payment and shipping providers

**Data Ownership:**
- Order line items with locked-in pricing
- Order status and fulfillment state
- Payment references and transaction IDs
- Shipping details and tracking information

---

### Customer Domain

**Responsibility:** User accounts and customer data management

**Separation Rationale:**
Customers are separate from Carts because cart operations need to be incredibly fast and lightweight. Customer data involves authentication, profile management, order history, and other concerns that shouldn't slow down cart operations.

**Data Ownership:**
- User authentication and authorization
- Customer profiles and preferences
- Shipping addresses and payment methods
- Order history references (not the orders themselves)

---

## Domain Entity Definitions

### Inventory

**Definition:** Represents a physical thing sitting on a shelf.

**Properties:**
- Quantity (never negative)
- Location
- SKU or internal identifier
- Backorder threshold
- Backorder allowed flag
- Physical characteristics (weight, dimensions)

**Behaviors:**
- Increase quantity (restocking)
- Decrease quantity (sale, damage, loss)
- Hold/release (temporary reservation)
- Trigger backorder when quantity hits threshold

**Rules:**
- Quantity can never go negative
- Decreasing below zero fails the operation
- Not sold directly to customers (Products make Inventory sellable)

**Events Published:**
- `InventoryCreated`
- `InventoryQuantityIncreased`
- `InventoryQuantityDecreased`
- `InventoryBackorderThresholdReached`
- `InventoryRestocked`
- `InventoryHeld`
- `InventoryReleased`

---

### Product (Abstract)

**Definition:** Makes Inventory sellable by defining how customers can purchase it.

**Subtypes:**

#### PhysicalProduct

**Definition:** Backed by Inventory, represents a sellable unit of physical items.

**Key Concept:** Multiple Products can reference the same Inventory with different configurations.

**Example:**
- Inventory: NEMA17 stepper motor (quantity: 100)
- Product A: "Single NEMA17" (1x motor, $15)
- Product B: "5-Pack NEMA17" (5x motors, $60) ← Cheaper per unit

**Properties:**
- References Inventory item(s) with quantities
- Sale quantity per purchase
- Total price
- Weight (for shipping calculations)
- Dimensions (for shipping calculations)

**Behaviors:**
- Calculate availability based on Inventory quantities
- Update availability when Inventory changes

#### OnDemandProduct

**Definition:** Not associated with Inventory. Used for items manufactured or sourced on-demand.

**Use Cases:**
- Custom-cut acrylic panels
- Print-on-demand items
- Made-to-order products
- Services

**Properties:**
- Same pricing/weight/dimension rules as PhysicalProduct (for shipping consistency)
- Lead time for fulfillment
- No quantity restrictions

#### DigitalProduct

**Definition:** Non-physical items delivered electronically.

**Use Cases:**
- Gift cards
- Downloadable PDFs
- Software licenses
- Digital designs/models

**Properties:**
- No weight or dimensions
- Instant fulfillment
- May have usage limits or expiration

**Behaviors:**
- Generate download link or license key on purchase
- Track download counts if limited

---

### SellableItem

**Definition:** A customer-visible product listing in the Storefront with pricing.

**Creation Pathways:**
1. **Primary:** Created automatically when Product events bubble out of Catalog
2. **Secondary:** Created directly in Storefront for virtual products (gift cards, PDFs)

**Properties:**
- Price (set in Storefront, not Catalog)
- Product reference (optional - may not exist for virtual items)
- Display name and description
- Images and media
- Categories and tags
- Customer-visible availability

**Key Design Decision:**
SellableItems exist in Storefront rather than Catalog because pricing is a customer-facing concern. The same Product might have different prices in different markets, during different promotions, or for different customer segments. Keeping this in Storefront domain allows pricing flexibility without polluting the Catalog.

---

## Event-Driven Workflows

### Workflow 1: Inventory Creation with Automatic Product

**Business Rule:** "When a new inventory item is created, automatically create a matching product to save the user a step"

**Implementation:**
```
1. Admin creates Inventory via backoffice
   └─> POST /api/inventory { name, quantity, sku, ... }

2. ApiService receives request
   ├─> Validates request
   └─> Publishes: CreateInventoryCommand

3. Inventory domain processes command
   ├─> Creates Inventory entity
   ├─> Stores in database
   └─> Publishes: InventoryCreated event

4. ApiService listens for InventoryCreated
   ├─> Applies business rule: "Always create default product"
   └─> Publishes: CreateProductCommand
       with { inventoryId, quantity=1, price=null }

5. Catalog domain processes command
   ├─> Creates Product entity referencing Inventory
   ├─> Stores in database
   └─> Publishes: ProductCreated event

6. Storefront domain listens for ProductCreated
   ├─> Creates SellableItem from Product
   ├─> Sets initial price (may be null, requiring admin to set)
   └─> Publishes: SellableItemCreated event

7. Backoffice UI listens for SellableItemCreated
   └─> Updates admin interface showing new product ready for pricing
```

**Key Points:**
- The business rule lives in ApiService orchestration, visible and testable
- Each domain publishes events after successful state changes
- Domains don't know about the orchestration, they just process commands
- Failure at any step can be retried without side effects (idempotency)

---

### Workflow 2: Backorder Handling

**Business Rule:** "When inventory runs out, allow customers to backorder if enabled. When restocked, fulfill backorders in the order they were placed."

**Implementation:**

```
Phase 1: Cart Purchase Depletes Inventory

1. Customer completes checkout with Cart containing PhysicalProduct
   └─> POST /api/checkout { cartId }

2. Order domain begins Cart-to-Order conversion
   └─> Publishes: DecreaseInventoryQuantityCommand for each item
       with { inventoryId, quantity, orderId, cartId }

3. Inventory domain processes command
   ├─> Acquires database lock on Inventory row
   ├─> Checks: currentQuantity >= requestedQuantity
   ├─> If yes:
   │   ├─> Decreases quantity
   │   ├─> Stores in database
   │   ├─> Releases lock
   │   └─> Publishes: InventoryQuantityDecreased event
   │       with { inventoryId, newQuantity, orderId }
   │
   └─> If no (insufficient stock):
       ├─> Releases lock
       └─> Publishes: InventoryInsufficientQuantity event
           with { inventoryId, requested, available, orderId }

4. Product domain listens for InventoryQuantityDecreased
   ├─> Recalculates availability for all Products using this Inventory
   ├─> Updates Product availability
   └─> Publishes: ProductQuantityUpdated event
       with { productId, newQuantity }

5. Storefront domain listens for ProductQuantityUpdated
   ├─> Updates SellableItem availability
   └─> Publishes: SellableItemQuantityUpdated event

6. Active Cart instances listen for SellableItemQuantityUpdated
   ├─> Compare cart quantities with newly announced quantities
   ├─> Carts with less than available: no action
   └─> Carts with more than available: enter backorder workflow

Phase 2: Cart Enters Backorder State

7. Cart with insufficient quantity available
   ├─> Checks backorder rules for all Products in cart
   ├─> If all Products allow backordering:
   │   ├─> Sets Cart.Status = PendingBackorder
   │   └─> Shows customer notification:
   │       "Items in your cart are out of stock but can be backordered"
   │
   └─> If any Product disallows backordering:
       ├─> Removes out-of-stock items from cart
       └─> Notifies customer to review cart

8. Customer chooses to backorder
   ├─> Checkout proceeds normally
   ├─> Order created but set to Status = PendingBackorder
   └─> Publishes: OrderBackordered event
       with { orderId, backorderedItems[] }

Phase 3: Inventory Restocked, Backorders Fulfilled

9. Admin restocks Inventory via backoffice
   └─> POST /api/inventory/{id}/restock { quantity }

10. Inventory domain processes restock
    ├─> Increases quantity
    ├─> If quantity was at backorder threshold:
    │   └─> Changes status from Backordered to Available
    ├─> Publishes: InventoryRestocked event
    └─> Publishes: InventoryQuantityIncreased event

11. Product domain listens for InventoryRestocked
    ├─> Recalculates availability
    └─> Publishes: ProductQuantityUpdated event

12. Order domain listens for ProductQuantityUpdated
    ├─> Queries for Orders with Status = PendingBackorder
    ├─> For each backordered Order using this Product:
    │   ├─> Attempts to fulfill (restart checkout from Step 2)
    │   └─> If successful:
    │       ├─> Sets Order.Status = PendingPayment
    │       ├─> Sends customer notification: "Your backorder can now be completed"
    │       └─> Publishes: OrderBackorderFulfilled event
    │
    └─> Orders are processed in FIFO order (first backordered, first fulfilled)
```

**Key Points:**
- Inventory quantity never goes negative
- Database locks prevent race conditions
- Backorder state is explicit, not inferred
- Customers are notified at every state change
- Backorders are fulfilled fairly (FIFO)
- All operations are idempotent and can be retried

---

### Workflow 3: Cart to Order Conversion

**Business Rule:** "When customer clicks checkout, convert cart to order, validate inventory, apply pricing, and process payment"

**Implementation:**

```
1. Customer clicks "Checkout"
   └─> POST /api/orders/from-cart { cartId }

2. Order domain begins conversion
   ├─> Creates new Order entity
   ├─> Copies customer information from Cart
   └─> For each SellableItem in Cart:
       └─> Publishes: PurchaseSellableItemCommand
           with { sellableItemId, quantity, cartId, orderId }

3. Storefront domain processes PurchaseSellableItem
   ├─> Looks up SellableItem
   ├─> If tied to Product:
   │   └─> Publishes: DecreaseInventoryQuantityCommand
   │       with { inventoryId, quantity, orderId }
   │
   └─> If virtual (not tied to Product):
       └─> Publishes: SellableItemPurchased event immediately
           with { sellableItemId, quantity, priceAtPurchase, orderId }

4. Inventory domain processes DecreaseInventoryQuantity
   ├─> Acquires database lock
   ├─> Validates quantity available
   ├─> Decreases quantity
   ├─> Releases lock
   └─> Publishes: InventoryQuantityDecreased event
       with { inventoryId, decreasedBy, newQuantity, 
              priceAtPurchase, orderId }

5. Order domain listens for InventoryQuantityDecreased
   ├─> Creates OrderLineItem
   │   with { productId, quantity, priceAtPurchase, timestamp }
   ├─> Removes corresponding item from Cart
   └─> When all items processed successfully:
       ├─> Applies coupon/discount codes
       ├─> Calculates shipping
       ├─> Generates Invoice
       └─> Publishes: OrderReadyForPayment event

6. If any inventory decrease fails:
   ├─> Order domain receives InventoryInsufficientQuantity event
   ├─> Shows customer: "Item X is no longer available"
   ├─> Customer chooses:
   │   ├─> Remove item → Continues with remaining items
   │   └─> Backorder → See Workflow 2
   │
   └─> If customer removes item:
       └─> Order processing resumes from Step 2 without that item

7. Customer proceeds to payment
   └─> Order domain integrates with Payment provider
       └─> On successful payment:
           ├─> Order.Status = PendingShipping
           ├─> Publishes: OrderPaid event
           └─> Shipping workflow begins (separate domain)
```

**Critical Design Points:**

**Price Locking:**
The price is captured at the moment inventory is decremented, not when the item was added to cart. This prevents pricing race conditions where a customer adds an item, the price changes, and they checkout expecting the old price.

**Atomic Cart Removal:**
Items are removed from the Cart only after successful inventory decrement. If a failure occurs, the Cart remains unchanged and the customer can make decisions.

**Concurrent Order Handling:**
MassTransit's consumer concurrency with database locking ensures that if two customers check out simultaneously for the last item:
1. First request acquires lock, decrements inventory successfully
2. Second request waits for lock, gets it, finds insufficient quantity, fails gracefully
3. Both customers get accurate feedback about availability

**Idempotency:**
Each command includes orderId. If a message is retried, the Inventory domain can check "Did I already process this orderId?" and respond with cached result instead of double-processing.

---

## Technical Implementation Details

### MassTransit Consumer Concurrency

**Problem:** Concurrent orders for the same inventory item can cause race conditions.

**Solution:** MassTransit consumers acquire database locks before modifying Inventory.

**How It Works:**

```
Message Queue: [DecreaseQty(item=A), DecreaseQty(item=A), DecreaseQty(item=B)]
                       ↓                    ↓                      ↓
Consumer Pool:    [Consumer1]          [Consumer2]           [Consumer3]
                       ↓                    ↓                      ↓
Database:         Lock(item=A)    Wait for Lock(item=A)      Lock(item=B)
                  Process                  ↓                   Process
                  Unlock          → Lock(item=A)              Unlock
                                    Process
                                    Unlock
```

Multiple consumers can process messages in parallel. When they target the same inventory item, database locks serialize access, preventing race conditions. When they target different items, they process concurrently.

**Result:** High throughput with guaranteed consistency.

---

### Query Service Evolution

**Current Implementation:**
Query services use direct database access for performance. They return projected read models optimized for specific UI needs.

```csharp
public interface IProductQueryService
{
    Task<ProductListViewModel> GetProductsAsync(int page, int pageSize);
    Task<ProductDetailViewModel> GetProductByIdAsync(Guid productId);
    Task<IEnumerable<ProductSearchResult>> SearchProductsAsync(string query);
}
```

**Future Implementation:**
Query services will be backed by Redis caches. Event handlers will maintain these caches:

```
ProductCreated event → Cache: products:{productId} = {...}
ProductUpdated event → Cache: products:{productId} = {...} (overwrite)
ProductDeleted event → Cache: del products:{productId}
```

The query service interface remains unchanged. Consumers don't know or care whether data comes from the database or cache.

**Migration Strategy:**
1. Implement Redis-backed queries alongside database queries
2. Verify cache consistency through monitoring
3. Feature flag to switch between implementations
4. Gradually roll out Redis queries to production
5. Remove database query code once stable

---

## Aspire Application Structure

Each domain is an independent Aspire application that can be deployed separately.

**Benefits:**
- Domains scale independently based on load
- Domain failures are isolated (Inventory down doesn't break Order processing)
- Development teams can work on separate domains without coordination
- Testing can target individual domains

**Deployment Topology:**

```
Load Balancer
    ├─> ApiService (orchestration)
    ├─> Catalog (Inventory + Products)
    ├─> Storefront (SellableItems + Carts)
    ├─> Orders (Order processing)
    └─> Customers (User management)

Message Bus (MassTransit + RabbitMQ)
    ↕
All domains publish/subscribe
```

---

## Design Decision: Why Separate Catalog and Storefront?

**Question Bryan Is Anticipating:** "Why not just have Products in one place?"

**Answer:**

Catalog concerns:
- Physical reality (what exists in the warehouse)
- Inventory management (quantities, locations, backorders)
- Product specifications (weight, dimensions, SKU)
- Relationships between inventory and sellable units

Storefront concerns:
- Pricing (varies by market, customer segment, promotion)
- Marketing content (descriptions, images, seo)
- Customer-visible availability (may hide low-stock items)
- Shopping cart mechanics

If these lived in the same domain:
- Pricing changes would touch the same tables as inventory changes
- Marketing updates would risk breaking inventory logic
- The domain would violate single responsibility principle
- Testing would require setting up both inventory and pricing data

By separating them:
- Catalog can be in "maintenance mode" while Storefront remains readable
- Different teams can own different concerns
- Catalog changes don't risk breaking customer-facing UI
- Virtual products (gift cards, PDFs) live purely in Storefront without polluting Catalog

---

## Open Questions for Refinement

### Question 1: Product Configuration Complexity

The current design handles simple Products (1x inventory → 1x product) and multi-item Products (5x inventory → 1x product with bulk pricing). 

How should complex configurations be handled? For example:
- "Voron Kit" = 200+ different inventory items
- Customer-configurable toolhead = Variable items based on selections

**Potential Approach:** Introduce ConfigurableProduct that maintains a set of rules mapping customer choices to required inventory items. The configurator validates choices against inventory availability before adding to cart.

### Question 2: Soft Reservations During Checkout

The current design doesn't hold/reserve inventory when items are in a cart. Inventory is only decremented when the order is placed.

Should there be a middle ground where inventory is soft-reserved during checkout (payment page) to prevent "checkout disappointment" when another customer completes their order first?

**Tradeoff:** Soft reservations reduce disappointment but add complexity and can lock up inventory for abandoned checkout sessions.

### Question 3: Tag-Based Product Organization

The document mentions "tags may live here [in Catalog]" but doesn't fully elaborate on the entity tag blockchain concept Bryan mentioned.

Should products be organized through hierarchical categories or through flexible tagging, and where does this classification logic live?

**Implication:** This affects how customers browse and filter products, which impacts Storefront more than Catalog.
