# Adopt Module Delivery Workflow — Verification

## Scope and Environment

- Change type: process, specification, documentation, and shared-agent-skill change; no production runtime or application UI behavior changed.
- Environment: Windows, .NET SDK 10.0.302, OpenSpec CLI, repository worktree `C:\Users\boe74\.codex\worktrees\cc93\FusionCanvas`.
- Desktop UI lane: Not applicable. The change has no application-facing path, persistence, visual state, or interactive workflow. No disposable application data was required.

## Acceptance Evidence

| Capability / scenario | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| Workflow / Contributor defines the next module | Document inspection | Pass | `openspec/project.md`, `AGENTS.md`, `openspec/config.yaml`, and `.codex/skills/openspec-propose/SKILL.md` define outcome, boundaries, dependencies, risks, verification, and scope rationale. |
| Workflow / Proposed module is too broad | Document inspection | Pass | Module rules require splitting independent outcomes, unresolved high-impact decisions, or undiagnosable verification scope. |
| Workflow / Several small features share delivery cost | Document inspection | Pass | Module rules allow grouping when outcome, data, surface, fixture, and acceptance cost are shared and justified. |
| Workflow / Module behavior is being discovered | Document inspection | Pass | `openspec/project.md`, `AGENTS.md`, and the propose/explore skills require collaborative examples, non-goals, edge cases, assumptions, dependencies, and decisions. |
| Workflow / High-impact ambiguity remains | Skill-path inspection | Pass | Propose/apply skills and `AGENTS.md` prevent implementation and require escalation rather than guessing. |
| Workflow / Bounded implementation is delegated | Document inspection | Pass | `AGENTS.md` defines the required handoff fields and current Codex/Kimi K3/GLM 5.2 routing examples. |
| Workflow / Implementation exposes a missing decision | Skill-path inspection | Pass | Apply skill pauses on missing product, UX, data, architecture, or acceptance decisions. |
| Workflow / High-judgment work is assigned | Document inspection | Pass | Capability-based routing reserves discovery, specification, design review, ambiguous correction, and acceptance review for sufficiently capable agents or humans. |
| Workflow / Implementation readiness is reviewed | Skill-path inspection | Pass | Propose and apply skills enforce observable scenarios, a detailed `Implementation Plan`, evidence mapping, bounded tasks, and no unresolved high-impact decisions. |
| Workflow / Acceptance criterion fails | Skill-path inspection | Pass | Apply skill and QA playbook require correction plus rerunning the criterion and relevant regressions; completion with hidden failures is prohibited. |
| Workflow / Module is reported complete | Skill-path inspection | Pass | Apply/archive skills and `docs/qa-review.md` require criterion-level `verification.md`, validation commands, limitations, and environment handoffs. |
| Workflow / Contributor explores a future module | Document inspection | Pass | Canonical guidance marks `docs/LifeOS` optional and requires revalidation against current intent, specs, and application state. |
| Workflow / Contributor defines current scope | Repository scan | Pass | No canonical non-historical document instructs agents to infer current scope, priority, ordering, or acceptance from LifeOS/PRD material. |
| Workflow / Contributor needs accepted behavior | Document inspection | Pass | `AGENTS.md`, `openspec/project.md`, and configuration retain `openspec/specs` as the durable source of truth. |
| Workflow / Module is proposed | Artifact inspection | Pass | The delivery-package definitions require proposal, design, delta specs, detailed implementation plan, tasks, and planned evidence mapping. This change itself contains those artifacts. |
| Workflow / Module enters implementation | Skill-path inspection | Pass | Propose/apply guidance requires approval or explicit delegation before implementation and constrains work to approved boundaries. |
| Workflow / Module is completed | Skill-path inspection | Pass | Apply/archive guidance requires passing acceptance and validation evidence plus a learning review before archive. |
| Workflow / Contributor writes module requirements | Artifact and rule inspection | Pass | OpenSpec delta specs remain observable and implementation-independent; `openspec/config.yaml` and propose guidance enforce boundaries and acceptance scenarios. |
| Workflow / Contributor prepares implementation guidance | Artifact and rule inspection | Pass | `design.md` contains `## Implementation Plan`; configuration/propose/apply skills specify the required layer, data, UI, edge-case, sequence, test, and migration detail. |
| Workflow / Implementation details change without behavior change | Document inspection | Pass | `openspec/project.md`, `AGENTS.md`, and the change design separate durable behavior from change-specific implementation guidance. |
| Testing / Verification is planned | Artifact and skill inspection | Pass | Change delta specs plus propose/apply guidance require a planned method or N/A rationale for every acceptance scenario. |
| Testing / Verification is recorded | Artifact inspection | Pass | This table records scenario-level methods/results/evidence; aggregate commands are recorded separately below. |
| Testing / User-facing module plans desktop verification | Document inspection | Pass | `AGENTS.md`, `docs/architecture.md`, `docs/qa-review.md`, testing deltas, and skills select critical/high-risk desktop scenarios and require a sufficiency rationale. Condition is N/A for this non-UI module. |
| Testing / Variants are equivalent and low risk | Document inspection | Pass | Canonical guidance permits representative desktop variants only when deterministic tests cover remaining rule combinations. |
| Testing / Interactive desktop is unavailable | Document inspection | Pass | Guidance consistently records the lane as N/A and preserves a scenario handoff rather than inferring pass. |
| Testing / Broad regression is warranted | Document inspection | Pass | Full all-features desktop regression is reserved for full QA, milestones, release candidates, or broad cross-cutting UI risk. Condition is N/A for this documentation-only module. |
| QA / Module completion is reviewed | Completion QA | Pass | Strict OpenSpec validation, repository scan, full deterministic baseline, diff check, and criterion-level evidence are included in this record. |
| QA / Module changes user-facing behavior | Applicability review | N/A | No production code, Avalonia view, interaction, persistence, or visible application behavior changed. |
| QA / Module has broad cross-cutting risk | Risk review | Pass | Process guidance is cross-cutting, so validation covered all active changes and all accepted specs; application desktop regression is not warranted because runtime behavior is unchanged. |
| QA / Delegated implementation is reviewed | Changed-scope review | Pass | Final diff review confirms changes stay within the approved documents, skills, active UI-baseline reconciliation, and new OpenSpec change; no product behavior was invented. |
| QA / Review finds a failed gate | Process-path inspection | Pass | QA and apply guidance explicitly return failed criteria and validation gates to correction and re-verification. The initial file-lock/formatting issues were corrected before completion. |

## Validation Results

- `openspec validate --all --strict`: Pass — 27 items passed, 0 failed.
- Accepted spec sync: Pass — deltas were merged into `openspec/specs/openspec-project-workflow/spec.md`, `openspec/specs/testing-baseline/spec.md`, and `openspec/specs/qa-review-baseline/spec.md`; strict validation remained green afterwards.
- Canonical PRD-first instruction scan: Pass — no stale instruction found outside historical material and archived/active change history.
- Contradictory exhaustive UI-policy scan: Pass — no canonical match; old wording remains only inside preserved historical LifeOS files.
- `dotnet test .\FusionCanvas.sln -m:1`: Pass — 329 tests passed (28 Domain, 136 Application, 29 Integration, 136 App), 0 failed, 0 skipped. Existing xUnit analyzer warnings remain outside this process-only scope.
- First parallel baseline attempt: Inconclusive due to an Avalonia-generated App assembly file lock; 193 tests had passed and no assertion failed. Orphaned processes from that run were stopped, then the serialized full baseline above passed.
- `git diff --check`: Pass — no whitespace errors (line-ending conversion warnings only).

## Limitations and Deferred Checks

- No real desktop UI test was run because this process change has no application-facing behavior.
- The current model-routing examples are intentionally operational guidance. The normative workflow remains capability-based so model names can change without a specification update.
