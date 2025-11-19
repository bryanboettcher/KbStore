# Contract Testing Workflow Diagrams

Supplementary visual guide for CONTRACT-TESTING-STRATEGY.md

---

## Quick Reference: Pact Workflow

```mermaid
flowchart TB
    Start([Developer Makes API Change])

    subgraph Frontend["Frontend (Consumer)"]
        FE1[Write Pact Test]
        FE2[Run npm run test:contract]
        FE3[Generate Contract JSON]
        FE4[Publish to Pact Broker]
    end

    subgraph Broker["Pact Broker"]
        B1[Store Contract]
        B2[Trigger Webhook]
    end

    subgraph Backend["Backend (Provider)"]
        BE1[Receive Notification]
        BE2[Fetch Contract from Broker]
        BE3[Run Provider Verification]
        BE4{Tests Pass?}
        BE5[Publish Success]
        BE6[Publish Failure]
    end

    subgraph Deployment["Deployment Gate"]
        D1{Can Deploy?}
        D2[✅ Deploy]
        D3[❌ Block Deploy]
    end

    Start --> FE1
    FE1 --> FE2
    FE2 --> FE3
    FE3 --> FE4
    FE4 --> B1
    B1 --> B2
    B2 --> BE1
    BE1 --> BE2
    BE2 --> BE3
    BE3 --> BE4
    BE4 -->|Yes| BE5
    BE4 -->|No| BE6
    BE5 --> D1
    BE6 --> D1
    D1 -->|All Contracts Valid| D2
    D1 -->|Breaking Changes| D3

    style BE4 fill:#ffe1e1
    style D1 fill:#ffe1e1
    style D2 fill:#e1ffe1
    style D3 fill:#ffe1e1
```

---

## Detailed: Consumer Test Workflow (Frontend)

```mermaid
sequenceDiagram
    autonumber
    participant Dev as Developer
    participant Test as Pact Test Suite
    participant Mock as Pact Mock Server
    participant Contract as Contract File
    participant Broker as Pact Broker
    participant CI as CI/CD Pipeline

    Dev->>Test: Write new feature test
    Test->>Mock: Configure expected interaction
    Note over Mock: Mock starts on localhost:9000
    Test->>Mock: Send HTTP request
    Mock->>Test: Return mocked response
    Test->>Test: Assert response matches expectations
    Test->>Contract: Generate pact JSON file

    Dev->>CI: Push code to git
    CI->>Test: Run npm run test:contract
    Test->>Contract: Regenerate pacts
    CI->>Broker: Publish contracts (if tests pass)
    Broker->>Broker: Store with version tag
    Broker->>CI: Trigger backend verification (webhook)
```

---

## Detailed: Provider Verification Workflow (Backend)

```mermaid
sequenceDiagram
    autonumber
    participant Broker as Pact Broker
    participant CI as Backend CI/CD
    participant Test as Provider Test
    participant API as Test API Server
    participant State as Provider State Setup
    participant DB as Test Database

    Broker->>CI: Webhook: New contract published
    CI->>Test: Run dotnet test (Provider)
    Test->>Broker: Fetch all consumer contracts
    Broker->>Test: Return contracts (all branches)

    loop For each interaction
        Test->>State: Setup provider state
        State->>DB: Seed test data
        DB->>State: Confirm data ready
        Test->>API: Replay HTTP request from contract
        API->>API: Execute real endpoint logic
        API->>Test: Return actual response
        Test->>Test: Compare actual vs contract

        alt Match
            Test->>Test: ✅ Interaction verified
        else Mismatch
            Test->>Test: ❌ Verification failed
            Test->>CI: Fail build
        end
    end

    Test->>Broker: Publish verification results
    Broker->>Broker: Update compatibility matrix
```

---

## Type Generation Flow

```mermaid
flowchart LR
    subgraph Backend["KbStore Backend"]
        A1[ASP.NET Minimal APIs]
        A2[Endpoint Annotations]
        A3[OpenAPI Generator]
        A4[openapi.json Spec]
    end

    subgraph Artifacts["Artifact Storage"]
        B1[Published openapi.json]
    end

    subgraph Frontend["KbClient Frontend"]
        C1[openapi-typescript CLI]
        C2[Generated api.d.ts]
        C3[Service Layer]
        C4[React Components]
    end

    A1 --> A2
    A2 --> A3
    A3 --> A4
    A4 --> B1
    B1 --> C1
    C1 --> C2
    C2 --> C3
    C3 --> C4

    style A4 fill:#e1f5ff
    style C2 fill:#e1ffe1
```

**Trigger Points**:
- **Manual**: `npm run generate:types` (dev)
- **Automated**: CI/CD on backend deployment
- **Scheduled**: Nightly cron job to catch drift

---

## Breaking Change Handling

```mermaid
stateDiagram-v2
    [*] --> BackendChange: Developer modifies API

    BackendChange --> ProviderTest: Run provider verification

    ProviderTest --> Compatible: All contracts pass
    ProviderTest --> Breaking: Some contracts fail

    Compatible --> Deploy: ✅ Safe to deploy
    Deploy --> [*]

    Breaking --> AnalyzeImpact: Review failing contracts

    AnalyzeImpact --> MinorImpact: Only old branches affected
    AnalyzeImpact --> MajorImpact: Production/main affected

    MinorImpact --> Deploy: ✅ Safe to deploy

    MajorImpact --> ChooseStrategy: Decide approach

    ChooseStrategy --> VersionAPI: Add v2 endpoint
    ChooseStrategy --> CoordinatedDeploy: Sync with frontend
    ChooseStrategy --> Revert: Roll back changes

    VersionAPI --> UpdateContracts: Frontend updates to v2
    UpdateContracts --> ProviderTest

    CoordinatedDeploy --> FrontendUpdate: Frontend adapts to change
    FrontendUpdate --> ProviderTest

    Revert --> [*]
```

---

## CI/CD Integration Points

```mermaid
graph TB
    subgraph "Pull Request (Frontend)"
        PR1[Code Changes]
        PR2[Run Unit Tests]
        PR3[Run Pact Consumer Tests]
        PR4[Publish Contracts to Broker]
        PR5{Can Merge?}
    end

    subgraph "Pull Request (Backend)"
        PR6[Code Changes]
        PR7[Run Unit Tests]
        PR8[Run Provider Verification]
        PR9{Can Merge?}
    end

    subgraph "Pact Broker"
        B1[Contract Repository]
        B2[Verification Status]
        B3[Compatibility Matrix]
    end

    subgraph "Deployment Pipeline"
        D1{Can Deploy Frontend?}
        D2{Can Deploy Backend?}
        D3[Deploy Frontend]
        D4[Deploy Backend]
        D5[Record Deployment]
    end

    PR1 --> PR2
    PR2 --> PR3
    PR3 --> PR4
    PR4 --> B1
    B1 --> PR5
    PR5 -->|Yes| D1
    PR5 -->|No| PR1

    PR6 --> PR7
    PR7 --> PR8
    PR8 --> B2
    B2 --> PR9
    PR9 -->|Yes| D2
    PR9 -->|No| PR6

    D1 -->|Check Matrix| B3
    D2 -->|Check Matrix| B3

    B3 -->|Compatible| D3
    B3 -->|Compatible| D4
    B3 -->|Incompatible| PR1
    B3 -->|Incompatible| PR6

    D3 --> D5
    D4 --> D5
    D5 --> B3

    style PR5 fill:#ffe1e1
    style PR9 fill:#ffe1e1
    style D1 fill:#ffe1e1
    style D2 fill:#ffe1e1
    style B3 fill:#e1ffe1
```

---

## Comparison: With vs Without Pact

### Without Pact (Current State)

```mermaid
graph TB
    subgraph Frontend
        F1[Manual Mocks]
        F2[Frontend Tests Pass ✅]
        F3[Deploy Frontend]
    end

    subgraph Backend
        B1[API Implementation]
        B2[Backend Tests Pass ✅]
        B3[Deploy Backend]
    end

    subgraph Production
        P1[Integration]
        P2{Runtime Compatibility?}
        P3[❌ 500 Error: Field Renamed]
        P4[🔥 Incident: API Broken]
    end

    F1 -.->|Assumes structure| F2
    F2 --> F3
    B1 --> B2
    B2 --> B3
    F3 --> P1
    B3 --> P1
    P1 --> P2
    P2 -->|Mocks were wrong| P3
    P3 --> P4

    style P3 fill:#ff0000,color:#fff
    style P4 fill:#ff0000,color:#fff
```

**Problem**: Drift discovered in production.

---

### With Pact (Proposed State)

```mermaid
graph TB
    subgraph Frontend
        F1[Pact Consumer Tests]
        F2[Generate Contracts]
        F3[Publish to Broker]
    end

    subgraph Backend
        B1[API Implementation]
        B2[Fetch Contracts]
        B3[Provider Verification]
        B4{Verification Passed?}
    end

    subgraph CI/CD
        C1{Compatible?}
        C2[✅ Deploy Both]
        C3[❌ Block Deploy]
        C4[Alert Team]
    end

    subgraph Production
        P1[Integration]
        P2[✅ Works Perfectly]
    end

    F1 --> F2
    F2 --> F3
    B1 --> B2
    B2 --> B3
    B3 --> B4
    B4 -->|Yes| C1
    B4 -->|No| C3
    C1 -->|All versions compatible| C2
    C1 -->|Breaking change| C3
    C3 --> C4
    C2 --> P1
    P1 --> P2

    style B4 fill:#ffe1e1
    style C1 fill:#ffe1e1
    style P2 fill:#00ff00
```

**Result**: Drift caught before deployment.

---

## Developer Experience: Daily Workflow

### Frontend Developer Journey

```mermaid
journey
    title Frontend Developer: Adding New Feature
    section Morning
      Pull latest code: 5: Frontend Dev
      Run npm install: 4: Frontend Dev
      Review OpenAPI types: 5: Frontend Dev
    section Development
      Write React component: 5: Frontend Dev
      Write Pact consumer test: 3: Frontend Dev
      Run test (fails initially): 2: Frontend Dev
      Fix interaction expectations: 4: Frontend Dev
      Run test (passes): 5: Frontend Dev
      Publish contract: 5: CI Pipeline
    section Backend Notification
      Backend CI triggered: 5: Webhook
      Provider tests run: 4: Backend CI
      Tests pass: 5: Backend CI
    section Integration
      Pull latest types: 5: Frontend Dev
      Implement service layer: 5: Frontend Dev
      Manual testing: 4: Frontend Dev
      Open PR: 5: Frontend Dev
      CI verifies contracts: 5: CI Pipeline
      PR approved: 5: Team
```

---

### Backend Developer Journey

```mermaid
journey
    title Backend Developer: Receiving Contract Request
    section Notification
      Webhook fires: 5: Pact Broker
      Check Pact Broker dashboard: 4: Backend Dev
      Review new contract: 4: Backend Dev
    section Implementation
      Create endpoint: 5: Backend Dev
      Add provider state setup: 3: Backend Dev
      Run provider test (fails): 2: Backend Dev
      Fix endpoint logic: 4: Backend Dev
      Run provider test (passes): 5: Backend Dev
    section Verification
      Push code: 5: Backend Dev
      CI runs all provider tests: 5: CI Pipeline
      Publish verification result: 5: CI Pipeline
      Update OpenAPI spec: 4: CI Pipeline
    section Communication
      Notify frontend team: 5: Backend Dev
      Frontend pulls new types: 5: Frontend Dev
```

---

## Migration Timeline

```mermaid
gantt
    title Contract Testing Migration (12 Weeks)
    dateFormat  YYYY-MM-DD
    axisFormat  Week %U

    section Infrastructure
    Deploy Pact Broker           :done, infra1, 2025-01-20, 3d
    Configure OpenAPI generation :done, infra2, 2025-01-20, 2d
    Setup CI/CD pipelines        :active, infra3, 2025-01-23, 5d

    section Backend
    Install PactNet              :done, be1, 2025-01-20, 1d
    Write provider state setup   :active, be2, 2025-01-22, 4d
    First provider test (POC)    :be3, 2025-01-26, 3d
    Full provider coverage       :be4, 2025-02-03, 14d

    section Frontend
    Install Pact dependencies    :done, fe1, 2025-01-20, 1d
    Generate initial types       :done, fe2, 2025-01-21, 1d
    Pact test infrastructure     :active, fe3, 2025-01-23, 4d
    First consumer test (POC)    :fe4, 2025-01-27, 3d
    Convert 50% endpoints        :fe5, 2025-02-03, 21d
    Convert remaining endpoints  :fe6, 2025-02-24, 14d
    Delete manual mocks          :fe7, 2025-03-10, 3d

    section Testing & Rollout
    Proof-of-concept validation  :milestone, poc, 2025-01-30, 0d
    50% coverage milestone       :milestone, m50, 2025-02-21, 0d
    100% coverage milestone      :milestone, m100, 2025-03-14, 0d
    Production deployment        :milestone, prod, 2025-03-21, 0d

    section Governance
    Team training                :gov1, 2025-03-10, 7d
    Documentation finalization   :gov2, 2025-03-14, 5d
    Performance optimization     :gov3, 2025-03-17, 5d
```

---

## Contract Compatibility Matrix Example

### Visual Representation

```mermaid
graph TD
    subgraph "KbClient Versions"
        C1[v1.2.3 main]
        C2[v1.3.0-rc.1 develop]
        C3[v2.0.0-alpha feature/redesign]
    end

    subgraph "KbStore Versions"
        S1[v1.5.0 production]
        S2[v1.6.0-beta staging]
        S3[v2.0.0-alpha feature/graphql]
    end

    C1 -->|✅| S1
    C1 -->|✅| S2
    C1 -->|❌| S3

    C2 -->|✅| S1
    C2 -->|⚠️| S2
    C2 -->|❌| S3

    C3 -->|❌| S1
    C3 -->|❌| S2
    C3 -->|⚠️| S3

    style C1 fill:#e1ffe1
    style S1 fill:#e1ffe1
    linkStyle 0 stroke:#00ff00,stroke-width:3px
    linkStyle 1 stroke:#00ff00,stroke-width:3px
    linkStyle 2 stroke:#ff0000,stroke-width:3px
```

**Legend**:
- ✅ **Compatible**: Safe to deploy together
- ⚠️ **Pending**: Verification in progress
- ❌ **Incompatible**: Breaking changes detected

---

## Error Recovery Flow

```mermaid
flowchart TB
    Start([Provider Verification Fails])

    A{Error Type?}

    A -->|Status Code Mismatch| B1[Backend: Fix response code]
    A -->|Missing Field| B2[Backend: Add missing field]
    A -->|Field Type Mismatch| B3[Backend: Fix serialization]
    A -->|Provider State Failure| B4[Backend: Fix state setup]
    A -->|Timeout| B5[Backend: Optimize performance]

    B1 --> C[Run provider test locally]
    B2 --> C
    B3 --> C
    B4 --> C
    B5 --> C

    C --> D{Passes?}

    D -->|Yes| E[Push fix]
    D -->|No| F{Breaking Change?}

    F -->|Yes| G[Coordinate with frontend]
    F -->|No| C

    G --> H[Choose strategy]

    H --> H1[Version API]
    H --> H2[Update consumer contract]
    H --> H3[Revert backend change]

    H1 --> E
    H2 --> I[Frontend updates contract]
    H3 --> E

    I --> J[Publish new contract]
    J --> C

    E --> K[CI runs verification]
    K --> L{All contracts pass?}

    L -->|Yes| M[✅ Deploy]
    L -->|No| Start

    M --> N([End])

    style D fill:#ffe1e1
    style L fill:#ffe1e1
    style M fill:#e1ffe1
```

---

## Webhook Configuration Flow

```mermaid
sequenceDiagram
    autonumber
    participant Admin as DevOps Admin
    participant Broker as Pact Broker
    participant GitHub as GitHub Actions
    participant Backend as Backend Repo

    Admin->>Broker: Create webhook config
    Note over Broker: Trigger: Contract Published<br/>Consumer: KbClient<br/>Provider: KbStore

    Broker->>Broker: Save webhook configuration

    Note over Frontend,Broker: Later: Frontend publishes contract

    Frontend->>Broker: POST /pacts (new contract)
    Broker->>Broker: Store contract
    Broker->>Broker: Evaluate webhook triggers
    Broker->>GitHub: POST /repos/org/KbStore/dispatches
    Note over GitHub: { "event_type": "pact_changed" }

    GitHub->>Backend: Trigger workflow
    Backend->>Backend: Run provider verification
    Backend->>Broker: POST /verification-results
    Broker->>Broker: Update compatibility matrix
```

---

## Pact Matchers Example

### Type Matching (Flexible)

```typescript
// Consumer test uses matchers for flexibility
import { Matchers } from '@pact-foundation/pact';

await provider.addInteraction({
  willRespondWith: {
    body: {
      productId: Matchers.uuid(),           // Any valid UUID
      sku: Matchers.like('WIDGET-001'),     // Any string
      name: Matchers.like('Widget'),        // Any string
      quantity: Matchers.integer(100),      // Any integer
      price: Matchers.decimal(9.99),        // Any decimal
      isAvailable: Matchers.boolean(true),  // Any boolean
      createdOn: Matchers.iso8601DateTime(), // Any ISO datetime
    }
  }
});
```

**Backend verification**: Must return response with **correct types**, but **values can differ**.

---

### Exact Matching (Strict)

```typescript
// When exact values matter (e.g., specific error codes)
await provider.addInteraction({
  willRespondWith: {
    status: 400, // Exact status code required
    body: {
      type: 'https://tools.ietf.org/html/rfc7231#section-6.5.1',
      title: 'Bad Request', // Exact string required
      status: 400,
    }
  }
});
```

---

## Cost-Benefit Analysis

### Initial Investment (Weeks 1-4)

```mermaid
pie title Time Investment Breakdown
    "Pact Broker Setup" : 8
    "Backend Infrastructure" : 16
    "Frontend Infrastructure" : 16
    "Team Training" : 12
    "Documentation" : 8
```

**Total**: ~60 hours (~1.5 weeks for team of 4)

---

### Long-Term Savings (Per Year)

```mermaid
pie title Time Saved Annually
    "Reduced Production Incidents" : 40
    "Faster API Changes" : 60
    "Eliminated Manual Testing" : 80
    "Less Debugging" : 50
    "Improved Onboarding" : 20
```

**Total**: ~250 hours saved/year (~6 weeks)

**ROI**: Break-even in ~3 months

---

## Monitoring Dashboard (Pact Broker)

### Example Metrics View

```
┌─────────────────────────────────────────────────────────┐
│ Pact Broker Dashboard                                   │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ ✅ Overall Health: HEALTHY                              │
│                                                         │
│ Consumer: KbClient                                      │
│ Provider: KbStore                                       │
│ Latest Contract: v1.3.0 (2025-01-19 14:32)              │
│ Verification: ✅ PASSED (15 interactions)                │
│                                                         │
│ ┌───────────────────────────────────────────────────┐  │
│ │ Compatibility Matrix                              │  │
│ ├───────────────────────────────────────────────────┤  │
│ │          KbStore v1.5.0  v1.6.0-beta  v2.0.0-alpha│  │
│ │ KbClient                                          │  │
│ │   v1.2.3       ✅            ✅            ❌      │  │
│ │   v1.3.0       ✅            ⚠️            ❌      │  │
│ │   v2.0.0       ❌            ❌            ⚠️      │  │
│ └───────────────────────────────────────────────────┘  │
│                                                         │
│ Recent Verifications:                                   │
│   • 2025-01-19 14:35 - KbStore v1.6.0-beta - ✅ PASSED  │
│   • 2025-01-19 12:21 - KbStore v1.5.0 - ✅ PASSED       │
│   • 2025-01-19 09:15 - KbStore v2.0.0-alpha - ❌ FAILED │
│                                                         │
│ Pending Deployments:                                    │
│   • KbClient v1.3.0 → staging ⚠️ (awaiting KbStore)    │
│   • KbStore v1.6.0-beta → production ✅ (can deploy)    │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## Quick Command Reference

### Frontend Commands

```bash
# Generate TypeScript types from OpenAPI
npm run generate:types

# Run Pact consumer tests
npm run test:contract

# Publish contracts to broker
npm run pact:publish

# Check if version can be deployed
npx pact-broker can-i-deploy \
  --pacticipant=KbClient \
  --version="1.3.0" \
  --to-environment=production

# Record deployment
npx pact-broker record-deployment \
  --pacticipant=KbClient \
  --version="1.3.0" \
  --environment=production
```

---

### Backend Commands

```bash
# Run provider verification tests
dotnet test --filter "KbStoreProviderTests"

# Export OpenAPI spec
curl http://localhost:5000/openapi/v1.json > openapi.json

# Check if version can be deployed
docker run --rm pactfoundation/pact-cli:latest \
  broker can-i-deploy \
  --pacticipant=KbStore \
  --version="1.6.0" \
  --to-environment=production

# Record deployment
docker run --rm pactfoundation/pact-cli:latest \
  broker record-deployment \
  --pacticipant=KbStore \
  --version="1.6.0" \
  --environment=production
```

---

### Pact Broker Administration

```bash
# List all contracts
npx pact-broker list-latest-pact-versions

# View compatibility matrix
npx pact-broker matrix \
  --pacticipant=KbClient \
  --pacticipant=KbStore

# Create webhook
npx pact-broker create-webhook \
  "https://api.github.com/repos/org/KbStore/dispatches" \
  --consumer=KbClient \
  --provider=KbStore \
  --contract-published

# Delete old contracts (cleanup)
npx pact-broker clean \
  --pacticipant=KbClient \
  --keep-versions=10
```

---

## Summary

This workflow guide provides visual references for:

1. **Process Flows**: How Pact integrates into development
2. **Sequence Diagrams**: Step-by-step interactions
3. **State Machines**: Decision trees for error handling
4. **Timelines**: Migration planning
5. **Comparisons**: Before/after Pact adoption
6. **Developer Journeys**: Daily workflows for frontend/backend teams

Use alongside the main CONTRACT-TESTING-STRATEGY.md for implementation.

---

**Document Version**: 1.0
**Last Updated**: 2025-01-19
**See Also**: CONTRACT-TESTING-STRATEGY.md (main document)
