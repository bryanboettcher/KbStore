# KB3D Business Operational Requirements
## Real-World E-Commerce Workflows from 5+ Years of Operations

**Document Purpose:** This document captures the business workflows, operational needs, and essential features identified through KB3D's operational experience with PrestaShop. These requirements come from real business needs, not theoretical planning.

**Source Context:** Brett and Kris have operated KB3D's e-commerce store on PrestaShop for over 5 years, accumulating hundreds of third-party modules to address gaps in native functionality. This document extracts what actually matters for their business operations.

---

## Critical Business Workflows

### Product Bundles and Kits

**Business Need:** KB3D sells simple product bundles ("packs") that combine multiple inventory items into a single purchasable kit.

**Current PrestaShop Behavior:**
PrestaShop supports packs, but they are described as "like the configurator but simpler without options." The business expects that a pack containing "2x Item A + 1x Item B" should automatically calculate how many complete packs can be sold based on current stock levels of the constituent items.

**Real-World Example:**
A heat set insert kit might contain assorted sizes of inserts. If the kit needs 2x M3 inserts and 1x M4 insert, and inventory shows 100x M3 and 50x M4, the system should calculate that 50 complete kits are available (limited by the M4 inventory).

**Business Expectation:**
When constituent item stock changes (through any means: sale, manual adjustment, receiving shipment), pack availability should update automatically without manual intervention or expensive recalculation processes.

**Pain Point:**
PrestaShop calculates pack availability correctly at creation, but does not recalculate when constituent inventory changes. The third-party module that attempts to fix this creates the operational outages described in the PrestaShop Pain Points document.

**KbStore Requirement:**
Pack availability must be recalculated asynchronously whenever constituent inventory changes. The customer-facing availability display should reflect current stock levels without requiring expensive synchronous calculations during checkout.

---

### Product Configurator

**Business Need:** KB3D offers a product configurator that allows customers to build custom assemblies by selecting compatible components.

**Current PrestaShop Implementation:**
Implemented via third-party module `swt_productconfiguratorusingquestionnaire`. The configurator creates dynamic product packs based on customer choices. These dynamic packs are stored in `ps_swt_customer_configurator` table.

**Key Distinction from Simple Packs:**
Configurator packs are dynamic and customer-specific. Unlike standard packs, their quantities do NOT need to be tracked or updated globally. Each configured product is essentially a one-time bundle specific to that customer's order.

**Business Logic:**
The configurator validates component compatibility and calculates pricing based on selections. When a customer completes their configuration and adds it to cart, the system needs to validate that all selected components are available at checkout time.

**KbStore Design Consideration:**
Configurator-generated products should be treated differently from standard packs. They don't need ongoing stock reconciliation because they're ephemeral configurations rather than standing inventory items.

**Open Question Not Addressed in Conversations:**
How complex are the compatibility rules in the configurator? Can certain components not be combined? Are there quantity restrictions or interdependencies between selections? This level of detail was not discussed and would need to be clarified with Kris.

---

## Essential Feature Requirements

### Payment Processing

**Business Priority:** "The biggest complexities would be payment processing and shipments"

**Current PrestaShop Approach:**
Payment providers offer PrestaShop modules, allowing integration without custom development. The module ecosystem handles the technical integration details.

**KbStore Challenge:**
Without a module ecosystem, KbStore must implement payment provider integrations directly. This is described as "a ton of work" by Brett, indicating this is a significant concern for the business.

**Required Capabilities (Inferred from Standard E-Commerce):**
- Credit card processing
- Payment method tokenization for repeat customers
- Refund and partial refund processing
- Payment status webhooks for asynchronous completion
- Fraud detection integration

**Stakeholder Note:**
Brett's concern suggests payment integration should be prioritized early in KbStore development. The business cannot operate without functioning payment processing.

---

### Shipping Integration

**Business Priority:** Identified alongside payment processing as a major complexity

**Current PrestaShop Implementation:**
KB3D uses multiple shipping providers through dedicated modules:
- `wkupsshipping` - UPS integration
- `wkuspsshipping` - USPS integration
- Additional carrier modules

**Real-World Operational Challenges:**

**UPS OAuth Migration:**
In late 2024, UPS switched their API to OAuth authentication, breaking the existing integration. The third-party module required updates to support the new authentication method. After updating:
- UPS rates disappeared from checkout for most US addresses
- Rates worked correctly for Kris's Ohio address and European addresses
- Rates would appear briefly then disappear
- Root cause was never fully diagnosed in the conversation

**Rate Caching Issues:**
Both UPS and USPS modules maintain caches of shipping rates per address and cart combination. This caching created debugging difficulties because:
- Repeated tests showed cached results instead of fresh calculations
- Cache invalidation required manual database manipulation
- No clear expiration strategy was evident

**Required Capabilities:**
- Real-time rate calculation from multiple carriers
- Package dimension and weight calculation based on cart contents
- Address validation
- Label generation and tracking number retrieval
- Rate shopping (showing customer the cheapest options)

**Business Logic:**
The system must calculate package dimensions and weight from the items in the cart to get accurate shipping quotes. This implies product data must include dimensions and weight information.

**KbStore Design Consideration:**
Shipping integration is a domain boundary problem. The Order domain needs shipping cost information, but the actual carrier API calls could be isolated in a dedicated Shipping domain that responds to queries about rates and handles label generation.

---

### Product Image Management

**Business Need:** "Automatic image handling for conversions to WEBP/AVIF"

**Current PrestaShop Implementation:**
Third-party module handles automatic conversion of uploaded images to modern formats (WebP and AVIF) for improved page load performance.

**Business Value:**
Modern image formats significantly reduce bandwidth usage and improve customer experience through faster page loads. This is particularly important for e-commerce where product images are central to the browsing experience.

**KbStore Requirement:**
Image upload process should automatically generate multiple formats and sizes. The frontend should serve appropriate format based on browser capabilities (with fallbacks for older browsers).

**Operational Workflow:**
1. Admin uploads product image (likely JPEG or PNG)
2. System automatically generates:
   - WebP version (smaller file size, good browser support)
   - AVIF version (even smaller, cutting-edge browsers)
   - Multiple sizes for responsive layouts (thumbnail, gallery, full-size)
   - Original format as fallback
3. Frontend serves optimal format based on browser capabilities

---

### SEO and Redirect Management

**Business Need:** "SEO tools and redirects management"

**Current PrestaShop Implementation:**
Third-party module provides SEO optimization tools and URL redirect management.

**Business Context:**
E-commerce sites need strong SEO for organic traffic. Product URLs may change over time (due to SKU changes, reorganization, etc.) and redirects prevent broken links and preserve search engine rankings.

**Required Capabilities:**
- Custom URL slugs for products and categories
- Meta tag management (title, description, keywords)
- Canonical URL generation
- 301 redirect management for changed URLs
- Automatic sitemap generation
- Structured data markup (Schema.org Product)

**KbStore Design Consideration:**
URL structure and redirects affect multiple domains. Product URLs are customer-facing (Storefront domain), but the product identity lives in Catalog. Redirects may need to be managed independently to avoid coupling domains.

---

### Wishlist

**Business Need:** Customers want to save products for future purchase without adding them to cart.

**Current PrestaShop Implementation:**
Third-party wishlist module.

**Typical Wishlist Functionality:**
- Anonymous users can create session-based wishlists
- Authenticated users have persistent wishlists
- Move items between wishlist and cart
- Share wishlist with others (gift registry functionality)
- Price drop notifications (optional feature)

**Business Value:**
Wishlists reduce cart abandonment by giving customers a place to "save for later" without pressure to purchase immediately. Also valuable for gift-giving scenarios.

**KbStore Design Consideration:**
Wishlists are customer-specific state, similar to carts but with different persistence requirements. Unlike carts which can expire quickly, wishlists should persist long-term for authenticated users.

---

### Quotation and Invoicing System

**Business Need:** "Quotations and invoicing system"

**Current PrestaShop Implementation:**
Third-party module for quote generation.

**Business Context:**
Not all purchases go through immediate checkout. Some customers (particularly business buyers) want to request quotes for large or custom orders before committing to purchase.

**Required Workflow (Typical E-Commerce):**
1. Customer adds items to cart
2. Instead of proceeding to checkout, requests a quote
3. Admin reviews quote request
4. Admin generates formal quote with pricing and terms
5. Quote sent to customer (typically with expiration date)
6. Customer accepts or declines quote
7. Accepted quote converts to an order

**Business Value:**
Enables B2B sales, large orders, and custom configurations where pricing may need negotiation or approval.

**KbStore Design Consideration:**
Quotes are related to carts and orders but represent a distinct workflow. A quote is essentially a "draft order" that exists outside the normal checkout flow, with human review and approval before converting to an actual order.

---

### Support Desk Integration

**Business Need:** "Support desk" for customer service operations

**Current PrestaShop Implementation:**
Third-party module integrating support ticketing system with the store.

**Business Value:**
Customer service needs context when helping customers. Integrating support tickets with order history, product information, and customer data improves support efficiency and customer satisfaction.

**Required Capabilities (Typical):**
- Ticket creation from customer account page
- Automatic ticket creation for returns/refunds
- Order context automatically attached to tickets
- Email integration (customers can reply via email)
- Internal notes (visible to staff only) vs customer-visible messages
- Ticket status tracking and SLA monitoring

**KbStore Design Consideration:**
Support tickets reference customers and orders but don't modify them. This suggests a separate Support domain that queries Customer and Order domains for context but maintains its own ticket state.

---

### Page Builder

**Business Need:** "Live page builder"

**Current PrestaShop Implementation:**
Third-party page builder module for creating custom landing pages and content.

**Business Value:**
Marketing campaigns, seasonal promotions, and product launches often need custom landing pages. Requiring developer involvement for each page creates bottlenecks and slows down marketing initiatives.

**Required Capabilities (Typical):**
- Drag-and-drop interface for content creation
- Pre-built components (hero sections, product grids, testimonials, CTAs)
- Custom HTML/CSS for advanced users
- Mobile preview and responsive design
- Version history and rollback capability
- A/B testing support (advanced feature)

**Business Context:**
Brett describes this as an essential feature that was originally a "nice-to-have." Over 5 years of operation, the page builder proved valuable enough to become indispensable.

**KbStore Design Consideration:**
Page builder content is distinct from product catalog. Pages may reference products or categories, but they're marketing assets rather than product data. This could be a separate Content domain.

---

## Module Inventory Analysis

Brett mentions "hundreds of modules" accumulated over 5 years. The conversation reveals several specific modules:

**Stock and Inventory:**
- `bundlesstocks` - Pack stock synchronization (the problematic module)
- `stockmanagement` - General inventory management

**Shipping:**
- `wkupsshipping` - UPS integration
- `wkuspsshipping` - USPS integration
- `statscarrier` - Carrier statistics
- `orderfees_shipping` - Shipping fees management
- `discountshippingcost` - Shipping discounts
- `stfreeshippingmanager` - Free shipping rules
- `wkavailfreeshipping` - Free shipping availability

**Product Configuration:**
- `swt_productconfiguratorusingquestionnaire` - Advanced product configurator

**Operations:**
- `barcodelabels` - Label printing

**Analysis:**
The sheer number of modules, particularly around shipping (7 shipping-related modules visible in the module list), suggests that shipping is genuinely complex and requires significant functionality. Shipping rules, discounts, free shipping thresholds, carrier selection, and label printing all represent distinct operational needs.

---

## Operational Pain Points

### Version Lock-In

**Problem Statement:**
KB3D runs a PrestaShop version that is approximately 5 years out of date (PrestaShop 1.7.x from ~2020). They cannot upgrade because:

1. Module compatibility testing would be prohibitively expensive
2. Custom modifications would be overwritten
3. Module vendors don't maintain compatibility consistently
4. The risk of breaking production outweighs the benefits of updates

**Business Impact:**
- Security vulnerabilities cannot be patched
- Performance improvements in newer versions are inaccessible
- New PrestaShop features cannot be used
- Technical debt accumulates over time

**Brett's Assessment:**
"Yeah, this version of PrestaShop is pretty much held together with hopes, prayers and band-aid solutions, but it's hanging in there and performant enough that we'll defer any drastic changes until forced to"

**KbStore Implication:**
The architecture must allow upgrading framework components without touching business logic. Domains should be isolated from web framework versions, allowing independent evolution.

---

### Developer Workflow Challenges

**Problem Statement:**
Brett mentions the frontend "performs pretty terribly at all" before recent optimizations, despite backend performance being acceptable. The distinction between "frontend" and "backend" suggests performance issues in the customer-facing pages rather than admin operations.

**Performance Optimization Efforts:**
"We just finished about a 2 week run of serious frontend performance optimizations" - indicating significant time investment in making customer pages faster.

**KbStore Implication:**
Customer-facing performance must be a first-class concern in architectural decisions. The Storefront domain's query services must be optimized for read-heavy workloads. Caching strategies should prioritize customer-facing data.

---

### Debugging Complexity

**Problem Statement:**
When issues occur, determining the root cause is extremely difficult:
- Is it PrestaShop core?
- Is it a specific module?
- Is it a module interaction?
- Is it a custom modification?

The UPS shipping issue exemplifies this: rates work for some addresses but not others, appear briefly then disappear, and the root cause remains unclear even after extensive investigation.

**Brett's Experience:**
"I can't even guess at the time it would take to build all of these on a new platform... definitely doable, but something that would have to be done over the course of months"

This suggests the complexity is genuinely substantial, not just perceived difficulty.

**KbStore Implication:**
Clear domain boundaries and explicit communication contracts make debugging tractable. When an issue occurs, the message flow can be traced to identify which domain is responsible.

---

## Business Rules and Policies

### Backorder Policies

The conversation discusses backorder handling but doesn't fully specify KB3D's actual backorder policies. Questions that need stakeholder clarification:

- Are backorders allowed for all products or only specific items?
- Is there a customer preference for allowing/disallowing backorders?
- When inventory is restocked, are backorders fulfilled in order or does inventory become generally available?
- Are customers notified when backordered items become available?

Bryan's proposed architecture (in the Domain Architecture document) assumes backorder support, but the specific business rules weren't discussed in detail.

---

### Pricing Rules

The conversation mentions "coupon / discount code / price rules" in passing but doesn't detail how pricing rules work. Questions that need clarification:

- Are there customer-segment-specific prices (wholesale, retail, VIP, etc.)?
- Are there quantity-based discounts beyond the simple "5-pack is cheaper than 5x single"?
- How do promotional discounts interact with other pricing rules?
- Are there time-based promotions (Black Friday sales, etc.)?

---

### Shipping Rules

Brett mentions multiple shipping-related modules, suggesting complex shipping rules. Questions that need clarification:

- How is free shipping threshold determined?
- Are there product-based shipping rules (heavy items, fragile items)?
- How does international shipping differ from domestic?
- Are there shipping discounts or promotions?

---

## Features Identified as Essential

Brett's statement: "There's just an extensive list of nice-to-haves, but now pretty much essential stuff that's been added"

This reveals that KB3D's feature set evolved through actual operational needs rather than upfront planning. Features that started as "nice-to-haves" proved essential through real-world usage.

**Confirmed Essential Features:**
1. Payment processing
2. Multi-carrier shipping integration
3. Product configurator
4. Support desk integration
5. Wishlist functionality
6. Quotation and invoicing system
7. SEO tools and redirect management
8. Automatic image optimization (WebP/AVIF)
9. Page builder
10. Product bundles/packs with automatic stock calculation

**Implication for KbStore:**
These features should be considered core capabilities rather than optional add-ons. While they don't all need to be implemented at launch, the architecture should accommodate them without requiring fundamental redesign.

---

## Questions Requiring Stakeholder Input

### Product Configuration Complexity

**Question:** How complex are the configurator's compatibility rules?

**Context:** The configurator creates dynamic packs based on customer choices, but the conversation doesn't detail how choices interact. Can customers select incompatible components? Are there quantity restrictions? Are there interdependencies?

**Why This Matters:** Configuration validation logic significantly affects domain design. Simple configurations could be validated at the UI level, while complex rule engines might require a dedicated Configuration domain.

---

### Customer Segmentation

**Question:** Does KB3D have different customer segments with different pricing or access?

**Context:** B2B e-commerce often has wholesale customers, retail customers, VIP customers, etc., each with different pricing and access to products.

**Why This Matters:** Customer segmentation affects how pricing is calculated and whether the Customer domain needs sub-types or roles.

---

### International Operations

**Question:** Does KB3D ship internationally, and if so, what additional complexity does this introduce?

**Context:** International shipping involves customs forms, duty calculations, restricted products, and different carrier integrations.

**Why This Matters:** International complexity might require a dedicated Shipping domain rather than treating it as a simple Order workflow step.

---

### Return and Refund Workflows

**Question:** What is KB3D's process for handling returns and refunds?

**Context:** Returns affect inventory (restocking), orders (partial refunds), and customer service. The conversation mentions "returns management" but doesn't detail the workflow.

**Why This Matters:** Returns create reverse workflows that affect multiple domains. Clear business rules are needed to design the event flows correctly.

---

## Summary: Operational Requirements for KbStore

Based on 5+ years of real operational experience, KB3D needs:

**Core Commerce:**
- Robust payment processing with multiple providers
- Multi-carrier shipping with real-time rates and label generation
- Product bundles (packs) with automatic availability calculation
- Advanced product configurator for custom assemblies
- Quotation workflow for large/custom orders

**Customer Experience:**
- Wishlist functionality
- Backorder handling with notifications
- Image optimization for fast page loads
- SEO-friendly URLs and metadata

**Operations:**
- Support desk integration with order context
- Page builder for marketing content
- Redirect management for URL changes
- Clear debugging paths when issues occur

**Platform Requirements:**
- Ability to upgrade framework without touching business logic
- Clear ownership of functionality (no "is it core or is it a module?" questions)
- Performance optimized for customer-facing pages
- Extensibility without requiring modifications to domain code

The overarching theme: KB3D has identified what actually matters through real operational experience. KbStore's architecture should treat these as first-class concerns rather than afterthoughts to be addressed through modules or plugins.
