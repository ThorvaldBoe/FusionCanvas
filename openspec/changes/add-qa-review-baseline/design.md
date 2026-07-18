## Context

FusionCanvas has accepted expectations for design and architecture (`architecture-guidelines`), testing (`testing-baseline`), and specification-first development (`openspec-project-workflow`), and security guidance is spread across `README.md` and `AGENTS.md`. What is missing is a defined way to *verify* these expectations periodically. Contributions come from multiple AI coding agents (Codex, OpenCode) plus human contributors, so quality checks need to be executable consistently by any of them, in full or in part.

This change is process-only: it defines the QA review practice and its playbook. It does not change application behavior, add tooling, or introduce CI automation.

## Goals / Non-Goals

**Goals:**

- Establish a recurring QA review process decomposed into independent tasks: SOLID and code design, Clean Architecture conformance, testing and coverage, security and vulnerability hygiene, and specification/documentation drift.
- Make the process executable in full (all tasks) or partially (single task by ID), with a shared protocol, severity scale, and report format.
- Keep the durable requirements in OpenSpec (`qa-review-baseline`) and the evolving operational detail (checklists, commands) in `docs/qa-review.md`.
- Route findings by impact: internal maintenance fixes directly; behavior changes and spec drift through the OpenSpec workflow.
- Keep the process runnable by both Codex and OpenCode (and humans) from the same instructions, using only the `dotnet` CLI, OpenSpec CLI, and git.

**Non-Goals:**

- No CI pipeline integration or automated quality gates.
- No enforced coverage percentage targets or new tooling dependencies.
- No UI automation, visual regression, or manual app-level test procedures.
- No fixes for findings discovered by future QA runs — those land as separate maintenance work or OpenSpec changes.

## Decisions

### Split durable requirements from the operational playbook

The OpenSpec capability captures *what QA must cover* and *how findings are routed*; `docs/qa-review.md` captures *how to execute each task* (checklists, commands, report template). Checklist refinements then happen without OpenSpec ceremony, while changes to QA scope or routing go through a proposal.

Alternative considered: playbook document only. That would deviate from how comparable process definitions (FC-0006 OpenSpec Project Workflow, FC-0007 Testing Baseline) were established, and would leave QA expectations without accepted-behavior status. The archived change freezes history, not the practice — the synced spec remains a living document updated by later changes, so a recurring process is not locked in by archiving.

### Define QA tasks as agent-agnostic instructions referenced from AGENTS.md

Both Codex and OpenCode read `AGENTS.md` at session start; the playbook is a plain document with explicit commands and checklists that any agent or human can follow.

Alternative considered: implement the tasks as per-tool custom commands/skills (e.g. duplicated slash commands for Codex and OpenCode). That would duplicate the definitions across tool-specific formats and invite drift between them.

### Select five review areas aligned to existing accepted expectations

SOLID/code design and Clean Architecture map to `architecture-guidelines`; testing maps to `testing-baseline`; security covers dependency hygiene, secrets, and injection vectors (proportionate to a local-first app that plans AI/marketplace features); specification drift protects the OpenSpec process itself.

Alternative considered: also include code metrics, linting, and formatting gates. Deferred — the incremental complexity principle favors a review practice first; automation can follow when the practice proves its value.

### Route findings by behavior impact, not by severity alone

The `openspec-project-workflow` spec already allows small maintenance changes without a proposal. QA adopts the same boundary: non-behavioral findings (including missing tests for accepted behavior and dependency updates) are fixed directly; anything that changes accepted behavior or reconciles spec drift goes through propose → apply → archive.

Alternative considered: route all fixes through OpenSpec for traceability. That would make QA-heavy periods ceremonial and discourages running reviews often.

### Keep reports ephemeral by default

Reports are presented in the session; an optional dated copy may be saved under `TestResults/qa/`, which is gitignored.

Alternative considered: commit QA reports to the repo. Rejected for now — committed reports add noise; the routing of findings into fixes or OpenSpec changes is where durability matters.

## Risks / Trade-offs

- [Risk] Reviews become box-ticking with vague findings -> Mitigation: the protocol requires concrete evidence (file, line, command output, or spec reference) and a severity for every finding; "no findings" must state what was checked.
- [Risk] Playbook and spec drift apart over time -> Mitigation: QA-5 explicitly checks alignment between `AGENTS.md`, the playbook, specs, and workflow skills.
- [Risk] Security review gives false confidence while the app has no network surface -> Mitigation: the task scopes checks to what is relevant today (dependencies, secrets, injection-style vectors) and re-evaluates as AI/marketplace features land.
- [Risk] Recurring full reviews cost more effort than they return -> Mitigation: tasks are independently runnable; the suggested cadence runs cheap tasks (security) often and the full review at milestones.

## Migration Plan

Not applicable — no existing behavior or data changes. The playbook and spec take effect when the change is implemented and archived.

## Open Questions

- Should a future change add CI execution of cheap QA checks (e.g. `dotnet test`, package vulnerability scan)? Candidate for a later proposal once the manual practice settles.
- Is a persistent, committed QA history ever needed (e.g. for releases)? Revisit if release management begins.
