# Contract Testing Documentation

Comprehensive guide for implementing Pact consumer-driven contract testing between KbStore (backend) and KbClient (frontend).

---

## Documentation Overview

This documentation suite consists of three complementary documents:

### 1. [CONTRACT-TESTING-STRATEGY.md](./CONTRACT-TESTING-STRATEGY.md) (56KB)

**Purpose**: High-level architecture and strategic decisions

**Contents**:
- Pact consumer-driven contract testing overview
- Architecture and integration points
- Full SDK vs Types-only approach comparison (**Recommendation: Types Only**)
- CI/CD integration strategy
- Developer workflow
- Migration strategy (12-week timeline)
- Versioning and breaking change management

**Read this first** to understand the "why" and "what" before implementation.

---

### 2. [CONTRACT-TESTING-WORKFLOWS.md](./CONTRACT-TESTING-WORKFLOWS.md) (22KB)

**Purpose**: Visual workflow diagrams and process flows

**Contents**:
- Mermaid diagrams for all key workflows
- Consumer test workflow (frontend)
- Provider verification workflow (backend)
- Type generation flow
- Breaking change handling
- CI/CD integration points
- Migration timeline (Gantt chart)
- Compatibility matrix examples
- Developer journey maps

**Use this** for visual references and quick workflow lookups.

---

### 3. [CONTRACT-TESTING-IMPLEMENTATION-GUIDE.md](./CONTRACT-TESTING-IMPLEMENTATION-GUIDE.md) (44KB)

**Purpose**: Step-by-step implementation with code examples

**Contents**:
- Setup checklist (3 phases)
- Backend implementation (PactNet)
  - OpenAPI configuration
  - Provider state middleware
  - Provider verification tests
- Frontend implementation (Pact JS)
  - Consumer test setup
  - Type generation
  - Service layer implementation
- Common patterns (pagination, errors, optional fields)
- Troubleshooting guide
- Best practices

**Use this** for hands-on implementation - copy/paste ready code examples.

---

## Quick Start

### For Decision Makers

1. Read: [CONTRACT-TESTING-STRATEGY.md](./CONTRACT-TESTING-STRATEGY.md) (Executive Summary + Approach Comparison)
2. Review: [CONTRACT-TESTING-WORKFLOWS.md](./CONTRACT-TESTING-WORKFLOWS.md) (Migration Timeline + Cost-Benefit)
3. Decide: Approve 12-week migration plan

**Key Decision**: Use **Types-Only Approach (Approach B)** for:
- Minimal bundle size (~5KB vs 50-200KB)
- Full control over HTTP implementation
- Better alignment with existing frontend architecture

---

### For Backend Engineers

1. Read: [CONTRACT-TESTING-STRATEGY.md](./CONTRACT-TESTING-STRATEGY.md) (Section: Provider-Side Pact Verification)
2. Reference: [CONTRACT-TESTING-WORKFLOWS.md](./CONTRACT-TESTING-WORKFLOWS.md) (Provider Verification Workflow)
3. Implement: [CONTRACT-TESTING-IMPLEMENTATION-GUIDE.md](./CONTRACT-TESTING-IMPLEMENTATION-GUIDE.md) (Backend Implementation)

**First Task**: Set up provider verification test for one existing endpoint (POC)

---

### For Frontend Engineers

1. Read: [CONTRACT-TESTING-STRATEGY.md](./CONTRACT-TESTING-STRATEGY.md) (Section: Consumer-Side Pact Tests)
2. Reference: [CONTRACT-TESTING-WORKFLOWS.md](./CONTRACT-TESTING-WORKFLOWS.md) (Consumer Test Workflow)
3. Implement: [CONTRACT-TESTING-IMPLEMENTATION-GUIDE.md](./CONTRACT-TESTING-IMPLEMENTATION-GUIDE.md) (Frontend Implementation)

**First Task**: Write consumer test for one existing endpoint (POC)

---

### For DevOps Engineers

1. Read: [CONTRACT-TESTING-STRATEGY.md](./CONTRACT-TESTING-STRATEGY.md) (Section: CI/CD Integration Strategy)
2. Reference: [CONTRACT-TESTING-WORKFLOWS.md](./CONTRACT-TESTING-WORKFLOWS.md) (CI/CD Integration Points)
3. Implement:
   - Deploy Pact Broker (Docker Compose or PactFlow SaaS)
   - Create GitHub Actions workflows
   - Configure webhooks

**First Task**: Deploy Pact Broker locally/staging

---

## Implementation Timeline

```
Week 1-2:   Infrastructure + POC
Week 3-6:   Convert 50% of endpoints
Week 7-10:  Convert remaining endpoints + delete mocks
Week 11-12: Optimization + team training
```

**Proof of Concept Success Criteria** (Week 2):
- ✅ Pact Broker running
- ✅ Backend provider verification test passes
- ✅ Frontend consumer test passes
- ✅ Contract published and verified
- ✅ Types generated from OpenAPI

---

## Key Benefits

### Before Contract Testing (Current State)

```
❌ Frontend mocks drift from backend reality
❌ Breaking changes discovered in production
❌ Manual coordination for API changes
❌ No automated compatibility verification
❌ Frequent integration bugs
```

### After Contract Testing (Target State)

```
✅ Frontend expectations automatically verified
✅ Breaking changes caught in CI/CD
✅ Self-documenting API contracts
✅ Deployment gates prevent incompatible releases
✅ Faster, safer API evolution
```

---

## Technology Stack

### Backend (KbStore)

- **Framework**: ASP.NET 9 Minimal APIs
- **Pact Library**: PactNet 5.0
- **OpenAPI**: Microsoft.AspNetCore.OpenApi
- **Test Framework**: NUnit 4.4

### Frontend (KbClient)

- **Framework**: React + TypeScript
- **Pact Library**: @pact-foundation/pact 13.1
- **Type Generator**: openapi-typescript 7.0
- **Test Framework**: Vitest 1.0

### Infrastructure

- **Pact Broker**: PactFlow (self-hosted or SaaS)
- **CI/CD**: GitHub Actions
- **Databases**: PostgreSQL (contracts), In-memory (tests)

---

## Key Decisions

### ✅ Approach B: Types-Only Generation (RECOMMENDED)

**Rationale**:
1. Frontend already building custom service layer
2. Need flexibility for auth, retries, interceptors
3. Minimal bundle impact (~5KB vs 50-200KB)
4. Better integration with existing patterns

**Implementation**:
- Generate TypeScript types from OpenAPI spec
- Frontend writes service layer using generated types
- Full type safety without SDK lock-in

### ❌ Approach A: Full SDK Generation (NOT RECOMMENDED)

**Why not**:
- Opinionated HTTP client (fetch/axios/xhr)
- Larger bundle size (~50-200KB)
- Less flexibility for custom logic
- Harder to integrate with existing architecture

---

## Success Metrics

### Phase 1 (POC - Week 2)

- [ ] 1 endpoint with contract coverage (both sides)
- [ ] CI/CD pipelines configured
- [ ] Team trained on workflow

### Phase 2 (50% Coverage - Week 6)

- [ ] 50% of API endpoints covered by contracts
- [ ] Zero production drift incidents
- [ ] < 5 minutes average contract verification time

### Phase 3 (Full Coverage - Week 10)

- [ ] 100% of API endpoints covered
- [ ] All manual mocks deleted
- [ ] Deployment gates active

### Phase 4 (Optimization - Week 12)

- [ ] Team playbooks finalized
- [ ] < 3 minutes contract verification time
- [ ] API versioning strategy documented

---

## Common Commands

### Backend

```bash
# Run provider verification
dotnet test --filter "KbStoreProviderTests"

# Export OpenAPI spec
curl http://localhost:5000/openapi/v1.json > openapi.json

# Check deployment eligibility
docker run pactfoundation/pact-cli broker can-i-deploy \
  --pacticipant=KbStore --version="1.0.0" --to-environment=production
```

### Frontend

```bash
# Generate types
npm run generate:types

# Run consumer tests
npm run test:contract

# Publish contracts
npm run pact:publish

# Check deployment eligibility
npx pact-broker can-i-deploy \
  --pacticipant=KbClient --version="1.0.0" --to-environment=production
```

---

## Troubleshooting Quick Reference

| Issue | Document | Section |
|-------|----------|---------|
| Provider state setup fails | [Implementation Guide](./CONTRACT-TESTING-IMPLEMENTATION-GUIDE.md) | Troubleshooting > Issue 1 |
| Type mismatch in contract | [Implementation Guide](./CONTRACT-TESTING-IMPLEMENTATION-GUIDE.md) | Troubleshooting > Issue 2 |
| Pact Broker connection fails | [Implementation Guide](./CONTRACT-TESTING-IMPLEMENTATION-GUIDE.md) | Troubleshooting > Issue 3 |
| Generated types don't match | [Implementation Guide](./CONTRACT-TESTING-IMPLEMENTATION-GUIDE.md) | Troubleshooting > Issue 4 |
| Breaking change detected | [Strategy](./CONTRACT-TESTING-STRATEGY.md) | Versioning & Breaking Changes |
| CI/CD pipeline failures | [Strategy](./CONTRACT-TESTING-STRATEGY.md) | CI/CD Integration Strategy |

---

## Resources

### Internal Documentation

- [CONTRACT-TESTING-STRATEGY.md](./CONTRACT-TESTING-STRATEGY.md) - Architecture and strategy
- [CONTRACT-TESTING-WORKFLOWS.md](./CONTRACT-TESTING-WORKFLOWS.md) - Visual workflows
- [CONTRACT-TESTING-IMPLEMENTATION-GUIDE.md](./CONTRACT-TESTING-IMPLEMENTATION-GUIDE.md) - Code examples

### External Resources

- **Pact Documentation**: https://docs.pact.io
- **PactNet (C#)**: https://github.com/pact-foundation/pact-net
- **Pact JS**: https://github.com/pact-foundation/pact-js
- **OpenAPI TypeScript**: https://github.com/drwpow/openapi-typescript
- **Pact Broker**: https://github.com/pact-foundation/pact_broker
- **PactFlow (SaaS)**: https://pactflow.io

---

## FAQ

### Q: Do we need to test every single endpoint?

**A**: Prioritize critical user journeys first. Aim for 80% coverage of business-critical paths, then expand. Not every internal endpoint needs contract tests.

---

### Q: What if frontend and backend teams work on different timelines?

**A**: This is the beauty of Pact! Frontend can define contracts first (expectations), then backend implements to satisfy them. Or vice versa. Teams can work independently.

---

### Q: How do we handle authentication in contract tests?

**A**: Provider state setup can create authenticated sessions. Consumer tests can include auth headers in interactions. See Implementation Guide > Common Patterns.

---

### Q: What's the difference between Pact and OpenAPI?

**A**:
- **OpenAPI**: Documents what the API *can* do (provider-driven)
- **Pact**: Verifies what the frontend *actually uses* (consumer-driven)

Use both! OpenAPI generates types, Pact prevents drift.

---

### Q: Can we use Pact with GraphQL?

**A**: Yes, but this guide focuses on REST APIs. See Pact documentation for GraphQL-specific patterns.

---

### Q: What if we have multiple frontend applications?

**A**: Each consumer publishes separate contracts. Pact Broker tracks compatibility matrix across all consumers. Backend must satisfy all consumer expectations.

---

### Q: How do we version APIs with Pact?

**A**: See [CONTRACT-TESTING-STRATEGY.md](./CONTRACT-TESTING-STRATEGY.md) > Versioning & Breaking Changes. TLDR: Use semantic versioning + compatibility matrix.

---

## Next Steps

1. **Week 1**: Schedule team kickoff meeting
2. **Week 1**: DevOps deploys Pact Broker
3. **Week 2**: Backend + Frontend implement POC (1 endpoint each)
4. **Week 2**: Review POC results, adjust strategy if needed
5. **Week 3+**: Begin incremental migration

**Questions?** Reach out to the team in #contract-testing channel (Slack/Teams).

---

**Document Version**: 1.0
**Last Updated**: 2025-01-19
**Maintained By**: Engineering Team
**Status**: Draft - Pending Team Review
