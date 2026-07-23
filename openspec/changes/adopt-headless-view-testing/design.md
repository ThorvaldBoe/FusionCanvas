## Context

FusionCanvas currently has extensive `FusionCanvas.App.Tests` coverage of view models, commands, navigation state, and UI coordination, but the test project references no Avalonia headless package and contains no tests that instantiate or interact with Avalonia views. Current policy instead expects targeted live-desktop verification when a display is available and preserves handoffs when it is not. That lane is slow and asymmetric between Codex and OpenCode.

This process-only change makes deterministic Avalonia headless view tests the normal framework-level UI lane and makes live desktop testing optional. It does not introduce the harness itself; the current absence is documented so a later bounded implementation can choose the Avalonia 12-compatible package and fixture after validating its APIs.

## Goals / Non-Goals

**Goals:**

- Establish one routine UI verification policy that Codex, OpenCode, CI, and humans can execute.
- Preserve the existing preference for framework-free view-model tests when they provide sufficient confidence.
- Require focused headless view tests only where Avalonia behavior is material.
- Remove live desktop runs and unavailable-environment handoffs from module-completion and full-QA gates.
- Keep optional live testing available for platform, accessibility, visual, native-window, and difficult defect investigation.

**Non-Goals:**

- Add `Avalonia.Headless`, `Avalonia.Headless.XUnit`, or another package in this change.
- Build the initial headless fixture or representative view tests.
- Claim that headless tests replace visual judgment, native-window integration, operating-system accessibility tooling, or all end-to-end testing.
- Change application runtime behavior or user experience.

## Decisions

### Use the lowest reliable test layer

Domain and application behavior remains outside the UI framework. UI-owned decision logic stays in view-model or coordinator tests. Avalonia headless tests are added only for meaningful view construction, binding, control-state, routed-input, focus, or visual-tree risks.

Alternative considered: require a headless test for every XAML file. Rejected because existence tests for static markup are brittle and add little information.

### Put headless tests in the normal deterministic lane

The eventual harness and view tests will run through `dotnet test .\FusionCanvas.sln` and must not require a display, network access, external services, or a running installed application. This makes the same completion routine available to Codex, OpenCode, CI, and humans.

Alternative considered: keep headless view tests in a separate optional command. Rejected because that would preserve the routine coverage gap and make omission easy.

### Make live desktop verification genuinely optional

Planning artifacts and completion evidence no longer require a live-desktop scenario matrix, a Codex-only pass, or an OpenCode handoff. Contributors may select an ad hoc live check when it answers a specific risk that deterministic tests cannot.

Alternative considered: retain mandatory live runs for releases and full QA. Rejected because the requested routine must be executable in both agent environments; a release team may still choose a live pass without encoding it as a universal repository gate.

### Separate policy adoption from harness implementation

This change updates the authoritative routine and explicitly records that no headless tests exist. A follow-up change will select the Avalonia 12-compatible test packages, establish application initialization and dispatcher isolation, and add representative view tests.

Alternative considered: add the harness while changing policy. Rejected because the user asked for documentation/routine changes and an inventory; package/API selection is a distinct implementation surface that should be verified independently.

## Risks / Trade-offs

- [Headless rendering differs from a real Windows desktop] → Limit claims to framework-level behavior and retain optional live checks for platform-specific risks.
- [Headless tests can become slow or brittle] → Test meaningful behavior at the lowest reliable layer and avoid static-markup or pixel-perfect assertions by default.
- [Policy temporarily names a lane that has no harness] → State the current gap clearly and track harness creation as the next bounded change.
- [Existing active change artifacts still describe mandatory desktop testing] → Treat `adopt-headless-view-testing` as the successor policy and reconcile canonical accepted specs and operational docs in this change.

## Implementation Plan

1. Update the `testing-baseline` delta to require focused Avalonia headless view tests where valuable, retain framework-free UI decision tests, and make live desktop checks optional.
2. Update the `qa-review-baseline` delta so completion QA and full QA depend only on deterministic, cross-agent checks.
3. Revise `AGENTS.md`, `openspec/project.md`, `docs/architecture.md`, `docs/qa-review.md`, `README.md`, and the repository OpenSpec propose/apply/archive skill instructions to describe the same headless-first routine and remove Codex/OpenCode-specific desktop handoffs.
4. Preserve safety guidance for any optional live run: use isolated disposable data and report it only as supplemental evidence.
5. Record criterion-level verification in `verification.md`, run strict OpenSpec validation, run `git diff --check`, and run the solution test baseline because the routine documentation references that command.
6. Do not add packages, fixtures, or test code. Escalate any decision about headless package/API selection to the follow-up implementation change.

## Acceptance-to-Verification Mapping

| Acceptance scenario | Planned verification |
| --- | --- |
| A user-facing view changes | Inspect accepted spec and canonical docs for a consistent headless-test expectation; strict OpenSpec validation |
| Decision logic can be tested without Avalonia | Inspect guidance for lowest-reliable-layer wording |
| Static markup has no meaningful behavior | Inspect guidance for explicit omission rationale |
| Contributor completes a user-facing module | Confirm no canonical routine makes live desktop testing a completion gate |
| A risk is not represented by headless testing | Confirm optional ad hoc examples and supplemental-evidence wording |
| Live desktop testing is unavailable | Confirm Codex and OpenCode share the same mandatory routine |
| Verification is planned / recorded | Validate delta and verification table |
| Contributor runs baseline / inspects layout | Run `dotnet test .\FusionCanvas.sln`; inspect project layout |
| Navigation logic changes | Inspect updated testing policy |
| Contributor evaluates test scope | Inspect deterministic/headless scope rules |
| Module completion is reviewed / user-facing module / broad risk | Inspect QA baseline and playbook |
| Testing review executes / gaps found | Inspect QA-3 checklist and run baseline |
| Full QA requested | Confirm all mandatory tasks are executable without a display |
| Reviewer elects live desktop check | Confirm it is optional supplemental evidence |

## Migration Plan

1. Apply the delta requirements and canonical documentation updates.
2. Sync the deltas into accepted specs after verification.
3. Use the new routine for subsequent delivery modules.
4. Propose a separate change for the Avalonia headless harness and representative tests.

Rollback is documentation-only: revert the policy and accepted-spec updates. No runtime or data migration is involved.

## Open Questions

None for this policy change. Exact Avalonia headless packages, fixture lifetime, dispatcher isolation, and the first representative views remain decisions for the follow-up implementation change.
