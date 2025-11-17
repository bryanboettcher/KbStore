# KbStore Codebase Guru - Knowledge Base Index

**Role**: Expert consultant for the KbStore project at `/mnt/c/users/bryan/source/bryanboettcher/KbStore`

**Version**: Phase 0 Complete (Catalog), Storefront Services In Progress

**Last Updated**: 2025-11-14

---

## Quick Start for Consulting Agents

### Most Frequently Needed Documents

1. **START HERE**: `quick-reference.md`
   - Fast command lookups
   - Critical file paths
   - Common patterns at a glance
   - Technology stack summary

2. **For Implementation**: `consultation-guide.md`
   - How to consult me effectively
   - Example consultation scenarios
   - Response style and decision framework
   - What to ask vs. what not to ask

3. **For Problems**: `troubleshooting.md`
   - Common issues and solutions
   - Debugging strategies
   - Diagnostic steps
   - Prevention best practices

4. **For Understanding**: `architecture-overview.md`
   - System design philosophy
   - Domain boundaries
   - Event-driven patterns
   - State machine deep dive

---

## Document Purposes

### `quick-reference.md` - Fast Lookups
**Use when you need**: Commands, paths, quick pattern reminders

**Contains**:
- Project root path
- Build, test, migration commands
- Key file locations (state machines, services, endpoints)
- Technology stack versions
- Domain status matrix
- Naming conventions
- Common pattern summaries

**Best for**: "Where is X?", "How do I run Y?", "What command for Z?"

---

### `architecture-overview.md` - Deep Understanding
**Use when you need**: Architectural context, design rationale, pattern explanation

**Contains**:
- System philosophy and principles
- High-level architecture diagrams
- Domain-Driven Design boundaries
- State machine lifecycle
- Cross-domain orchestration
- Aspire orchestration setup
- Event-driven communication flows
- Persistence patterns (PostgreSQL, MongoDB)
- Security considerations (planned)
- Scalability patterns

**Best for**: "Why is it designed this way?", "How do domains communicate?", "What's the state machine pattern?"

---

### `troubleshooting.md` - Problem Solving
**Use when you need**: Error resolution, debugging help, issue diagnosis

**Contains**:
- 10 most common issues with solutions
- State machine event correlation problems
- Database migration issues
- Event consumption failures
- Test failure patterns
- Service layer exceptions
- MongoDB issues
- API endpoint problems
- Cross-domain correlation issues
- Debugging strategies (logging, UIs, test isolation)
- Performance troubleshooting

**Best for**: "Tests are failing with X", "Events not consuming", "How do I debug Y?"

---

### `consultation-guide.md` - How to Use Me
**Use when you need**: Guidance on consulting me effectively

**Contains**:
- My role and expertise
- What I know (complete knowledge areas)
- How to consult me (effective patterns)
- Example consultation scenarios
- My decision-making framework
- When to loop me in
- Response style expectations
- Knowledge base maintenance

**Best for**: "How should I ask the Guru?", "What can the Guru help with?", "Best consultation practices"

---

## Quick Decision Tree

```
Need to...

Find a file or path?
  → quick-reference.md (Key File Locations section)

Understand why architecture works this way?
  → architecture-overview.md (Core Design Principles section)

Implement a new feature?
  → consultation-guide.md (consult with specific feature details)
  → architecture-overview.md (find similar pattern)
  → quick-reference.md (get exact file paths)

Fix an error or test failure?
  → troubleshooting.md (find error in Common Issues section)

Add a new command to state machine?
  → quick-reference.md (Common Patterns → Adding New Command)
  → Reference existing implementation in codebase

Add cross-domain event consumer?
  → architecture-overview.md (Cross-Domain Orchestration section)
  → quick-reference.md (find ProductCreatedConsumer.cs path)

Understand state machine lifecycle?
  → architecture-overview.md (State Machine Pattern Deep Dive)

Get build/test commands?
  → quick-reference.md (Critical Commands section)
```

---

## Knowledge Base Coverage

### ✅ Fully Documented
- Project structure and organization
- Catalog domain (complete implementation)
- State machine patterns
- Service layer patterns
- API endpoint patterns
- Testing infrastructure
- MassTransit configuration
- Database persistence (PostgreSQL, MongoDB)
- Aspire orchestration
- Exception handling
- Cross-domain coordination
- Common troubleshooting issues

### 🔄 Partially Documented
- Storefront domain (in progress)
- Event consumers (active but evolving)
- Query patterns (basic coverage)

### 📋 Planned for Future Documentation
- Authentication/authorization patterns (when implemented)
- Advanced querying and filtering
- Performance optimization patterns
- Deployment and DevOps patterns
- Additional domains (as they're added)

---

## File Locations in Knowledge Base

All files located in: `/home/insta/.claude/agents/kbstore-guru/`

```
kbstore-guru/
├── INDEX.md                     ← You are here
├── README.md                    ← Knowledge base overview
├── quick-reference.md           ← Fast lookups (4 pages)
├── architecture-overview.md     ← Deep dive (15 pages)
├── troubleshooting.md          ← Problem solving (10 pages)
└── consultation-guide.md        ← How to consult me (5 pages)
```

---

## Codebase Status Summary

### Implementation Status (as of 2025-11-14)

| Domain | Status | Components |
|--------|--------|------------|
| **Catalog** | ✅ COMPLETE | State machines, services, endpoints, tests |
| **Storefront** | 🔄 IN PROGRESS | State machine exists, services being implemented |
| **ApiService** | 🔄 PARTIAL | Catalog endpoints complete, consumers active |

### What Works Now
- Product CRUD operations via HTTP API
- Inventory management via HTTP API
- Product and Inventory state machines with full lifecycle
- Event publishing from Catalog domain
- Cross-domain consumers (ProductCreated, ProductNameUpdated, etc.)
- Automated database migrations
- Comprehensive test coverage for Catalog domain

### What's Being Built
- Storefront service layer implementations
- Storefront query operations
- Enhanced cross-domain orchestration

### What's Next
- Storefront HTTP endpoints
- Authentication/authorization
- Advanced querying and filtering
- Additional domains as needed

---

## Key Architectural Principles

These principles guide all decisions:

1. **Repeatable Vertical Stack** - Same pattern for every domain
2. **Pure Domain Logic** - Business rules in state machines only
3. **Service Layer Abstraction** - Hide MassTransit complexity
4. **ApiService Orchestration** - Only place for cross-domain coordination
5. **Event-Driven Communication** - Domains never call each other directly
6. **Saga-Based State Management** - Entities are state machine instances
7. **Testability First** - Pure logic, mockable infrastructure

**Golden Rule**: If you don't see a pattern in the codebase, ask the Guru before inventing a new one.

---

## Consultation Patterns

### Good Consultation Example
```
"I need to add product tags feature. Tags can be added/removed from products
and used for filtering. Should this be:
A) Properties on Product entity
B) Separate Tag state machine with ProductId references
C) Part of Storefront domain
What's the KbStore pattern for this?"
```

**Why good**: Specific feature, clear question, considers alternatives, asks for pattern.

### Improved Consultation Example
```
"I'm getting ProductNotFoundException in tests even though I call CreateAsync
first. Here's the test code: [snippet]. The saga shows in SagaHarness.Sagas
but CorrelateById throws. What's wrong with my event correlation?"
```

**Why bad**: Missing context, no diagnostic steps taken, unclear what was tried.

**Improved**:
```
"I'm getting ProductNotFoundException when testing UpdateNameAsync. I verified:
✓ Product created successfully (Response.Message.ProductId exists)
✓ Saga in harness (SagaHarness.Sagas.Contains() returns saga)
✗ UpdateName throws ProductNotFoundException

Event correlation configured as:
Event(() => NameUpdated, e => e.CorrelateById(c => c.Message.ProductId))

Using same ProductId in update that was returned from create. What am I missing?"
```

**Why improved**: Shows diagnostics done, includes config, specific about what works/doesn't.

---

## How This Knowledge Base Works

### For Consulting Agents
1. Read `consultation-guide.md` first to understand how to work with me
2. Use `quick-reference.md` for fast lookups during implementation
3. Reference `architecture-overview.md` when you need to understand "why"
4. Check `troubleshooting.md` when things go wrong

### For the Guru (Me)
1. Stay consistent with documented patterns
2. Reference specific files with absolute paths
3. Explain rationale, not just steps
4. Warn about pitfalls proactively
5. Update knowledge base when new patterns emerge

### Knowledge Base Principles
- ✅ Specific to KbStore (not generic .NET advice)
- ✅ Based on actual codebase (reference real files)
- ✅ Pattern-focused (teach patterns, not just answers)
- ✅ Practical (real examples, real paths, real commands)
- ✅ Living (updated as codebase evolves)

---

## Version History

### v1.0 - 2025-11-14 - Initial Creation
- Catalog domain fully documented (complete implementation)
- Storefront domain partially documented (in-progress implementation)
- Core patterns established
- Troubleshooting guide created
- Consultation patterns defined

### Future Updates
- Update as Storefront services complete
- Add new domains as implemented
- Refine troubleshooting as issues discovered
- Expand patterns as new scenarios emerge

---

## Success Criteria

This knowledge base is successful if:
- ✅ Agents can quickly find file locations
- ✅ Agents understand which pattern to apply
- ✅ Agents can troubleshoot issues without escalation
- ✅ Implementations remain consistent with architecture
- ✅ Cross-domain integrations follow established patterns
- ✅ New domains can be added following same playbook

---

## Contact & Updates

**Maintained by**: KbStore Codebase Guru agent

**Update trigger**: When new patterns emerge, domains are added, or common issues are discovered

**Review cycle**: After major feature implementations

---

**Ready to consult!** Other agents should start with `consultation-guide.md` to understand how to work with me effectively.
