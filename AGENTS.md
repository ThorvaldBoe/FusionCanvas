# AGENTS.md — FusionCanvas Agent Guide

This file is the shared entry point for AI coding agents working in this repository (Codex, OpenCode, and similar tools). Read it before doing anything and follow it for every task. Human contributors should start with `README.md`.

## Project Snapshot

- **FusionCanvas** is an open-source, local-first desktop application for Print-on-Demand creators.
- Stack: .NET 10, C#, Avalonia, SQLite (Microsoft.Data.Sqlite), xUnit v3, OpenSpec.
- Solution: `FusionCanvas.sln`. Production code in `src/`, tests in `tests/`.
- Status: early development. Accepted system behavior is defined by OpenSpec specs, not by the code alone.

## Required Context (read before any non-trivial task)

Read these in order; drill into detail only where the task touches them:

1. **This file** — the working agreement for agents.
2. **`openspec/project.md`** — mission, core principles, architecture direction, OpenSpec usage, decision-making guidelines.
3. **`openspec/specs/<capability>/spec.md`** — accepted behavior per capability. **Specs are the source of truth; code follows specs, not the reverse.**
4. **`docs/`** — background and direction:
   - `principles.md` — durable product principles; evaluate decisions against them.
   - `architecture.md` — Clean Architecture, modules, testing strategy.
   - `ui-guidelines.md` — Obsidian-inspired shell, layout, dark theme.
   - `data-model.md`, `design-pipeline.md`, `plugin-model.md`, `product-vision.md`, `strategic-decisions.md`, `roadmap.md` as relevant.
   - `qa-review.md` — the QA review playbook (see below).

`docs/LifeOS/` is optional historical reference. It may contain useful ideas, but it is potentially stale and is not required context, current scope, feature ordering, or acceptance authority.

## OpenSpec Workflow (mandatory for behavior changes)

Every feature that adds or changes behavior goes through a rolling delivery-module workflow:

```text
Discover → Define module → Propose → Review → Apply → Verify → Learn → Archive
```

- The OpenSpec CLI (`openspec`) is installed. Key commands: `openspec list --json`, `openspec status --change <name> --json`, `openspec instructions <artifact> --change <name> --json`, `openspec validate`.
- Step-by-step workflow skills live in `.codex/skills/` and are shared by Codex and OpenCode: `openspec-propose`, `openspec-apply-change`, `openspec-archive-change`, `openspec-sync-specs`, `openspec-explore`. Follow them when proposing, implementing, or archiving a change.
- Spec deltas use `## ADDED / MODIFIED / REMOVED / RENAMED Requirements` sections. Accepted specs live in `openspec/specs/`; active work in `openspec/changes/`; history in `openspec/changes/archive/`.
- Never edit `openspec/specs/` directly to change behavior. Change behavior through a change's delta specs, then sync and archive through the workflow.
- Small maintenance work that does not alter accepted behavior (bug fixes, internal refactors, docs, dependency updates) may proceed without a proposal — see `openspec/specs/openspec-project-workflow/spec.md`.

### Delivery Module Rules

- A **delivery module** is a cohesive, independently verifiable set of features with one clear outcome. It is a planning unit, not necessarily a code project or architecture module, and it has no fixed feature-count limit.
- Plan only the next module in detail. Keep later opportunities lightweight until the current module is implemented, verified, and reviewed for lessons.
- The proposal states included features, dependencies, non-goals, risks, verification approach, and why the scope is coherent and reviewable. Split it when it contains independent outcomes, unresolved high-impact decisions, or a verification surface too large to diagnose.
- Discovery is collaborative. Capture resolved examples, edge cases, assumptions, and decisions in the artifacts. Do not begin implementation while a product, UX, data, architecture, or acceptance decision could materially change the result unless the user explicitly delegates it.
- Do not create a separate module-specification document by default. Once the next module is understood well enough to name and bound, create or refine its OpenSpec change: `proposal.md` is the module-level anchor, delta specs own requirements and acceptance scenarios, `design.md` owns design and the implementation plan, `tasks.md` owns bounded work, and `verification.md` owns criterion-level evidence.
- One module normally maps to one OpenSpec change. Delta specs contain durable requirements and acceptance scenarios. `design.md` contains conceptual/functional design plus a dedicated implementation plan. `tasks.md` decomposes that plan. `verification.md` maps every acceptance scenario to results and evidence.
- The implementation plan must be explicit enough for the assigned agent: affected layers and likely types/files, responsibility placement, data/persistence and UI changes, algorithms and edge cases, sequencing, test locations, migration/compatibility needs, and decisions not to reopen.
- Acceptance criteria are pass/fail gates. Map each one to a verification method before implementation. If a criterion fails, correct the implementation or approved artifacts and rerun it plus relevant regression checks; never hide it behind an aggregate test pass.

### Agent Assignment and Handoffs

- Assign by capability, not by assuming all agents or models are interchangeable. Use a high-reasoning agent or human for discovery, specification, design review, ambiguous corrections, and final acceptance review.
- Lower-cost agents may implement bounded tasks only from an approved, implementation-ready delivery package. Current suggested routing is Codex for high-value planning, review, and desktop work; Kimi K3 for complex specification/review in OpenCode; and GLM 5.2 for explicit implementation tasks. Model names are operational examples and may change.
- A handoff names the change, required artifacts, exact task range, validation commands, prohibited scope expansion, and escalation conditions.
- If implementation exposes a missing product, UX, data, architecture, or acceptance decision, stop the affected task and return the ambiguity for review. Do not guess.

## Architecture Rules

Clean Architecture with four layer projects; dependencies point inward:

```text
FusionCanvas.App ─┐
                  ├─→ FusionCanvas.Application → FusionCanvas.Domain
FusionCanvas.Integration ─┘
```

- **Domain**: business concepts, invariants, calculations, workflow rules. No references to UI, persistence, or external SDKs.
- **Application**: use cases, orchestration, ports/contracts (e.g. `IWorkspaceRepository`, `IWorkspaceFileStore`).
- **Integration**: SQLite persistence, workspace file storage, and future marketplace/AI/plugin adapters behind application-facing contracts.
- **App**: Avalonia UI, view models, navigation, presentation state. No business logic. Hand-rolled MVVM (no MVVM toolkit); compiled bindings.
- Details live in `docs/architecture.md` and `openspec/specs/architecture-guidelines/spec.md`; those sources win if older notes disagree (the layer project is named **Integration**, not Infrastructure).
- Do not create speculative projects or abstractions, and do not place domain or application behavior in the UI project because it is convenient.

## Coding Principles

- **Pragmatic SOLID**: focused responsibilities, explicit constructor dependencies, and abstractions only where they protect a real boundary, variation point, or testable contract. Avoid god classes and tight coupling, but also avoid speculative interfaces and interface-per-class noise.
- Prefer composition over inheritance, immutability where practical, and minimal, focused changes that follow existing code style.
- Nullable reference types and implicit usings are enabled; keep code warning-clean.

## Testing

- Framework: **xUnit v3** with coverlet collector. Test projects mirror production under `tests/` (`FusionCanvas.<Layer>.Tests`).
- Every behavior change ships with focused tests per `openspec/specs/testing-baseline/spec.md`: domain rules without frameworks, application use cases with deterministic collaborators, persistence boundaries with isolated temporary resources, and UI-owned decision logic testable in code without launching the app.
- A targeted **real desktop UI verification pass** against the built Avalonia application is expected for each user-facing delivery module **when the contributing agent can run an interactive desktop session** (e.g., Codex). Prioritize the critical end-to-end workflow, new framework wiring, persistence, destructive actions, state synchronization, complex focus/input behavior, recovery, accessibility, and tabs/windows. Representative variants are enough when other low-risk combinations are covered deterministically; record why the chosen scenarios provide sufficient confidence.
- **OpenCode cannot perform interactive desktop verification** (no display). When running under OpenCode, the desktop UI pass is optional and non-blocking: record it as not-applicable in the change verification evidence, state that OpenCode lacks the capability, and rely on the fast deterministic baseline plus any UI-owned decision-logic tests. Do not fake or silently skip it.
- Desktop UI verification uses a disposable database/workspace, never the contributor's normal workspace. Record the tested build/environment, scenarios, results, isolation method, limitations, and material screenshots or automation logs in the change verification evidence.
- Keep desktop UI verification separate from the fast deterministic baseline. Run the full all-features desktop matrix at milestones, before releases, or after broad cross-cutting UI changes—not after every ordinary module. External-service tests and pixel-perfect visual regression remain outside the default baseline unless a feature specifically requires them.
- Baseline command — must pass before work is considered done:

  ```powershell
  dotnet test .\FusionCanvas.sln
  ```

## Security

- This is a **public GitHub repo**: never commit secrets, API keys, tokens, or credentials. Future AI/marketplace features must keep secrets out of source control.
- Keep NuGet packages current and address vulnerability warnings promptly (`dotnet list package --outdated`, `dotnet list package --vulnerable --include-transitive`).
- Treat external and user-controlled content as untrusted input. Guard against SQL injection, path traversal in workspace file handling, and prompt-injection-style attack vectors in current and future AI-facing code.

## QA Reviews

- The QA playbook lives in **`docs/qa-review.md`**. When the user asks for a QA review — full or a specific area (SOLID, architecture, testing, security, spec drift) — follow that document.
- Every completed delivery module receives scoped completion QA: build and deterministic tests, strict OpenSpec validation, criterion-level evidence, changed-scope drift review, and the architecture/security/persistence/UI checks relevant to its risks. This is not automatically a full repository QA review.
- A full QA review includes the QA-6 real desktop regression matrix for **all** accepted and implemented user-facing features. If an interactive desktop is unavailable (for example, when running under OpenCode), report QA-6 as **not applicable** rather than passed; it does not block the rest of the review.
- Running QA requires no OpenSpec ceremony. Findings that would change accepted behavior are routed through the OpenSpec workflow; specification drift is reconciled via an OpenSpec change.

## Working Agreement

- Windows + PowerShell environment; the `dotnet` CLI is available.
- Keep changes minimal and scoped; match existing conventions; do not fix unrelated issues unless asked.
- Do not run `git commit`, `git push`, or other git mutations unless explicitly asked.
- If a task is ambiguous or reveals a design/spec conflict, stop and ask rather than guessing.
