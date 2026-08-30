# add-local-mockup-template-sources Retrospective

## Outcome

The template is explicitly a reusable collection: one named Mockup Template owns multiple source-image entries, normally one per supported color, with each entry linked to one or more offering option values.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| A single local source image could be added through the template save flow | User clarified that a template must contain the complete set of color-specific images | Add a draft source-image collection and support repeated Browse/Add actions before saving the named template | Missing requirement / UX | Change-specific | Active specs and implementation tasks |

## Deferred or Change-Specific Notes

- Per-entry editing of mappings and applicability remains to be completed before archive.
- Printify/API-backed source selection remains explicitly deferred.
