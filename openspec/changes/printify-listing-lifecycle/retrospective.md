# Printify Listing Lifecycle Retrospective

## Outcome

The Printify tool is being implemented as a compact, single-column Listing-stage tool. It projects authoritative Item, Design, catalog, and Store data into a Printify product lifecycle while keeping remote mutations fail-closed, mock-tested, and explicitly reconcilable.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| A two-column prototype would make the product context and editable listing fields easy to compare. | The existing Fusion Canvas stage-tool host is primarily a single-column vertical composition, and a split layout makes the tool too wide and visually busy. | Use one main column with compact stacked label/control rows and state-dependent actions at the bottom. | UX / UI | Change-specific, with relevance to future stage tools | Retain in this change's design and prototype guidance; the existing UI guidelines remain the durable global rule. |
| A prominent connection banner would make readiness obvious. | The user requested a small traffic-light-like indicator that does not dominate the tool. | Use a compact status pill with text and an accompanying explanation when action is blocked. | UX / UI | Change-specific | Retain in the design and prototype. |
| A Printify tool might need to repeat variant/color editing. | Design already owns the variant policy and selected colors; duplicating that editor would create conflicting sources of truth. | Show Design-owned values read-only and link back to Design when a correction is required. | Missing requirement / architecture | Reusable for cross-stage projections | Retain in the proposal, specs, and implementation plan. |

## Deferred or Change-Specific Notes

- Printify's documented unpublish endpoint is phrased as a notification that a product has been unpublished. The application will model the operation as pending/unverified until a supervised showcase-store check confirms the final remote visibility behavior.
- The shipping-profile control remains an integration-owned default in the local projection. Its outbound serialization must follow the specific Printify endpoint capability; it must not be presented as a Shopify API operation.
- Integration-owned listing fields are stored separately from the normalized synchronization snapshot. This preserves the Design stage as the owner of colors, variants, and source artwork while allowing an accepted remote title, description, shipping profile, or price to survive reload and participate in later projections.
- The v22 mapping migration is idempotent when a workspace already has the new column but an older user-version. This protects partially migrated or interrupted workspace upgrades.
- Remote reads and destructive mutations are fail-closed: an exception while checking or deleting a product becomes an uncertain mapping that requires reconciliation, rather than allowing a blind retry.
- The Printify Listing UI follows the existing single-column shell. Remote deletion is a two-step in-tool confirmation, while conflict resolution remains explicit with accept-remote and keep-local actions.
- The official Product API accepts a shipping template through the `external.shipping_template_id` payload member. It does not expose a general out-of-stock policy field; the Listing tool therefore shows that behavior as Printify-controlled instead of presenting a local control that would silently do nothing.
