## Why

FusionCanvas has established expectations for SOLID design, Clean Architecture, testing, security, and specification-first development, but no defined way to verify them periodically. With multiple AI coding agents (Codex, OpenCode) contributing to the same repository, quality checks need to be repeatable and consistent rather than ad hoc, so that architecture erosion, test gaps, security issues, and specification drift are caught early instead of compounding silently.

## What Changes

- Define a recurring QA review process composed of independent QA tasks that can run together as a full review or individually by task ID.
- Cover these review areas: pragmatic SOLID and code design, Clean Architecture conformance, testing baseline and coverage, security and vulnerability hygiene, and specification/documentation drift.
- Establish an operational playbook (`docs/qa-review.md`) with per-task checklists, commands, a severity scale, a standard report format, and fix-routing rules.
- Establish that running QA requires no OpenSpec ceremony, while findings that change accepted behavior — including specification drift — are routed through the normal OpenSpec workflow.
- Keep CI integration, enforced coverage gates, new tooling dependencies, UI automation, and manual app-level testing out of scope.

## Capabilities

### New Capabilities
- `qa-review-baseline`: Defines the recurring QA review process, its review areas, execution modes (full or per-task), finding severity and reporting expectations, and how findings are routed to direct fixes or OpenSpec changes.

### Modified Capabilities

## Impact

- Affected docs: new `docs/qa-review.md` playbook; `AGENTS.md` points agents at the playbook for QA requests.
- Affected specs: a new `qa-review-baseline` capability will become accepted behavior after implementation and archive.
- Affected code: none — this is a process definition. Later QA runs may surface findings that result in separate maintenance fixes or OpenSpec changes.
- Dependencies: no new packages or tools; the process uses the existing `dotnet` CLI, OpenSpec CLI, and git.
- UX review: not applicable — this is a non-user-facing process change with no application UI impact.
