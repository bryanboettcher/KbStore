### ADR-001: Polymorphic SellableItem Architecture

**Status:** Adopted

**Date:** 2025-10-28

#### 1. Context

The `Storefront` domain is responsible for presenting a diverse and evolving catalog of items to customers. These items include:
*   Physically stocked products (`StockedProduct`)
*   Made-to-order goods (`OnDemandProduct`)
*   Downloadable files (`DigitalDownload`)
*   Gift cards (`GiftCard`)

A traditional, monolithic data model for a `SellableItem` would require a single class with numerous optional properties to accommodate all variations. This approach leads to a brittle architecture that is difficult to maintain and extend, violating core SOLID principles (specifically the Open/Closed Principle).

The architectural goal is to create a flexible, extensible system that aligns with our domain-driven design, leverages the strengths of a document database, and provides a clear, repeatable pattern for introducing new types of sellable items in the future without refactoring existing code.

#### 2. Decision

We will adopt a **"Generic Container + Specific Payload"** pattern for the `SellableItem` document model.

This pattern treats a `SellableItem` as a generic "shell" document containing a minimal set of universal properties. A mandatory `type` string field will act as a **discriminator**, indicating the schema of a nested `payload` object that contains the specific attributes for that item type.

This architectural pattern will be supported by a conceptual **"SellableItem Type Plugin"** architecture. Each "plugin" will be a cohesive set of components (backend consumers, DTOs, frontend UI) that collectively define and manage a single `SellableItem` type.

#### 3. Rationale

*   **Extensibility:** New product types can be introduced by creating a new "plugin" without modifying any existing models or components. This makes the system open for extension but closed for modification.
*   **Domain Decoupling:** The `Storefront`'s representation of a product is fully decoupled from the `Catalog`'s. The mapping logic in the `ApiService` and `Storefront` provides a clean anti-corruption layer.
*   **Simplicity & Cohesion (SRP):** This pattern avoids a monolithic "god object." Backend and frontend components are small, focused, and only deal with the data relevant to the specific type they handle.
*   **Clean API Contracts:** API payloads are lean and self-describing. Consumers only receive the data necessary for a given product type, reducing bandwidth and cognitive overhead.
*   **Improved Developer Experience:** Provides a clear, standardized, and repeatable pattern for adding new business capabilities.

#### 4. Implementation Strategy

##### A. The `SellableItem` Document Model

The `SellableItem` will be stored in the `Storefront`'s document database with the following structure. The `payload` is polymorphic, its schema determined by the `type` field.

**Container (Shell) Structure:**
```csharp
// All SellableItem documents will share this top-level structure.
// Extensive xmldoc will be used here to drive OpenAPI generation.
public record SellableItem(
    Guid Id,
    string Type, // Discriminator field
    string Name,
    string Slug,
    string ThumbnailUrl,
    decimal BasePrice,
    object Payload // The polymorphic payload
);
```

**Initial Payload Types:**

| `Type` Discriminator | Payload DTO (`object Payload`) | Key Properties | Originating Domain / Concept |
| :--- | :--- | :--- | :--- |
| `StockedProduct` | `StockedProductPayload` | `AvailableQuantity`, `AllowBackorders`, `InventoryId` | `Catalog` Domain |
| `OnDemandProduct` | `OnDemandProductPayload` | `EstimatedLeadTime`, `CustomizationOptions` | `Storefront` (Virtual Product) |
| `DigitalDownload` | `DigitalDownloadPayload` | `FileType`, `FileSize`, `Version` | `Storefront` (Virtual Product) |
| `GiftCard` | `GiftCardPayload` | `ValueTiers`, `DeliveryMethod`, `ValidityPeriod` | `Storefront` (Virtual Product) |

##### B. The "SellableItem Type Plugin" Architecture

A "plugin" is not a formal framework but a conceptual grouping of the necessary components to fully support a `SellableItem` type. Adding a new type requires creating these five pieces:

1.  **Payload DTO:** A C# `record` or `class` defining the structure of the payload for the new type (e.g., `StockedProductPayload`). This DTO will be heavily commented with `xmldoc` for schema generation.

2.  **ApiService Orchestration Consumer / Endpoint Logic:** Logic within the `ApiService` project that initiates the creation process.
    *   **Responsibility:** To act as the secure gateway and initial business rule orchestrator.
    *   **Action:** It publishes a new, highly specific command to the `Storefront` domain (e.g., `CreateStockedSellableItemCommand` or `CreateOnDemandSellableItemCommand`).

3.  **Storefront Factory Consumer:** A MassTransit consumer within the `Storefront` project.
    *   **Responsibility:** To act on the specific command from the `ApiService`.
    *   **Action:** It constructs the full `SellableItem` document, including the correct payload, and persists it to the `Storefront`'s database. It then publishes the final `SellableItemCreated` event.

4.  **Storefront Query/Command Services (Optional):** If the new type requires unique write operations (e.g., "redeem gift card"), a dedicated service or endpoint in the `Storefront` API would be part of the plugin.

5.  **Frontend UI Component:** A dedicated component (e.g., React's `<StockedProductDetails>`) that knows how to render the specific payload for this type. A central "component mapper" will dynamically render the correct component based on the `type` field.

##### C. Example Workflows

**Workflow 1: For a `Catalog`-Backed `StockedProduct`**

1.  **Event:** The `Catalog` domain publishes a `PhysicalProductCreated` event.
2.  **Orchestration (`ApiService`):** The `StockedProductOrchestratorConsumer` receives the event and publishes a `CreateStockedSellableItemCommand`.
3.  **Creation (`Storefront`):** The `SellableItemFactoryConsumer` receives the command, constructs the `SellableItem` document with `type: "StockedProduct"`, and saves it.
4.  **Notification:** The `Storefront` publishes a `SellableItemCreated` event.
5.  **Rendering (Frontend):** The frontend fetches the document and renders the appropriate component.

**Workflow 2: For a `Storefront`-Native `OnDemandProduct`**

1.  **Command Initiation:** An admin uses a backoffice UI, which sends a request to the `ApiService`.
2.  **Gateway (`ApiService`):** The `ApiService` validates the request and publishes a `CreateOnDemandSellableItemCommand`.
3.  **Creation (`Storefront`):** The `SellableItemFactoryConsumer` receives the command, constructs the `SellableItem` document with `type: "OnDemandProduct"`, and saves it.
4.  **Notification & Rendering:** The process continues as above.

##### D. Cross-Domain Validation Pattern

To prevent conceptual leakage between domains (e.g., the `Storefront` domain should not know about "Carts"), a formal asynchronous validation pattern will be used.

1.  **Internal Logic:** The `SellableItemEntity` and its associated `IPayloadHandler` will expose a domain-neutral method: `ValidateQuantityRequest(object payload, int quantity)`. This method's sole responsibility is to determine if a given quantity can be fulfilled based on the item's internal state and rules (e.g., stock level, backorder policy).

2.  **Cross-Domain Communication:** When another domain (e.g., `Order`) needs to perform this validation, it will use a **Command/Response** message pattern:
    *   The `Order` saga publishes a `ValidateSellableItemQuantityCommand` containing the `SellableItemId`, `Quantity`, and a `CorrelationId`.
    *   A consumer in the `Storefront` domain receives this command, loads the `SellableItemEntity`, and invokes the internal `ValidateQuantityRequest` logic.
    *   The `Storefront` consumer then publishes a `SellableItemQuantityValidated` event containing the result (success/failure) and the original `CorrelationId`.
    *   The `Order` saga listens for this response event to continue its workflow.

This pattern ensures that the validation logic remains encapsulated within the `Storefront` domain, which is the source of truth for sellable item rules, while maintaining loose coupling and asynchronous communication between domains.

#### 5. Consequences

*   **Standardized Workflow:** The "plugin" concept establishes a clear, predictable process for developers to add new product types.
*   **Coordinated Changes:** Adding a new type requires coordinated creation of components across the `ApiService`, `Storefront`, and frontend projects.
*   **Increased Initial Setup:** The initial implementation of the factory/consumer infrastructure is more complex than a single, rigid model, but provides significant long-term flexibility.
*   **Discoverability:** A central registry or clear naming convention will be essential for developers to easily discover the components associated with each `SellableItem` type.
*   **Enforced Boundaries:** The asynchronous validation pattern reinforces strict domain boundaries, preventing business logic from leaking between contexts.