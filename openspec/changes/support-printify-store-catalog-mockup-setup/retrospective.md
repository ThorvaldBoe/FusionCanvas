# Support Printify Store Catalog Mockup Setup Retrospective

## Outcome

The Store Editor uses the opened Blueprint as the sole context for offering creation and no longer exposes a duplicate normalized-catalog selector or raw offering form on Blueprint detail.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Showing normalized setup controls beside the compatibility editor would make the new catalog model accessible. | The second Blueprint selector was empty or disconnected from the Blueprint already open, and the duplicate form exposed implementation structure instead of a coherent task. | Remove the duplicate block and keep one context-aware `Add Blueprint Offering` flow owned by the opened Blueprint. | UX / implementation defect | Reusable progressive-disclosure rule | Captured in this change's product-supplier requirement and design. |
| A normalized offering selector inside offering detail could bridge the compatibility and normalized editors. | Opening a Blueprint Offering did not synchronize that selector, so dependent controls could be empty, disabled, or accidentally associated with another offering. | Use the opened offering's stable ID as the sole context, never fall back to another offering, and show one unavailable state when its normalized record is absent. | UX / implementation defect | Reusable identity/context rule | Captured in this change's product-supplier requirement and design. |

## Deferred or Change-Specific Notes

- Blueprint Offering detail still uses compatibility controls for Basics, Variants, and Placeholders alongside normalized Options/Values and Mockup Templates. Replacing that compatibility editor is a larger follow-up; this correction only establishes one safe offering context and removes dead or duplicate selectors.
