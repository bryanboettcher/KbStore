# Orchestration Workflow Guide for Specialized Agent Delegation

**Date**: 2025-11-13
**Commit(s)**: 4b3b09a
**Author**: Bryan Boettcher
**Category**: Documentation

## Summary

Introduced `orchestration.md` as a comprehensive reference guide for effective task delegation to specialized agents. This guide documents core principles of outcome-driven delegation, workflow patterns for common development tasks, parallel execution strategies, and quality gates for feature completion. Enables more efficient autonomous agent collaboration by defining goals and boundaries rather than prescriptive checklists.

## Changes

### Files Added
- `.claude/commands/orchestrate.md` - Complete orchestration workflow guide (334 lines)

## Key Decisions

- **Outcome-Driven Delegation**: Emphasizes defining functional goals and boundaries rather than micromanaging step-by-step checklists. This approach enables agents to use reasoning autonomously while maintaining architectural consistency.
- **Specialized Agent Roster**: Documents the capabilities and appropriate usage of each specialized agent (git-workflow-manager, code-review, dotnet-backend-engineer, systems-architect, changelog-manager, Explore, domain experts).
- **Workflow Patterns**: Provides reusable templates for common scenarios (new feature, bug fix, architectural decision, multi-phase work) to guide delegation decisions.
- **Parallel Execution Guidelines**: Specifies when tasks can and cannot run in parallel to maximize efficiency without creating dependency issues.
- **Quality Gates**: Defines six checkpoints (code review pass, build succeeds, tests pass, services registered, committed, documented) that must be satisfied before considering a feature complete.

## Impact

### Before
Development workflow lacked clear guidance on:
- When to delegate vs handle directly
- How to effectively prompt specialized agents
- Optimal workflow patterns for different task types
- Quality standards for feature completion

This resulted in:
- Inconsistent agent utilization
- Uncertainty about task delegation boundaries
- Potential for incomplete implementations
- Missing quality verification steps

### After
Development team now has:
- Clear decision framework for agent delegation
- Reference patterns for common workflows
- Structured quality gates ensuring completeness
- Guidelines for parallel task execution
- Best practices for outcome-driven prompting

This enables:
- More efficient autonomous agent collaboration
- Consistent, high-quality implementations
- Reduced back-and-forth on task clarifications
- Faster feature delivery with better quality assurance

## Technical Details

### Orchestration Philosophy

The guide is built on the principle that **capable agents reason better than follow checklists**. Rather than prescribing exact file structures or step-by-step procedures, the guide defines:

1. **Functional Goals**: What needs to work (not how to implement it)
2. **Boundaries**: Architectural patterns and constraints to follow
3. **Reference Implementations**: Existing code to use as template
4. **Success Criteria**: Measurable outcomes that indicate completion

### Specialized Agent Definitions

The guide formally documents each agent's domain:

- **git-workflow-manager**: All Git operations (commits, branching, complex workflows)
- **changelog-manager**: Commit documentation and CHANGELOG.md maintenance
- **code-review**: Quality assurance, architectural consistency, security checks
- **dotnet-backend-engineer**: C#/.NET implementation, MassTransit patterns, databases, state machines, tests
- **systems-architect**: High-level design, cross-system integration, strategic planning
- **Explore**: Fast codebase exploration, pattern discovery, answering "how does X work?"
- **Domain Experts**: kbstore-codebase-guru, masstransit-expert, efcore-expert, aspnetcore-expert

### Workflow Patterns

Four reusable patterns are provided:

1. **New Feature Implementation**: Design → Implement → Review → Fix → Commit → Document
2. **Bug Fix**: Explore (if needed) → Fix + Test → Review → Commit → Document
3. **Architectural Decision**: Research → Design → Confirm → Implement → Review → Document
4. **Large Multi-Phase Work**: Break into phases → Execute each phase through full workflow → Final integration review

### Quality Gates

Six checkpoints must be satisfied before a feature is considered complete:

1. Code Review Pass (no CRITICAL or HIGH issues)
2. Build Succeeds (dotnet build with no errors)
3. Tests Pass (all relevant tests passing)
4. Services Registered (DI container can resolve)
5. Committed (git-workflow-manager created commit)
6. Documented (changelog-manager updated CHANGELOG.md)

## Related

- **CLAUDE.md**: Development workflow section (foundational document for orchestration principles)
- **KbStore Architecture**: Follows the repeatable vertical stack pattern with service layers, state machines, and database per domain

## Validation

This is a reference/guidance document rather than code. Validation involves:

- [x] Comprehensive coverage of all specialized agents
- [x] Clear examples of effective vs ineffective delegation
- [x] Practical workflow patterns reflecting real development tasks
- [x] Quality gates aligned with project standards
- [x] Consistency with existing CLAUDE.md principles
- [x] Actionable decision-making framework
