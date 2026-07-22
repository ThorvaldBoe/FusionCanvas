## 1. Canonical Workflow Guidance

- [x] 1.1 Update `openspec/project.md` and `openspec/config.yaml` to define rolling module delivery, implementation-ready artifacts, acceptance gates, and optional historical inputs
- [x] 1.2 Update `AGENTS.md` with the module lifecycle, scope/readiness checks, capability-based agent routing, ambiguity escalation, and verification retry loop
- [x] 1.3 Replace the phase-oriented `docs/roadmap.md` with a rolling current/next/later module format

## 2. OpenSpec Operational Skills

- [x] 2.1 Update `openspec-propose` to capture module definition, shared-understanding decisions, acceptance scenarios, and a detailed implementation plan
- [x] 2.2 Update `openspec-apply-change` to enforce readiness, bounded implementation, acceptance-evidence mapping, and correction loops
- [x] 2.3 Update `openspec-archive-change` to require completed verification evidence and learning review
- [x] 2.4 Update `openspec-explore` to support module discovery and prevent premature long-range artifact creation

## 3. QA and Testing Guidance

- [x] 3.1 Update `docs/qa-review.md` with module-scoped completion QA and criterion-level evidence checks
- [x] 3.2 Update `docs/architecture.md` to describe targeted risk-based module desktop verification and milestone/release regression
- [x] 3.3 Reconcile the active `add-ui-testing-baseline` design and delta specs with risk-based desktop test allocation and unavailable-environment handoff

## 4. Product and Historical Documentation

- [x] 4.1 Update `README.md` and `docs/README.md` to describe the rolling delivery workflow and historical status of LifeOS material
- [x] 4.2 Update `docs/LifeOS/README.md` to label LifeOS documents as optional, potentially stale historical reference while preserving the files
- [x] 4.3 Replace remaining current-document claims of PRD authority in `docs/strategic-decisions.md` and other canonical guidance without rewriting archived history

## 5. Verification and Evidence

- [x] 5.1 Create `verification.md` mapping this change's acceptance criteria to document inspection and validation evidence
- [x] 5.2 Run strict OpenSpec validation for this and all active changes and correct relevant failures
- [x] 5.3 Scan canonical non-historical documents for stale PRD-first instructions and contradictory exhaustive per-feature UI guidance
- [x] 5.4 Run `git diff --check` and `dotnet test .\FusionCanvas.sln`, then record results and limitations
