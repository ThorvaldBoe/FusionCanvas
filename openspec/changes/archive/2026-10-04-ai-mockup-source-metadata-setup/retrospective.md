# AI Mockup Source Metadata Setup Retrospective

## Outcome

Issue #726 was implemented and merged in PR #775. The Mockup Template editor now supports optional, batch AI assistance for selected source-image metadata. The implementation is metadata-first, conditionally sends image content only when visual inspection is needed, follows the configured General AI and Zero Data Retention settings, applies results to editable drafts, and preserves the existing Save workflow as the persistence boundary.

## Feedback-Driven Adjustments

| Initial assumption | Evidence | Correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Image metadata could be sent for every selected source image. | User feedback emphasized that filenames may already contain color information and that image transmission should be conditional. | Resolve recognizable filename/context signals first; send image content only when unresolved metadata requires visual inspection. | Missing requirement | Any future local-image AI assistance | Captured in the delta spec and design. |
| The feature might need a dedicated privacy or model configuration. | User requested following the existing AI Settings ZDR setting and reusing the General configuration. | Reuse the General AI path and propagate the configured ZDR preference. | Reusable architecture lesson | Future AI-assisted workflows | Captured in the delta spec and design. |
| Assisted values should always preserve existing values. | User allowed replacement when logically justified, provided the result is supported and reviewable. | Permit confidence-aware replacement of existing draft values while preserving uncertain or invalid values. | Acceptance clarification | Future draft-first assistance | Captured in the delta spec and design. |
| Placement could be inferred independently for each image. | Existing project mappings provide a safer, more consistent source of truth. | Reuse compatible established mappings and mark conflicts or missing references for review. | Reusable domain lesson | Future metadata setup assistance | Captured in the delta spec and design. |

## Learning Review

- Result: reusable lessons identified and captured in the change proposal, design, and delta specification.
- Evidence reviewed: final proposal, design, delta specification, completed task list, verification evidence, user feedback, and implementation history in commit `c921fe6` / merged PR #775.
- Promotions completed: none outside the change artifacts; no broader architecture, UX, or UI guidance needed updating.
- Deferred promotions: none. The relevant behavior is capability-specific and belongs in the archived delta specification rather than a broader project guideline.
