# Orchestration Workflow

Use this command to remind yourself of best practices for delegating to specialized agents and managing complex multi-step tasks.

## Core Philosophy: Trust Capable Agents

When delegating to Sonnet 4+ agents, **define outcomes and trust reasoning** rather than prescribing steps.

### Effective Delegation Pattern

✅ **DO:**
- Define **functional goals** (what needs to work)
- Set **boundaries** (architectural patterns, constraints)
- Point to **reference implementations** (existing code to follow)
- Specify **success criteria** (builds, tests pass, services injectable)
- Trust agent **autonomy** (let them explore, decide HOW)

❌ **DON'T:**
- Create file-by-file checklists (disables reasoning)
- Micromanage implementation details
- Prescribe exact code structure
- Prevent agents from using exploration tools

### Example: Good vs Bad Prompting

**Bad (Checklist-Driven):**
```
Create these 3 files:
1. File A with properties X, Y, Z
2. File B that calls method M
3. Update line 42 in File C
```
Result: Agent executes checklist exactly, stops when done (may miss completeness)

**Good (Outcome-Driven):**
```
Goal: Implement a fully functional Services layer for Storefront.

Constraints:
- Follow EXACT pattern from Catalog.Services
- Services must be registered in DI and resolvable
- MongoDB queries through dedicated query service

Reference: KbStore.Catalog.Services/ (entire directory)

Success Criteria:
- ApiService can inject and use services
- dotnet build succeeds
- Services resolvable from DI container
```
Result: Agent explores patterns, implements completely, self-verifies

---

## Specialized Agent Roster

### Git Operations
**Agent:** `git-workflow-manager`
**When:** ALL Git operations (commits, branching, merging, status)
**Capabilities:**
- Analyzes changes and writes descriptive commit messages
- Determines appropriate workflow (branch vs direct commit)
- Handles complex operations (rebase, merge, stash)
- Autonomous decision-making on Git strategy

**Usage:**
```
Invoke after completing features/fixes
Let agent analyze changes and determine commit message
Agent handles add, commit, and workflow decisions
```

### Documentation
**Agent:** `changelog-manager`
**When:** After git-workflow-manager creates commits
**Capabilities:**
- Analyzes recent commits
- Generates CHANGELOG.md entries
- Creates ADR-style detailed documentation
- Single-line index entries + detailed files

**Usage:**
```
Invoke automatically after commits
Agent reads commit history and generates docs
```

### Code Quality
**Agent:** `code-review`
**When:** Before finalizing features, after implementation complete
**Capabilities:**
- Reviews for correctness, security, standards compliance
- Checks architectural consistency with existing patterns
- Identifies leaky abstractions and coupling issues
- Categorizes issues by severity (Critical/High/Medium/Low)

**Usage:**
```
Invoke BEFORE creating commits
Let agent explore codebase and identify issues
Trust agent's severity assessments
Use findings to guide fixes
```

### Backend Implementation
**Agent:** `dotnet-backend-engineer`
**When:** Implementing .NET backend code, state machines, services, domain logic
**Capabilities:**
- Performance-critical C#/.NET code
- MassTransit patterns and state machines
- Database queries and optimizations
- Service layer implementations
- Includes test coverage as part of deliverables

**Usage:**
```
Delegate entire vertical slices (domain + services + tests)
Point to reference implementations
Define architectural boundaries
Trust agent to implement completely
```

### Codebase Exploration
**Agent:** `Explore` (with thoroughness level)
**When:** Need to understand codebase structure, find patterns, answer "how does X work?"
**Capabilities:**
- Fast codebase exploration
- Pattern identification
- Answers architectural questions
- File discovery by patterns

**Thoroughness Levels:**
- `quick` - Basic searches
- `medium` - Moderate exploration
- `very thorough` - Comprehensive analysis

**Usage:**
```
Use when YOU don't know the codebase well enough to direct
Let agent explore and report findings
Use findings to inform delegation to other agents
```

### Architectural Planning
**Agent:** `systems-architect`
**When:** High-level design, cross-system integration, strategic planning
**Capabilities:**
- Analyzes patterns and dependencies across services
- Designs integration points
- Strategic technical planning
- Implementation-agnostic recommendations

**Usage:**
```
Invoke BEFORE implementation for complex features
Let agent analyze existing architecture
Use recommendations to guide implementation agents
```

### Domain Expertise
**Agents:** `kbstore-codebase-guru`, `masstransit-expert`, `efcore-expert`, `aspnetcore-expert`
**When:** Need deep knowledge of specific domain/framework
**Capabilities:**
- Expert guidance on patterns and best practices
- Troubleshooting framework-specific issues
- Reference implementations from source code
- Architectural recommendations

**Usage:**
```
Consult when architectural decisions needed
Ask about framework capabilities and patterns
Use for troubleshooting complex issues
```

---

## Workflow Patterns

### Pattern 1: New Feature Implementation

```
1. systems-architect: Design integration approach
2. dotnet-backend-engineer: Implement (domain + services + tests)
3. code-review: Review implementation
4. dotnet-backend-engineer: Fix issues (if any)
5. git-workflow-manager: Create commit
6. changelog-manager: Generate documentation
```

### Pattern 2: Bug Fix

```
1. Explore (if needed): Understand the issue
2. dotnet-backend-engineer: Fix the bug + add tests
3. code-review: Verify fix doesn't introduce issues
4. git-workflow-manager: Create commit
5. changelog-manager: Document fix
```

### Pattern 3: Architectural Decision

```
1. Domain expert (masstransit-expert, etc.): Research options
2. systems-architect: Design approach
3. AskUserQuestion: Confirm direction
4. dotnet-backend-engineer: Implement
5. code-review: Verify consistency
6. git-workflow-manager + changelog-manager: Document
```

### Pattern 4: Large Multi-Phase Work

```
1. systems-architect: Break into phases
2. AskUserQuestion: Confirm phase breakdown
3. For each phase:
   a. dotnet-backend-engineer: Implement
   b. code-review: Review
   c. Fix issues
   d. git-workflow-manager: Commit
   e. changelog-manager: Document
4. Final code-review: Verify integration
```

---

## Parallel Execution

**Maximize efficiency by launching independent agents in parallel:**

### When to Parallelize
✅ Multiple independent tasks (abstractions + domain + services)
✅ Different domains/modules
✅ Exploration + research tasks
✅ Code review while implementation continues

### When to Serialize
❌ Tasks with dependencies (contracts before services)
❌ Implementation → Review → Fix workflow
❌ Exploration → Decision → Implementation

### Syntax
```
Single message with multiple Task tool calls:
- Task 1: Create contracts
- Task 2: Create exceptions
- Task 3: Create interfaces
(All execute in parallel)
```

---

## TodoWrite Usage

**Use TodoWrite proactively for:**
- Complex multi-step tasks (3+ steps)
- Non-trivial tasks requiring planning
- Tracking progress across multiple sessions
- Demonstrating thoroughness to user

**Update frequently:**
- Mark task `in_progress` BEFORE starting work
- Mark `completed` IMMEDIATELY after finishing (don't batch)
- Keep EXACTLY ONE task `in_progress` at a time
- Add new tasks as discovered during implementation

**Skip TodoWrite for:**
- Single straightforward tasks
- Trivial operations (< 3 steps)
- Purely conversational interactions

---

## Decision-Making Framework

### When to Delegate
- **Complex implementation** → dotnet-backend-engineer
- **Architectural design** → systems-architect
- **Framework questions** → Domain expert
- **Code quality check** → code-review
- **Codebase exploration** → Explore
- **Git operations** → git-workflow-manager
- **Documentation** → changelog-manager

### When to Handle Directly
- Small edits (1-2 line changes)
- Quick file reads for decision-making
- Simple grep/glob searches
- Build/test verification commands
- Direct user questions/clarifications

### The Threshold Test
**Ask:** "Would a senior engineer delegate this to a specialist?"
- Yes → Use agent
- No → Handle directly
- Unsure → Delegate (agents are fast, trust their capability)

---

## Common Pitfalls

❌ **Prescriptive checklists** - Disables agent reasoning, leads to incomplete implementations
❌ **Micromanaging files** - Prevents agents from using exploration tools
❌ **Not trusting agents** - Second-guessing decisions wastes context
❌ **Forgetting git-workflow-manager** - Manual commits miss quality
❌ **Skipping code-review** - Issues caught late are expensive
❌ **Not parallelizing** - Sequential execution is slower
❌ **Forgetting changelog-manager** - Documentation falls behind

---

## Quality Gates

Before considering a feature "complete":

1. ✅ **Code Review Pass** - No CRITICAL or HIGH issues
2. ✅ **Build Succeeds** - `dotnet build` with no errors
3. ✅ **Tests Pass** - All relevant tests passing
4. ✅ **Services Registered** - DI container can resolve
5. ✅ **Committed** - git-workflow-manager created commit
6. ✅ **Documented** - changelog-manager updated CHANGELOG.md

---

## Adaptive Learning

This workflow evolves as we discover better patterns:
- Update this file when new patterns emerge
- Adjust agent usage based on results
- Refine prompting strategies
- Document successes and failures

**Trust the process. Trust the agents. Focus on outcomes.**
