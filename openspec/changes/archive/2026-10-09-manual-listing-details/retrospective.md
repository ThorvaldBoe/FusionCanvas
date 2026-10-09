# Manual Listing Details Retrospective

## Outcome

Delivered local Listing Details for Manual and enabled Shopify Manual stores, with separate listing copy, per-Variant selling price and fulfillment cost, one shipping profile, strategy-based visibility, and a confirmed one-at-a-time Offering migration that preserves read-only history and transfers values only across exact semantic Variant matches.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| A generic “listing tool” might duplicate general Item metadata. | User specified customer-facing title and description, existing Item tags, and pricing per Variant. | Keep title and description separate; reuse Item tags; store price and fulfillment cost per Variant. | Missing requirement | Change-specific | Captured in manual-listing-details delta spec. |
| Fulfillment setup could be edited as a normal field. | User clarified only one setup is active and changes are migrations. | Require a reviewable, confirmed migration; keep source history; exact-match prices/costs; reset shipping terms. | Missing requirement / UX | Change-specific | Captured in manual-listing-details and design-area-target-selection delta specs. |

## Learning Review

- Result: no reusable lessons were found beyond the approved capability behavior.
- Evidence reviewed: user clarifications, final proposal and design, all three delta specs, completed tasks, criterion-level verification, and merged implementation PR #869.
- Promotions completed: none; the rules discovered are specific to manual listing data and Offering migration and are already captured in the capability specs.
- Deferred promotions: none.
