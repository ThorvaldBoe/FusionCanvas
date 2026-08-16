# Support Printify Store Catalog Mockup Setup Retrospective

## Outcome

The Store Editor uses the opened Blueprint as the sole context for offering creation and no longer exposes a duplicate normalized-catalog selector or raw offering form on Blueprint detail.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Showing normalized setup controls beside the compatibility editor would make the new catalog model accessible. | The second Blueprint selector was empty or disconnected from the Blueprint already open, and the duplicate form exposed implementation structure instead of a coherent task. | Remove the duplicate block and keep one context-aware `Add Blueprint Offering` flow owned by the opened Blueprint. | UX / implementation defect | Reusable progressive-disclosure rule | Captured in this change's product-supplier requirement and design. |

## Deferred or Change-Specific Notes

- The Blueprint Offering detail still mixes compatibility and normalized controls. Its information architecture is analyzed separately before another behavior change.
