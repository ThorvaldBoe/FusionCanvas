## Context

FusionCanvas has accumulated detailed long-range PRDs and a phase roadmap, but those documents became stale before implementation reached them. Recent work also showed that large implementation batches and behavior-only specifications leave too much product and architecture interpretation to an implementation model. The process already uses OpenSpec, deterministic tests, desktop verification, and retrospectives; this change reshapes those tools into a rolling delivery loop.

The term **delivery module** in this process means a cohesive, reviewable set of features delivered together. It is a planning unit and does not imply a new assembly, namespace, Clean Architecture boundary, or one-to-one mapping to the product modules described in `docs/architecture.md`.

The process must work for humans, Codex, and OpenCode with different model tiers. Model names and capabilities will evolve, so normative rules are capability-based while current model assignments remain operational guidance.

## Goals / Non-Goals

**Goals:**

- Keep detailed planning limited to the next coherent module.
- Build and record shared product understanding before implementation.
- Give bounded implementation agents enough technical detail to execute without inventing product or architecture decisions.
- Make acceptance criteria observable, traceable, and retryable completion gates.
- Spend desktop UI verification on the workflows and risks where it provides the most information.
- Preserve OpenSpec requirements as durable behavior while allowing implementation guidance to evolve with the codebase.
- Keep historical LifeOS material available without making it required or authoritative.

**Non-Goals:**

- Pick the next product module or define a fixed number of features per module.
- Mandate a permanent model vendor, model name, or agent tool.
- Put source-file-level implementation detail into accepted capability specifications.
- Run a complete all-features desktop regression after every module.
- Rewrite archived OpenSpec changes that accurately preserve historical rationale.
- Change application behavior or UI; UX preflight is not applicable.

## Decisions

### Use one OpenSpec change as the delivery package for one module

A delivery module normally maps to one OpenSpec change. Its proposal defines the module outcome and boundaries; delta specs define requirements and acceptance scenarios; `design.md` contains conceptual/functional design plus a dedicated implementation plan; `tasks.md` decomposes that plan into checkable work; `verification.md` records acceptance evidence and residual limitations.

Exception: a module may need more than one coordinated change when it contains independently deployable capabilities or unavoidable sequencing. The module brief must explain the split and shared completion gate.

Alternative considered: create a new custom OpenSpec artifact schema immediately. Rejected because the existing artifacts can express the process clearly, and a schema migration would add tooling work without improving product confidence yet.

### Select scope by cohesion, uncertainty, and verification cost

No numeric feature limit is imposed. A suitable module has one coherent user or platform outcome, shares dependencies and verification setup, and remains small enough that a reviewer can understand the full change and an implementer can complete it without losing context. The proposal must justify why grouping the included features reduces overhead without hiding unrelated work.

Scope is reduced when it contains multiple independent outcomes, unresolved architecture choices, too many cross-layer migrations, or a desktop matrix too large to diagnose efficiently. Scope may be expanded when several small features use the same data model, surface, fixture, and acceptance pass.

Alternative considered: prescribe a fixed range such as 5–12 features. Rejected because feature sizes differ too much for a count to be a reliable complexity measure.

### Add an explicit understanding and approval gate

Module discovery is conversational. The human and specification agent exchange questions, examples, non-goals, edge cases, assumptions, and disagreements until the module has no unresolved high-impact product decision. The artifacts capture the resulting decisions, not the entire chat transcript. Implementation begins only after the user approves the module scope and delivery package, or explicitly delegates that approval.

Alternative considered: let implementation expose ambiguities and correct them later. Rejected because bounded lower-cost agents are least reliable when forced to make product decisions implicitly.

### Separate durable behavior from a detailed implementation plan

Accepted specs remain implementation-independent and testable. The change design gains an `Implementation Plan` section detailed enough for the assigned implementer. Depending on the module it identifies affected projects and likely files, responsibilities by layer, domain and persistence changes, UI composition and state ownership, algorithms and edge cases, migration/compatibility needs, test locations, execution order, and decisions the implementer must not reopen.

The plan may name current types and files but is not archived as permanent architectural truth; accepted requirements remain authoritative if implementation details later change. `tasks.md` is derived from the plan and uses small, ordered, verifiable steps.

Alternative considered: put the implementation plan directly into capability specs. Rejected because implementation paths become stale faster than behavior and would pollute the durable source of truth.

### Treat acceptance criteria as a traceable completion contract

Every behavior requirement has observable scenarios. Before implementation, the delivery package maps each scenario to one or more verification methods: focused automated test, integration test, targeted desktop scenario, manual inspection, or an explicit not-applicable rationale. `verification.md` records the result and evidence for every criterion. A failed criterion returns to implementation and is rerun; it is not waived by an overall test-suite pass.

Alternative considered: use task completion and `dotnet test` as sufficient evidence. Rejected because they can both pass while the intended workflow remains incomplete or incorrectly wired.

### Budget desktop UI verification by risk and information value

Each user-facing module receives a targeted real-desktop pass when an interactive session is available. The pass prioritizes the module's critical end-to-end workflow, new framework wiring, destructive or persistent behavior, state synchronization, complex focus/input behavior, and previous failure areas. Equivalent low-risk variants may be sampled rather than repeated exhaustively. The plan records why selected scenarios are sufficient and what remains covered by deterministic tests.

Full all-features desktop regression remains appropriate at milestones, before releases, or when cross-cutting shell/navigation changes create broad risk. Agents without an interactive desktop mark the lane not applicable and hand off the recorded matrix to a capable agent or human; they never infer a desktop pass from code-level tests.

Alternative considered: run every acceptance scenario through the desktop after every feature. Rejected because it spends scarce interactive-test capacity on low-information repetition and slows the feedback loop.

### Assign work by required reasoning and verification capability

Module definition, specification, design review, ambiguous corrections, and final acceptance review go to a high-reasoning agent or human. Bounded implementation tasks can go to a lower-cost agent only after the delivery package is approved and implementation-ready. Current suggested routing is Codex for high-value planning/review/desktop work, Kimi K3 for complex specification or review work in OpenCode, and GLM 5.2 for explicit implementation tasks. These names are examples, not permanent policy.

Handoffs must name the exact change, required artifacts, task range, validation commands, prohibited scope expansion, and escalation conditions. If an implementer finds a missing decision, it stops that task and returns the ambiguity rather than guessing.

Alternative considered: prescribe one model for the whole lifecycle. Rejected because cost and capability needs differ across discovery, implementation, and verification.

### Make the roadmap rolling and demote LifeOS documents

`docs/roadmap.md` becomes a rolling delivery view: current module, candidate next module, recently completed modules, and lightweight later opportunities. Only the current/next module receives detailed specifications. `docs/LifeOS/` remains an explicitly historical archive that may inspire later discussions but is not required reading, current scope, ordering, or acceptance authority.

Alternative considered: keep the phase roadmap and add review gates. Rejected because it continues to imply commitment to a large stale inventory and encourages premature specification.

## Implementation Plan

1. Update `openspec-project-workflow`, `testing-baseline`, and `qa-review-baseline` through this change's delta specs.
2. Rewrite the OpenSpec project context and configuration rules so new proposals start from current accepted behavior, current application state, user discussion, and the rolling roadmap—not LifeOS PRDs.
3. Update `AGENTS.md` with the delivery-module lifecycle, readiness checklist, agent-routing guidance, acceptance retry loop, and desktop-test budget.
4. Update the repository OpenSpec skills:
   - `openspec-propose` must capture the module definition, understanding decisions, acceptance criteria, and detailed implementation plan before apply-ready status.
   - `openspec-apply-change` must check implementation readiness, execute only approved scope, maintain criterion-to-evidence mapping, and loop on failures.
   - `openspec-archive-change` must require completed verification evidence and learning review.
   - `openspec-explore` should support module discovery without prematurely creating artifacts.
5. Replace `docs/roadmap.md` with the rolling format and update `README.md`, `docs/README.md`, `docs/qa-review.md`, `docs/architecture.md`, and `docs/strategic-decisions.md` where they imply PRD authority or unconditional exhaustive desktop testing.
6. Rewrite `docs/LifeOS/README.md` to label the folder as historical optional reference. Preserve the PRD files and archived changes themselves.
7. Reconcile the still-active `add-ui-testing-baseline` artifacts with the risk-based policy so a later archive cannot reintroduce contradictory unconditional language.
8. Validate this change and all active OpenSpec changes, scan non-historical canonical documents for stale PRD instructions, run `git diff --check`, and run the solution test baseline.

## Risks / Trade-offs

- [Detailed plans become stale during implementation] → Update the active design and tasks when discoveries change the approved approach; keep accepted specs behavior-focused.
- [Module boundaries become subjective] → Require an explicit scope rationale and apply the cohesion/uncertainty/verification-cost checks during review.
- [Extra gates create ceremony] → Use one delivery package, scale evidence to risk, and skip OpenSpec only for maintenance that truly does not change behavior.
- [Lower-cost agents follow a flawed plan precisely] → Require high-reasoning specification review and make ambiguity escalation explicit.
- [Targeted desktop testing misses a regression] → Preserve deterministic coverage, choose scenarios from risk, and run full desktop regression at milestone/release gates.
- [Model guidance becomes obsolete] → Keep normative requirements capability-based and model names in an easily updated operational section.
- [Historical ideas are forgotten] → Preserve `docs/LifeOS/` and list it as an optional discovery input, not a default dependency.

## Migration Plan

Adopt the new workflow for the next module selected after this process change. Existing active feature changes may finish under their approved artifacts unless they are materially re-scoped; contradictory active process changes are reconciled now. No production or data migration is required.

Rollback is documentation-only: revert the process documents and delta specs. Historical LifeOS files remain untouched either way.

## Open Questions

None. The next product module is intentionally left unselected until the user and planning agent perform the new discovery step together.
