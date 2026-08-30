# add-local-mockup-template-sources Retrospective

## Outcome

The template is explicitly a reusable collection: one named Mockup Template owns multiple independently configurable source-image entries, normally one per supported Color and all Sizes, and one shared Design Area. Uploading builds the image collection; selecting a row configures that image's grouped applicability and placement later.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| A single local source image could be added through the template save flow | User clarified that a template must contain the complete set of color-specific images | Add a draft source-image collection and support repeated Browse/Add actions before saving the named template | Missing requirement / UX | Change-specific | Active specs and implementation tasks |
| Browse captured the current applicability and the form mixed file acquisition with metadata | Manual testing showed that creators need to upload first and work through configuration independently | Use a master-detail editor with an upper image table and lower selected-image metadata/placement editor; upload assigns no metadata | UX and state-model correction | Reusable master-detail lesson | UX guidance, delta specs, design, tasks, UI-language artifact |
| Source entries required non-empty conditions and a mapping | The independent workflow needs incomplete uploaded rows to survive Save and reload | Make incomplete source entries persistable, visibly incomplete, excluded from matching, and progressively completable | Missing data/invariant requirement | Change-specific | Domain, persistence, application, and verification tasks |
| Flat condition values were treated as one conjunction | Multiple alternatives in the same Option would never match a concrete Variant | Match OR within an Option and AND across configured Options; optimize the UI for one Color with no Size restriction | Product-rule correction | Potentially reusable condition semantics | Delta spec, domain policy, tests |
| Template readiness behaved as one aggregate gate | A future generation attempt should fail only for the missing or ambiguous Variant, not discard usable resolutions | Retain a stable outcome per Variant while keeping whole-Template readiness as a summary | Future-compatibility correction | Reusable resolution-result lesson | Delta spec, contracts, policy tests |

## Deferred or Change-Specific Notes

- The validated UI-language mockup is the approved semantic design reference for the remaining master-detail implementation.
- Actual mockup rendering/generation remains deferred; this change supplies recoverable per-Variant resolution outcomes for that later consumer.
- Printify/API-backed source selection remains explicitly deferred.
