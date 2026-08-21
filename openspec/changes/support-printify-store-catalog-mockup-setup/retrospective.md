# Support Printify Store Catalog Mockup Setup Retrospective

## Outcome

The Store Editor uses the opened Blueprint and Blueprint Offering as stable contexts, repairs compatibility-only catalog records into the normalized graph, and presents one progressive normalized offering editor instead of parallel legacy and normalized forms.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Showing normalized setup controls beside the compatibility editor would make the new catalog model accessible. | The second Blueprint selector was empty or disconnected from the Blueprint already open, and the duplicate form exposed implementation structure instead of a coherent task. | Remove the duplicate block and keep one context-aware `Add Blueprint Offering` flow owned by the opened Blueprint. | UX / implementation defect | Reusable progressive-disclosure rule | Captured in this change's product-supplier requirement and design. |
| A normalized offering selector inside offering detail could bridge the compatibility and normalized editors. | Opening a Blueprint Offering did not synchronize that selector, so dependent controls could be empty, disabled, or accidentally associated with another offering. | Use the opened offering's stable ID as the sole context, never fall back to another offering, and show one unavailable state when its normalized record is absent. | UX / implementation defect | Reusable identity/context rule | Captured in this change's product-supplier requirement and design. |
| Keeping legacy Basics/Variants/Placeholders beside normalized Options/Templates would be an acceptable transitional editor. | The resulting screen duplicated concepts, exposed incompatible creation paths, obscured prerequisites, and left current-schema records created by earlier builds without normalized counterparts. | Make the normalized graph authoritative for the entire offering-detail editor, repair compatibility-only records idempotently, and retain legacy records only as a synchronized compatibility projection. | Missing architecture / UX correction | Reusable single-source-of-truth rule | Captured in this change's product-supplier requirement and design. |

## Deferred or Change-Specific Notes

- Blueprint overview and initial Blueprint/offering creation still enter through the compatibility presentation service. This module now normalizes those records immediately and uses only normalized state inside offering detail; replacing the remaining overview entry points can be evaluated separately without reopening offering-detail ownership.
