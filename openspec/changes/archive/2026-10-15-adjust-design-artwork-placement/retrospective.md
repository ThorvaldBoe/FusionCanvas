# Adjust Design Artwork Placement Retrospective

## Outcome

The Design Stage preview now supports proportional movement and resizing based on detected non-transparent PNG bounds. Applying an adjustment clips artwork to the original canvas, replaces the slot image, and preserves the immediate before-image as an Item-linked Related Image. Each later successful Apply preserves the then-current image as another before-image.

## Feedback-Driven Adjustments

| Initial assumption | Evidence | Correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| The existing enlarged preview only needed a larger, resizable image view and transparent-canvas treatment. | User requested an adjustment rectangle around visible artwork, distinguished from the PNG canvas boundary, with proportional resizing and free positioning. | Extend the focused preview with reversible adjustment mode based on non-zero alpha bounds. | Missing requirement | Design artwork adjustment | Captured in the `design-artwork-adjustment` delta specification. |
| Adjustments could be limited to a reversible preview or a single saved result. | User clarified that Apply must destructively replace the image, clip pixels outside the canvas, and save a before-image in Related Images; later adjustments must save each immediate before-image. | Apply atomically replaces the slot image and adds an independent before-image for every successful Apply. | Missing requirement | Design artwork adjustment and related-image persistence | Captured in the `design-artwork-adjustment` and `asset-management` delta specifications. |

## Learning Review

- Result: reusable lessons identified
- Evidence reviewed: approved user feedback in the conversation; proposal, design, both delta specs, tasks, criterion-level verification evidence, and implementation history for commit `d52b1f4`.
- Promotions completed: the feature-specific behavior and repeat-Apply recovery requirement are recorded in the delta specs and synced into the accepted `asset-management` and `design-artwork-adjustment` specifications.
- Deferred promotions: none. No broader UX, visual, or architecture rule was supported by this feature alone.
