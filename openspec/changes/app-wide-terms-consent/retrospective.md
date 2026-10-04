# App-Wide Terms Consent Retrospective

## Outcome

The app-wide consent gate is merged and technically verified. FusionCanvas now gates normal workspace access on a minimal, versioned application-settings acknowledgement record, presents separate FusionCanvas/provider/IP responsibility acknowledgements, keeps FusionCanvas policy content available offline, and exposes review state from Settings. The user-approved policy is bundled as version `0.1`.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| The consent ViewModel could persist settings directly for a small feature. | The Software Factory architecture review identified a UI-to-persistence boundary violation. | Move acceptance persistence and record creation into `TermsConsentService` in the Application layer. | Architecture | Reusable | Implemented in the Application layer; retain as an architecture review lesson. |
| Raw visual values were acceptable for a focused dialog. | The Software Factory UI review identified missing semantic token usage. | Use shared semantic design tokens for consent layout, typography, colors, spacing, and controls. | UI | Reusable | Implemented in the design system; retain as a UI review lesson. |
| The feature could be finalized in the shared checkout while another feature was active. | Shared branch/worktree changes became interleaved and obscured Git state. | Use an isolated worktree for feature delivery and keep the shared checkout on its active branch. | Implementation defect | Operational | Deferred to local workflow practice; no product-spec promotion. |
| Draft legal wording could be treated as complete after implementation. | The user approved the proposed product wording, while separate legal-counsel review is not represented as completed. | Use the approved version `0.1` without a draft marker and retain the non-legal-advice and user-responsibility boundaries. | Decision resolved | Change-specific | Promoted into the bundled policy, version policy, consent UI, verification, and task record. |

## Learning Review

- Result: reusable lessons identified and promoted in implementation/design review; product-owner approval is recorded and separate legal-counsel review remains external.
- Evidence reviewed: proposal, design, delta specs, tasks, verification evidence, Software Factory audit record, commit history, and full solution test results.
- Promotions completed: Application-layer persistence ownership and semantic design-token usage.
- Deferred external review: legal-counsel review, because it requires a qualified reviewer rather than implementation inference.
