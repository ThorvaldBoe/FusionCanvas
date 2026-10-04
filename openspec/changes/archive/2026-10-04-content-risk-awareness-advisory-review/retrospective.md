# Content Risk Awareness Advisory Review Retrospective

## Outcome

FusionCanvas now provides persistent, advisory content-risk awareness for customer-facing AI-assisted text and artwork. The feature covers possible IP signals, harmful or inappropriate content, and marketplace-suitability signals without presenting automated analysis as legal clearance, safety certification, or permission to publish.

The deterministic test baseline and strict OpenSpec validation passed. The live analyzer benchmark remains intentionally deferred until a configured provider and credential setup is available.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| The request primarily concerned IP infringement awareness. | User clarified that harmful and otherwise inappropriate content is also broadly prohibited in print-on-demand. | Separate safety/inappropriate-content findings were added alongside IP and marketplace-suitability signals. | Missing requirement | Reusable for customer-facing POD review | Captured in the capability delta spec |
| Concept refinement was the first visible workflow boundary. | User clarified that Ideation precedes Concept technically. | The default awareness warning appears in Ideation before Concept while remaining present at later customer-facing boundaries. | UX | Reusable for staged creative workflows | Captured in design and UI tests |
| A negative automated result could be shown as a positive status. | The product goal requires awareness even when a thorough scan cannot be claimed. | Every relevant surface keeps a warning; `NoObviousSignalDetected` is never clearance and unavailable review remains visible. | Missing requirement | Reusable for advisory classifiers | Captured in the capability delta spec |
| The Content Risk PR could exclude the existing Terms Consent source-layout fix as unrelated. | CI exposed that the merged base still had two top-level types in one production source file. | The fix was delivered as follow-up PR #808 and its deterministic CI passed. | Implementation defect | Change-specific | No broader promotion needed |

## Learning Review

- Result: reusable lessons identified.
- Evidence reviewed: user decisions in the conversation, proposal, design, capability delta, tasks, verification record, commits `a22b264` and `7227528`, merged PRs #807 and #808, and their CI results.
- Promotions completed: the advisory/non-clearance rule, separate risk dimensions, ideation-first placement, and non-blocking failure behavior are captured in the capability delta and design.
- Deferred promotions: no main `content-risk-awareness` spec exists yet, so the delta is archived without syncing; a future spec-sync should create the accepted main capability spec. The live benchmark is deferred until provider credentials are available.
