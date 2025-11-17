# Implementation Status

This document tracks the current implementation status of KbStore components. For architectural documentation, see [CLAUDE.md](../CLAUDE.md).

## Current Phase: Storefront Domain Foundation

**Last Updated**: 2025-11-17

### Component Status

| Component | Status | Details |
|-----------|--------|---------|
| **Catalog Domain** | FULLY IMPLEMENTED | Product and Inventory state machines, command/query services, PostgreSQL persistence, comprehensive tests |
| **Storefront Domain** | DOMAIN COMPLETE | SellableItem state machine, command/query services, MongoDB persistence, 9+ domain tests, interface-based contracts |
| **ApiService HTTP Gateway** | PARTIAL | Catalog endpoints fully functional, Storefront endpoints not created yet |
| **ApiService Orchestration** | PLACEHOLDER CONSUMERS | Consumer classes exist with basic orchestration, but limited cross-domain logic |
| **Authentication/Authorization** | NOT IMPLEMENTED | Wide open, planned for future |
| **Aspire Orchestration** | FULLY IMPLEMENTED | RabbitMQ, PostgreSQL, MongoDB all configured and functional |

### What Works Now

**Catalog Domain:**
- Create, update, query, and delete Products via HTTP API
- Create, modify, and track Inventory via HTTP API
- Product and Inventory state machines with full lifecycle management
- Event publishing from Catalog domain
- Automated database migrations for Catalog domain
- Comprehensive test coverage

**Storefront Domain:**
- SellableItem state machine with Draft → Published → Hidden/Discontinued lifecycle
- Command service: Create, Update (Name/Description/Price/Payload), Publish, Hide, Discontinue, Reinstate, Delete
- Query service: GetById, GetBySku, GetAll, GetByItemType, GetPublished
- Interface-based contracts matching MassTransit idioms with polymorphic subscription support
- DateTimeOffset timestamps (CreatedOn/UpdatedOn)
- Deterministic GUID generation from SKU for cross-domain correlation
- 9 comprehensive domain tests

**Infrastructure:**
- .NET Aspire orchestration with RabbitMQ, PostgreSQL, MongoDB
- PgWeb UI for PostgreSQL management
- MongoExpress UI for MongoDB management
- Central Package Management via Directory.Packages.props

### What Doesn't Work Yet

**Storefront:**
- HTTP API endpoints for SellableItems (contracts exist, endpoints not wired up)
- Service registration in ApiService DI container (services exist, not injected yet)

**Cross-Domain Orchestration:**
- Event consumers have placeholder logic but limited actual orchestration
- No active cross-domain workflows (e.g., Catalog Product → Storefront SellableItem synchronization)

**Features:**
- Customer-facing product listings, pricing, or cart operations
- Authentication or authorization on API endpoints
- Background job processing (Hangfire configured but not utilized)

### Phase History

#### Phase 0: MongoDB Foundation (COMPLETE)
- Catalog domain fully implemented
- Storefront infrastructure configured
- Aspire orchestration working

#### Phase 1: Storefront Domain (IN PROGRESS)
- SellableItem state machine DONE
- Command/Query services DONE
- Interface-based contracts DONE
- HTTP API endpoints TODO
- DI registration TODO

#### Phase 2: Cross-Domain Orchestration (PLANNED)
- Active event consumers
- Catalog → Storefront synchronization
- Deterministic GUID correlation

#### Future Phases
- Authentication/Authorization
- Customer cart and checkout workflows
- Advanced pricing and promotions
- Background job scheduling

### Recent Changes

- **2025-11-17**: Converted Storefront contracts from records to interface hierarchies
- **2025-11-17**: Added DateTimeOffset timestamps, renamed CreatedAt/UpdatedAt to CreatedOn/UpdatedOn
- **2025-11-17**: Applied [ExcludeFromTopology] for proper MassTransit routing

For detailed change history, see [CHANGELOG.md](../CHANGELOG.md) and [changelogs/](../changelogs/).
