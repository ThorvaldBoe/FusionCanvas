# app-wide-terms-consent Retrospective

## Outcome

The consent flow now keeps startup shutdown coordinated with the asynchronous acknowledgement save. Quit and close are unavailable while saving, the startup cancellation token is passed through the settings persistence boundary, and startup awaits any pending save before terminating.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| A consent decision could complete startup independently of the save task. | Audit finding #802 showed that Agree followed by Quit/close could let shutdown proceed while settings persistence was still unresolved. | Disable quit/close during save, pass startup cancellation into the save, and await the pending save before shutdown. | Implementation defect | Change-specific | None |

## Deferred or Change-Specific Notes

- The correction preserves the existing save-error and retry behavior; it does not broaden the consent surface or provider-policy workflow.
