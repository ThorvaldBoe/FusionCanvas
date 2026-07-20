## Why

FusionCanvas can already capture a one-line idea as a draft listing in the editable tree, but creators have no focused place to triage raw, unprocessed ideas, attach lightweight audience/phrase-fragment/visual-direction context, or move a promising idea forward into Concept, Design, or Listing work without losing the original seed. FC-0201 adds the Idea Inbox as the Idea-stage focused surface that complements inline capture and keeps the item as the workflow center.

## What Changes

- Add a focused Idea Inbox surface that lists unprocessed idea-stage listings for the active store, niche, group, or topic context and lets the creator triage them in place.
- Add optional idea-stage fields — audience, phrase fragments, and visual direction — stored as idea-stage listing metadata alongside the existing one-line idea value.
- Add inline capture from the inbox into the selected topic using the existing listing-management resolution and atomic persistence, with focus retention and rollback on failure.
- Add explicit triage actions: promote to Concept stage, skip ahead to Design or Listing stage when the creator has enough direction, archive, and reject-without-delete.
- Reuse existing listing archive, restore, and canonical selection so idea records remain listings and never become a free-floating entity.
- Define complete empty, blocked, validation, loading, rollback, cancellation, focus, and destructive-action behavior for the inbox.

## Capabilities

### New Capabilities

- `idea-inbox`: Defines the focused Idea-stage surface that triages unprocessed idea-stage listings for an active workspace context, captures optional idea-stage audience/phrase-fragment/visual-direction metadata, and promotes, archives, or rejects ideas through existing listing-management and workflow-stage-navigator behavior.

### Modified Capabilities

None. FC-0201 reuses `listing-management` for listing identity, archive, restore, topic resolution, and persistence, `workflow-stage-navigator` for stage advancement, and `context-aware-tools` for inherited context without changing their accepted requirements.

## Impact

- Adds an `IdeaInbox` application service, view model, and Avalonia focused surface opened from store/niche/group/topic context or the active idea-stage listing.
- Stores idea-stage audience, phrase fragments, and visual direction as documented listing metadata keys; no schema migration is expected.
- Reuses the existing editable tree, canonical selection, archive-aware projection, and optimistic rollback model.
- Adds domain, application, app, and UI tests for triage resolution, optional metadata, promotion, archive, reject, blocked context, rollback, and shared desktop control guidance.
- Requires the completed FC-0104 listing-management implementation and accepted workflow-stage-navigator behavior.
