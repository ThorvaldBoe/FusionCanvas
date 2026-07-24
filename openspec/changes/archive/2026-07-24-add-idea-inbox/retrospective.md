# Add Idea Inbox Retrospective

## Outcome

The audience field was added as an optional idea-stage field on the listing, stored as the `idea.audience` metadata key, shown and edited in the Listing Inspector's Idea section with automatic save. Idea-stage triage (promote, reject, archive) is presented through existing document-surface controls rather than a new surface. No separate Idea Inbox surface, dedicated Idea entity, or new status value was introduced.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| A separate Idea Inbox surface was needed. | Most idea-capture behavior already existed: inline tree creation, inspector editing, stage advance/regress, reject/archive, and filtering. | Present idea triage through existing controls; add only the missing audience field. | Architecture / scope | Reusable for evaluating whether a new surface is justified | Captured in `listing-inspector` spec. |
| Audience needed a dedicated entity or metadata structure. | A simple metadata key is sufficient for an optional idea-stage context field. | Store as `idea.audience` listing metadata key. | Data model | Reusable for lightweight optional fields | Captured in `listing-inspector` spec and `ItemMetadataCodec`. |

## Deferred or Change-Specific Notes

- Depends on `add-inline-details-editing` for the visible, auto-saving details pane; the two changes shipped together.
- Explicitly does not introduce duplicate `idea.phraseFragments`/`idea.visualDirection` keys; the existing `phrase` and `graphicDirection` fields carry those concepts.
