# FusionCanvas QA Review Playbook

This document defines the recurring QA review tasks for FusionCanvas. It is the operational playbook for AI coding agents (Codex, OpenCode, and similar tools) and human reviewers.

The durable requirements behind this process live in `openspec/specs/qa-review-baseline/spec.md` (once accepted). This playbook is living documentation: refine checklists, commands, and formats freely without OpenSpec ceremony. Changing *what QA must cover* is a requirement change and goes through the OpenSpec workflow.

## How to Run a QA Review

- **Full review:** run all tasks QA-1 through QA-6 in order, including the all-features real desktop regression, then produce one consolidated report. Trigger example: "Run a full QA review."
- **Partial review:** run only the requested task(s) by ID. Trigger examples: "Run QA-1 (SOLID)", "Do a security QA pass", "Check for spec drift", "Run QA-6 (desktop UI)."
- **Scope limiting:** a task may be limited to a layer or area (e.g. "QA-2 on the Integration layer only"). State the scope in the report.

### Delivery-Module Completion QA

Every delivery module receives a scoped completion review before it is reported complete. This is a change gate, not automatically a full repository QA review. Review the module's proposal, delta specs, design and implementation plan, tasks, code changes, and `verification.md`, then check:

1. Every acceptance scenario has a planned method, final result, and material evidence or explicit not-applicable rationale.
2. `dotnet build .\FusionCanvas.sln`, `dotnet test .\FusionCanvas.sln`, and strict validation of the active OpenSpec change pass.
3. The implementation stayed within the approved module and did not invent unapproved product behavior or architecture decisions.
4. Changed-scope spec/code/doc drift is absent.
5. Relevant architecture, security, persistence, migration, and recovery risks were reviewed.
6. A user-facing module has targeted real-desktop evidence or an explicit unavailable-environment handoff. Scenario selection prioritizes critical workflows and distinct high-risk behavior; equivalent low-risk variants may rely on deterministic tests.

If a criterion or validation gate fails, return the module to correction and rerun the affected criterion plus relevant regression checks. Do not convert a failure into a limitation merely to close the change. Expand to the relevant QA tasks—or a full review—when the module affects broad shell/navigation behavior, crosses many capabilities, or creates plausible unrelated regressions.

For the completion record, add an acceptance table to `verification.md`:

```markdown
| Capability / scenario | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| `<requirement> / <scenario>` | Unit / integration / desktop / inspection | Pass / Fail / N/A | `<test, command, screenshot, notes>` |
```

### Review Protocol (applies to every task)

1. **Load context first.** Read `AGENTS.md`, `openspec/project.md`, and the specs relevant to the task (`architecture-guidelines`, `testing-baseline`, capability specs under review) before judging anything.
2. **Establish the build state.** Run `dotnet build .\FusionCanvas.sln` early. A broken build is itself a Critical finding; note it and continue the review where practical.
3. **Review, don't fix.** A QA run reports findings; it does not change code, specs, or docs unless the user explicitly asks for fixes. Fixing happens afterwards through the routing rules below.
4. **Report in the standard format** (see below). Every finding cites concrete evidence: file paths, line numbers, commands, or spec references.
5. **No findings is a valid outcome.** Report it as a pass and say what was checked.
6. **Do not overstate unavailable verification.** If an interactive desktop or another required test environment is unavailable, mark the affected task and scenarios **Not applicable** (for example, when running under OpenCode, which has no display) and state which environment would be required. A code-level test pass is not a desktop UI pass, but a not-applicable desktop pass does not block the rest of the review.

### Severity Scale

| Severity | Meaning | Examples |
| --- | --- | --- |
| **Critical** | Must fix immediately; security exposure, data loss risk, broken build/tests | Committed secret, vulnerable package with known exploit, failing baseline suite |
| **High** | Violates an accepted spec or architecture boundary; will compound if ignored | Domain project referencing SQLite, missing tests for new domain rules, spec drift |
| **Medium** | Deviates from guidelines; hurts maintainability over time | God class forming, outdated (non-vulnerable) packages, unjustified abstraction layer |
| **Low** | Minor inconsistency or style deviation | Naming inconsistencies, small duplication, doc typos |
| **Info** | Observation, suggestion, or positive confirmation | "This boundary is well-protected", optional improvements |

### Report Format

Present the report in the chat/session. If a persistent copy is useful, save it to `TestResults/qa/YYYY-MM-DD-qa-review.md` (`TestResults/` is gitignored — do not commit reports).

```markdown
# QA Review — <date> — Scope: <full | QA-n ...>
Build: <pass/fail> | Tests: <pass/fail, n tests> | Desktop UI: <pass/fail/blocked/N/A> | Reviewer: <agent/tool>

## Summary
| Task | Result | Critical | High | Medium | Low | Info |
|------|--------|----------|------|--------|-----|------|
| QA-1 SOLID & Design | Issues found | 0 | 1 | 2 | 3 | 1 |
| QA-6 Desktop UI Regression | Pass | 0 | 0 | 0 | 0 | 1 |
| ... |

## Desktop UI Feature Matrix
| Capability | Feature / primary scenario | Interaction dimensions | Persistence / restart | Result | Evidence |
|------------|----------------------------|------------------------|-----------------------|--------|----------|
| `<spec>` | `<workflow>` | Keyboard, pointer, focus, ... | Pass / N/A | Pass / Fail / Blocked / N/A | `<log, screenshot, notes>` |

## Findings
### QA-1 — SOLID Principles & Code Design
- **[High] SRP violation** — `src/.../StoreManagement.cs:45-210`
  Description of the problem and evidence.
  Recommendation: concrete suggested fix and routing (direct fix / OpenSpec change).

## Verdict
<one paragraph: overall state, top priorities, recommended next actions>
```

## Handling Findings (Fix Routing)

| Finding type | Route |
| --- | --- |
| Internal issue with no behavior change (style, structure, dead code, missing XML docs) | Fix directly as maintenance; run the test baseline after |
| Missing tests for existing accepted behavior | Add tests directly as maintenance, unless the behavior itself is unclear or unspecified — then clarify via OpenSpec first |
| Missing real desktop coverage for an accepted user-facing feature | Add the feature-scoped UI verification and evidence directly; use OpenSpec first only when expected behavior is unclear |
| Desktop UI behavior contradicts an accepted spec | Report the product failure and route through OpenSpec to fix the code or amend the spec |
| Code contradicts an accepted spec | OpenSpec change: fix the code **or** amend the spec, whichever is wrong |
| Code implements behavior not covered by any spec | OpenSpec change: accept and document the behavior, or remove it |
| Security finding rated Critical | Fix immediately; note the exposure in the report (never quote the secret itself) |
| Outdated or vulnerable NuGet packages | Direct maintenance update (precedent: `codex/fix-vulnerable-package-warning`); run baseline after |
| Spec/doc drift that does not match reality | OpenSpec change syncing the correct side; never silently edit both |

A QA run itself requires **no** OpenSpec ceremony, just like running the test suite. Only the *fixes that change accepted behavior* go through propose → apply → archive.

### Suggested Cadence

- **Full review:** after a milestone or a batch of archived changes; before any release. Every run includes a fresh QA-6 all-features desktop matrix against the current build.
- **QA-4 (Security):** frequently — package vulnerabilities appear at any time; cheap to run.
- **QA-3 (Testing):** when module completion QA reveals broader test-quality or coverage uncertainty; the deterministic baseline and criterion mapping still run for every module.
- **QA-5 (Drift):** after archiving changes, to confirm specs, docs, and code stayed aligned.
- **QA-6 (Desktop UI):** targeted and risk-based for every user-facing delivery module; comprehensive at milestones, before releases, after broad cross-cutting UI changes, and in every full review.

---

## QA-1 — SOLID Principles & Code Design

**Goal:** verify the code applies SOLID pragmatically — well-organized, loosely coupled, without dogmatic over-engineering.

This review judges *reasonable* application of the principles, per `openspec/specs/architecture-guidelines/spec.md` ("abstractions protect a real boundary, variation point, or testable contract"). It is **not** a goal to create interfaces for everything or split every method into its own class.

### Checklist

1. **Single responsibility (sensible):**
   - Look for god classes: classes carrying many unrelated responsibilities. Signal, not a hard rule: roughly >300–400 lines, many injected dependencies, or methods operating on disjoint state.
   - Look for god methods: very long methods mixing levels of abstraction (validation + orchestration + persistence + formatting in one body).
   - Counter-check: also flag *over*-splitting — chains of trivial one-method classes that obscure simple logic.
2. **Dependency management:**
   - Dependencies arrive via constructors, not service locators, static state, or `new` of infrastructural types inside business logic.
   - No hidden global state; singletons only where genuinely justified.
3. **Abstractions are justified:**
   - Each interface protects a real boundary (persistence, file system, external service), a variation point, or a test seam. Flag interface-per-class noise with a single trivial implementation and no boundary.
   - Flag concrete types that *should* be behind a contract — e.g. UI or application code directly constructing Integration types (composition root excepted).
4. **Open/closed and Liskov, pragmatically:**
   - New variation should not require editing stable core code each time (e.g. stage tools, future plugins/AI providers should be addable via extension points).
   - Inheritance is rare and correct; composition preferred.
5. **Duplication:** repeated logic blocks that should be shared, and conversely, premature shared helpers coupling unrelated features.

### Method

- Read the production code under `src/`, layer by layer. Use file size and structure as triage, then read the suspicious types fully.
- Cross-check against `docs/architecture.md` ("Architectural Principles") and the architecture-guidelines spec.

---

## QA-2 — Clean Architecture & Dependency Direction

**Goal:** verify the Clean Architecture boundaries hold: right layer, inward dependencies, domain purity.

### Checklist

1. **Project dependency graph is inward-only:**

   ```powershell
   dotnet list src\FusionCanvas.Domain\FusionCanvas.Domain.csproj reference
   dotnet list src\FusionCanvas.Application\FusionCanvas.Application.csproj reference
   dotnet list src\FusionCanvas.Integration\FusionCanvas.Integration.csproj reference
   dotnet list src\FusionCanvas.App\FusionCanvas.App.csproj reference
   ```

   Expected: Domain → nothing; Application → Domain; Integration → Application (→ Domain); App → Application (→ Domain; Integration reference allowed only for composition/bootstrapping). Flag any outward or sideways leak, and any test project referencing the wrong layers.

2. **Domain purity:** search `src/FusionCanvas.Domain` for forbidden references: `Avalonia`, `Microsoft.Data.Sqlite`, `System.Net.Http`, file I/O (`System.IO` beyond pure path types), external SDKs. Domain must stay framework-free.
3. **Responsibilities in the right layer:**
   - Business rules, invariants, calculations → Domain. Flag business logic in view models, code-behind, or Integration adapters.
   - Use-case orchestration and ports → Application. Flag orchestration living in the App layer.
   - Persistence/file system/external systems → Integration, behind Application contracts. Flag SQL or file access leaking into Application/Domain/App.
   - Presentation state, navigation, commands → App. Flag views/view models owning domain decisions.
4. **Contracts point the right way:** interfaces consumed by Application use cases are *defined* in Application (or Domain), implemented in Integration — not the reverse.
5. **UI layer discipline:** view models contain no persistence calls; code-behind limited to view concerns; bindings follow compiled-binding conventions already in use.
6. **Naming and structure consistency:** project names, namespaces, and folder layout match the accepted architecture (note: the layer is `Integration`; flag docs that say otherwise via QA-5).

### Method

- Run the reference commands, then grep/read code for the purity checks. Compare actual placement against `docs/architecture.md` and the architecture-guidelines spec.

---

## QA-3 — Testing & Coverage

**Goal:** verify the testing baseline passes and that behavior is genuinely protected by focused tests, per `openspec/specs/testing-baseline/spec.md`.

### Checklist

1. **Baseline passes:**

   ```powershell
   dotnet test .\FusionCanvas.sln
   ```

   A failing or non-building suite is Critical. The suite must not require network, external services, or a running UI.

2. **Structural coverage — behavior has tests:**
   - Map production behavior types to tests: for each public domain rule/invariant, application use case, integration adapter, and UI decision-logic type, is there a corresponding test class in the mirrored project (`tests/FusionCanvas.<Layer>.Tests`)? List unprotected behavior.
   - Optional coverage data: `dotnet test .\FusionCanvas.sln --collect:"XPlat Code Coverage"` (coverlet collector is already referenced; results land in `TestResults/`). Use the data to find untested branches in Domain/Application — treat percentage numbers as a signal, not a gate.
3. **Test quality, per layer:**
   - Domain tests: no frameworks, persistence, or file system.
   - Application tests: deterministic collaborators (fakes/stubs), no real SQLite or file system.
   - Integration tests: isolated temporary resources (temp DB/files), no shared state, clean up after themselves.
   - App tests: exercise UI-owned decision logic (view models, navigation, commands) in code without launching Avalonia; no superficial tests of static markup.
4. **Spec-driven coverage:** behavior described by accepted spec requirements/scenarios has corresponding tests, or an explicit documented reason why not.
5. **Scope discipline:** no slow, flaky, UI-driving, or external-service tests sneaking into the fast solution-level baseline. Real desktop verification is expected where applicable but runs separately under the feature workflow or QA-6, and is Not applicable when the reviewing agent has no interactive desktop (OpenCode).

---

## QA-4 — Security & Vulnerabilities

**Goal:** keep dependencies clean, secrets out of the public repo, and code resistant to relevant attack vectors.

Context: FusionCanvas is a local-first desktop app with no network attack surface today, but the repo is public, and AI/marketplace features with API keys are planned.

### Checklist

1. **NuGet package hygiene:**

   ```powershell
   dotnet list package --vulnerable --include-transitive
   dotnet list package --deprecated
   dotnet list package --outdated
   ```

   - Vulnerable packages → Critical/High depending on severity and reachability.
   - Deprecated packages → High/Medium.
   - Outdated packages → Medium/Low; recommend update batches. Major-version jumps get a note about breaking changes.

2. **Secrets scan (repo is public):**
   - Search tracked files for secret-shaped content: `git grep -n -i -E "(api[_-]?key|api[_-]?secret|secret[_-]?key|access[_-]?token|private[_-]?key|password|passwd|BEGIN [A-Z ]*PRIVATE KEY|sk-[A-Za-z0-9])" -- .` (review matches manually; many will be false positives like the word "password" in docs).
   - Check config files (`*.json`, `*.toml`, `*.yaml`, `*.config`, `appsettings*`, `.env*`) for embedded credentials.
   - Verify `.gitignore` would exclude common secret files (`.env*`, `*.pfx`, `*.snk`, local settings) — extend it if a gap appears.
   - If a real secret is found: **Critical** — do not quote it in the report, recommend revocation/rotation and history scrub; never "fix" by committing a redaction over it silently.
3. **Injection and attack-vector review (proportionate to current code):**
   - **SQL injection:** all SQLite access (`Microsoft.Data.Sqlite`) uses parameterized commands; no string-concatenated SQL with user input.
   - **Path traversal:** workspace file storage validates/normalizes paths; user- or file-supplied names cannot escape the managed workspace directory.
   - **Prompt injection (forward-looking):** any code that assembles AI prompts must treat workspace/user content as data, not instructions; flag places where untrusted content would be interpolated into system-level instructions without separation or sanitization.
   - **Untrusted deserialization:** JSON/metadata parsing of workspace files handles malformed input gracefully (no unguarded `dynamic`/type-unsafe deserialization).
4. **Future-facing note:** API key storage plans (AI providers, marketplaces) must use OS-level secure storage or user-local settings outside the repo — flag any design artifact suggesting keys in config files committed to source control.

---

## QA-5 — Specification & Documentation Drift

**Goal:** keep OpenSpec specs, `docs/`, and code aligned so the specification-first process stays trustworthy.

### Checklist

1. **Specs vs. code:**
   - For each capability in `openspec/specs/`: does implemented code contradict any requirement or scenario? (Contradiction = drift; missing-but-planned behavior is normal — verify it's tracked by an active change or roadmap intent.)
   - Does the code expose accepted-looking behavior that no spec covers? Route per the fix table.
2. **Change hygiene:**
   - `openspec list` — no stale active changes that were implemented but never archived.
   - No unsynced delta specs: delta `ADDED/MODIFIED/REMOVED` sections in `openspec/changes/` (non-archived) should reflect intended, not-yet-applied work only.
3. **Docs vs. specs vs. code:**
   - `README.md`, `docs/*.md`, `openspec/project.md` agree on layer names, project structure, test commands, and workflow. (Known historical example: `docs/architecture.md` used the name "Infrastructure" while the accepted name is "Integration".)
   - `docs/LifeOS/` remains optional historical reference; canonical documents do not treat its inventory, ordering, or acceptance text as current authority, and reused ideas still go through discovery and OpenSpec.
   - `AGENTS.md` remains consistent with this playbook and the workflow skills in `.codex/skills/`.
4. **Archived context preserved:** archived changes exist under `openspec/changes/archive/` with their artifacts; nothing was casually deleted.

### Method

- Use `openspec list --json` and `openspec status --change <name> --json` for change state; read specs and compare against `src/` structure and key behaviors; skim docs for contradictions.

---

## QA-6 — Real Desktop UI Regression (All Features)

**Goal:** verify the current built Avalonia application end to end across every accepted and implemented user-facing feature. In a full QA review, this is a complete feature inventory and regression matrix—not a smoke test and not a sample of recently changed features.

### Required Environment and Safety

1. Use an interactive desktop session capable of launching the built application and delivering actual keyboard, pointer, or accessibility-driven input. Windows UI Automation is currently suitable, but the policy does not depend on one tool.
2. Build the current checkout before testing and record the executable/configuration, commit or working-tree identity, operating environment, and automation mechanism.
3. Select a disposable database/workspace fixture before the first mutating interaction. Never run create, move, archive, restore, or delete scenarios against the contributor's normal workspace.
4. Prefer stable automation identifiers and accessible names. Wait for observable UI state rather than relying on fixed sleeps. Treat missing accessibility exposure as a finding when it prevents reliable operation or evidence.
5. If the interactive environment cannot be obtained (for example, when running under OpenCode, which has no display), mark QA-6 **Not applicable**, list every unexecuted matrix row, and state what environment is required. Do not infer a pass from QA-3. A not-applicable QA-6 does not block the rest of the review.

### Build the All-Features Inventory

1. Read every accepted capability under `openspec/specs/` and identify user-facing requirements that are implemented in the current build.
2. Cross-check the application shell, navigation surfaces, menus, dialogs, editors, inspectors, tabs/windows, and implemented workflows so UI behavior that lacks a spec is also visible as a QA-5 drift finding.
3. Create one or more matrix rows for every implemented user-facing feature. Each row names the capability/spec requirement and its primary end-to-end workflow.
4. Assign every row exactly one result: **Pass**, **Fail**, **Blocked**, or **Not applicable**. Explain Blocked and Not applicable. No feature may be silently omitted.

### Full UI Test Specification

For **every inventory row**, execute the primary happy path through the real desktop application and apply each relevant dimension below. Mark a dimension Not applicable only with an evident feature-specific reason.

1. **Launch and discoverability:** the feature is reachable from the intended workspace surface; initial, empty, loading, and disabled states are coherent where applicable.
2. **Keyboard operation:** shortcuts, traversal, Enter/Escape behavior, inline editing, and focus return work without unintended global-command leakage.
3. **Pointer operation:** click, double-click, right-click/context menu, drag/drop, hit targets, and hover/drop feedback work where the feature exposes them.
4. **Selection and focus:** canonical selection, multi-surface synchronization, inspector/detail updates, and focus ownership remain correct before and after the action.
5. **Validation and errors:** invalid input is rejected visibly, editable state and user input are retained when appropriate, and recoverable failures do not leave misleading UI state.
6. **Visibility and filtering:** search, filters, expansion/collapse, hidden selections, empty results, and restoration of pre-filter context behave correctly where applicable.
7. **Destructive and lifecycle actions:** confirmation text accurately names impact; cancel is safe; confirmed archive/restore/delete behavior and selection fallback match the accepted spec.
8. **Persistence and restart:** saved changes survive a real application restart with identity, hierarchy/order, selection-relevant state, and displayed properties reconstructed correctly where specified.
9. **Recovery and atomicity:** failed persistence or interrupted operations retain or restore the last confirmed state and expose actionable feedback when the scenario can be safely induced.
10. **Accessibility exposure:** actionable controls have stable accessible names/roles/identifiers, state changes are observable, and the feature remains operable through supported accessibility-driven input.
11. **Tabs and windows:** normal selection versus explicit tab/window opening, activation, duplication prevention, close behavior, and unsaved-change protection work where applicable.
12. **Cross-feature regression:** complete at least one realistic workflow that crosses the feature's upstream/downstream integrations so individually passing controls are also verified together.

### Evidence and Reporting

- Record the full feature matrix in the consolidated report using the standard columns above. Add columns when a feature needs more precise risk tracking.
- For each row, record concise observed results and link or identify relevant screenshots, accessibility/automation output, or restart evidence. Evidence must establish behavior, not merely that a control existed.
- Record the disposable fixture and confirm the normal workspace was not mutated.
- Report product failures with severity and route them using the normal finding rules. Report test-infrastructure limitations separately so they are not confused with application defects.
- A full QA review can pass QA-6 when every implemented user-facing feature row passes or has a justified Not applicable result; any Failed row fails QA-6. When the reviewing agent has no interactive desktop (for example, OpenCode), QA-6 is Not applicable for the whole matrix and the rest of the review is unaffected.

---

## Maintaining This Playbook

- Update checklists, feature-matrix columns, and commands as the stack evolves (e.g. promote repeatable scenarios into a dedicated UI harness when useful).
- Adding/removing a QA area or changing severities' meaning changes the QA baseline requirements — propose it through OpenSpec.
