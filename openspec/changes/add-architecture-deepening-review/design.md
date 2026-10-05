## Context

The Software Factory already audits dependency direction, abstraction ownership, responsibility cohesion, testability, and anti-overengineering. Its audit model is intentionally evidence-backed and type-oriented. Matt Pocock's engineering skills add a useful design vocabulary and a proactive workflow for finding deepening opportunities, but importing the repository unchanged would introduce another process authority and document conventions that do not match FusionCanvas's OpenSpec and Factory model.

This module adds a design-review procedure around the existing Factory evidence. It is not a product feature, runtime dependency, new audit axis, or replacement for OpenSpec. It is a way to turn architectural observations into bounded, reviewable candidates before implementation.

## Goals / Non-Goals

**Goals:**

- Make depth, leverage, locality, seams, adapters, and interface-as-test-surface explicit in Factory architecture reviews.
- Identify shallow-module candidates using existing source extracts, dependency context, callers, implementations, registrations, and tests.
- Prevent speculative interface proliferation by requiring an earned seam and a concrete benefit.
- Separate improvement recommendations from compliance findings and certification.
- Provide a controlled handoff to OpenSpec or the maintenance workflow.

**Non-Goals:**

- Do not clone, vendor, or execute Matt Pocock's repository as a Factory dependency.
- Do not create a seventh QA axis or change the meaning of existing audit results.
- Do not require every class to have an interface, adapter, or separate module.
- Do not automatically refactor code or create implementation tasks before a candidate is selected.
- Do not create a generic domain glossary or ADR system as part of this module; add those only when a later change has a real need.

## Decisions

### 1. Add a supporting architecture procedure, not a new axis

The procedure belongs beside the Architecture & Code Structure audit guidance. Deepening is a design improvement lens that can use evidence from architecture, functionality, and testing; making it an axis would duplicate existing certification semantics and encourage treating a judgment call as a pass/fail rule.

**Alternative rejected:** add a mandatory deep-module checkpoint to every architecture assessment. This would create ceremony for low-risk types and conflict with the Factory's risk-directed profiles.

### 2. Use the existing type extract as the starting inventory

The review starts from fully qualified types and their discovered relationships. It may expand to callers, implementations, registrations, tests, and adjacent types when a candidate requires more context. The review records the source revision and working-tree state so recommendations remain traceable.

**Alternative rejected:** introduce a separate file- or module-inventory format. The existing type extract is the Factory's stable navigation unit and already supports the context needed for deepening analysis.

### 3. Record candidates before designs

The first output is a candidate record containing current-state evidence, the deletion-test result, proposed seam, expected leverage/locality, dependency classification, test surface, risks, and recommendation strength. The reviewer must not jump directly to a new interface.

**Alternative rejected:** add a checklist that directly instructs agents to extract services or interfaces. The current architecture standard correctly requires abstractions to earn their existence.

### 4. Use two alternatives only for high-impact interfaces

Design-it-twice is required for cross-layer, plugin, provider, persistence, or otherwise high-impact interfaces. Small local refactors may use one clear design when the seam and behavior are already established. Alternatives are compared by depth, leverage, locality, seam placement, dependency handling, and testability.

**Alternative rejected:** require multiple designs for every refactor. This would reduce the signal-to-noise ratio and violate incremental complexity.

### 5. Treat the highest stable interface as the test surface

Tests for a selected deepening candidate assert observable behavior through the selected interface. Internal helpers may have focused tests when useful, but the review does not expose internal seams merely to make tests convenient. Existing shallow tests are replaced or consolidated when the deeper interface provides equivalent or stronger behavioral coverage.

**Alternative rejected:** preserve every old test and add deep-module tests on top. Layering redundant tests increases maintenance cost and can preserve the wrong ownership model.

### 6. Preserve existing workflow authority

If a candidate changes accepted behavior, public extension contracts, persistence semantics, or other durable requirements, the review hands off to OpenSpec. If it is an internal refactor, it follows the maintenance path and still requires focused regression and architecture revalidation. Audit records remain append-only and authoritative for audit results.

**Alternative rejected:** let the deepening review publish or implement changes directly. That would bypass the repository's specification-first workflow.

## Risks / Trade-offs

- **[Risk] Review vocabulary becomes another layer of jargon.** → Keep the glossary short, define each term in plain language, and require concrete evidence and examples for every candidate.
- **[Risk] Reviewers treat leverage or locality as line-count metrics.** → Explicitly prohibit line-count ratios and require caller complexity, change concentration, and test-surface evidence.
- **[Risk] Candidates become disguised mandatory refactors.** → Preserve explicit candidate status, selection, deferral, and rejection records; do not convert candidates into findings automatically.
- **[Risk] The Factory duplicates Matt Pocock's process.** → Adapt concepts only; keep the upstream repository reference-only and retain OpenSpec and Factory records as the authorities.
- **[Risk] Review expands beyond the selected scope.** → Require scope expansion to be recorded with the new source items and reason, using the same risk-directed limits as targeted audits.

## Migration Plan

1. Add the procedure, vocabulary, candidate template, and handoff guidance to the private Software Factory documentation.
2. Do not rewrite historical audits. Link the new procedure only from future reviews or explicitly revalidated work.
3. Pilot the procedure against the completed terms-consent architecture correction, treating it as a retrospective example rather than a new finding.
4. Review the pilot for false positives, unnecessary interface recommendations, and evidence gaps.
5. Use the procedure for future changed-scope architecture reviews after the pilot is accepted.

Rollback is documentation-only: remove the new procedure from active Factory navigation and retain the proposal and pilot notes as historical planning artifacts. No product migration or data rollback is required.

## Implementation Plan

1. Add the deepening vocabulary and scope rules to the Factory Architecture & Code Structure guidance without weakening existing earned-abstraction or anti-overengineering rules.
2. Add the review procedure covering scope, evidence collection, candidate discovery, deletion test, dependency classification, interface/test-surface analysis, selection, and handoff.
3. Add a candidate record template with stable identity, source revision, target types, current interface, proposed seam, complexity leakage, leverage/locality, dependency/adapters, tests, risks, recommendation, decision, and links to related audits or OpenSpec changes.
4. Add explicit separation rules for candidates, findings, challenges, certifications, and maintenance tasks.
5. Add the OpenSpec and maintenance handoff rules, including the no-code-before-selection gate for the review itself.
6. Run the pilot against the terms-consent evidence and record the result, including any `UNKNOWN` or unsupported conclusions rather than inferring a pass.
7. Validate the documentation structure and run strict OpenSpec validation; no Avalonia or Appium coverage is applicable because this module has no user-facing product surface.

## Acceptance-to-Verification Mapping

| Criterion | Planned verification |
|---|---|
| Scoped review is distinct from audit certification | Documentation inspection and a pilot record showing separate candidate and audit-result sections |
| Deep-module vocabulary and behavioral interface are explicit | Procedure/template inspection plus a terminology consistency check |
| Earned seams and test surfaces are required | Scenario walkthrough using deterministic and external dependency examples |
| Candidate selection precedes implementation | Procedure inspection and pilot evidence showing selected/deferred/rejected status |
| High-impact interfaces compare alternatives | Template and example inspection |
| OpenSpec and maintenance handoff is preserved | Workflow inspection and strict OpenSpec validation |
| No user-facing coverage is needed | Recorded non-applicability rationale; no UI or Appium changes |

## Open Questions

None blocking implementation. The exact private Factory filenames and navigation links can be chosen during implementation, provided they remain within the existing Factory structure and do not expose private context in tracked repository artifacts.
