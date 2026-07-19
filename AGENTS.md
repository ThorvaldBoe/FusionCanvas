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
5. **`docs/LifeOS/PRD/`** — product planning source material **only**. PRDs are NOT accepted specifications. Translate PRD intent into OpenSpec changes; never implement directly from a PRD without an accepted change.

## OpenSpec Workflow (mandatory for behavior changes)

Every feature that adds or changes behavior goes through OpenSpec:

```text
Propose → Review → Apply (implement) → Validate → Archive
```

- The OpenSpec CLI (`openspec`) is installed. Key commands: `openspec list --json`, `openspec status --change <name> --json`, `openspec instructions <artifact> --change <name> --json`, `openspec validate`.
- Step-by-step workflow skills live in `.codex/skills/` and are shared by Codex and OpenCode: `openspec-propose`, `openspec-apply-change`, `openspec-archive-change`, `openspec-sync-specs`, `openspec-explore`. Follow them when proposing, implementing, or archiving a change.
- Spec deltas use `## ADDED / MODIFIED / REMOVED / RENAMED Requirements` sections. Accepted specs live in `openspec/specs/`; active work in `openspec/changes/`; history in `openspec/changes/archive/`.
- Never edit `openspec/specs/` directly to change behavior. Change behavior through a change's delta specs, then sync and archive through the workflow.
- Small maintenance work that does not alter accepted behavior (bug fixes, internal refactors, docs, dependency updates) may proceed without a proposal — see `openspec/specs/openspec-project-workflow/spec.md`.

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
- Every new or changed user-facing feature also requires a proportional **real desktop UI verification pass** against the built Avalonia application when the agent has an interactive desktop (e.g., Codex). Agents without an interactive desktop (e.g., OpenCode) mark the desktop verification task as not applicable to their runtime and leave it for a human or desktop-capable agent to complete; they do not block the deterministic baseline on it. Derive the pass from accepted scenarios and cover applicable keyboard, pointer, focus/selection, validation, filtering, destructive confirmation, persistence/restart, recovery, accessibility, and tab/window behavior.
- Desktop UI verification uses a disposable database/workspace, never the contributor's normal workspace. Record the tested build/environment, scenarios, results, isolation method, limitations, and material screenshots or automation logs in the change verification evidence.
- Keep desktop UI verification separate from the fast deterministic baseline. External-service tests and pixel-perfect visual regression remain outside the default baseline unless a feature specifically requires them.
- Baseline command — must pass before work is considered done. For agents with an interactive desktop it does not replace required desktop UI verification for a user-facing change; for agents without one, the deterministic baseline is the completion gate and the desktop pass is deferred:

  ```powershell
  dotnet test .\FusionCanvas.sln
  ```

## Security

- This is a **public GitHub repo**: never commit secrets, API keys, tokens, or credentials. Future AI/marketplace features must keep secrets out of source control.
- Keep NuGet packages current and address vulnerability warnings promptly (`dotnet list package --outdated`, `dotnet list package --vulnerable --include-transitive`).
- Treat external and user-controlled content as untrusted input. Guard against SQL injection, path traversal in workspace file handling, and prompt-injection-style attack vectors in current and future AI-facing code.

## QA Reviews

- The QA playbook lives in **`docs/qa-review.md`**. When the user asks for a QA review — full or a specific area (SOLID, architecture, testing, security, spec drift) — follow that document.
- A full QA review includes the QA-6 real desktop regression matrix for **all** accepted and implemented user-facing features. If the agent lacks an interactive desktop (e.g., OpenCode), report QA-6 as not applicable to the current runtime and defer it for a desktop-capable agent or human rather than marking it blocked.
- Running QA requires no OpenSpec ceremony. Findings that would change accepted behavior are routed through the OpenSpec workflow; specification drift is reconciled via an OpenSpec change.

## Working Agreement

- Windows + PowerShell environment; the `dotnet` CLI is available.
- Keep changes minimal and scoped; match existing conventions; do not fix unrelated issues unless asked.
- Do not run `git commit`, `git push`, or other git mutations unless explicitly asked.
- If a task is ambiguous or reveals a design/spec conflict, stop and ask rather than guessing.
