## Why

FC-0201 (Idea Inbox) asks for a low-friction place to capture rough product ideas and add lightweight context — title, notes, audience, phrase fragments, visual direction — before they become listings, with the ability to review, promote, archive, or reject them without deletion.

Most of that behavior already exists and is accepted: ideas are captured inline in the editable tree as draft idea-stage listings (FC-0104), the Listing Inspector edits title, idea, phrase, graphic direction, and notes in the details pane (FC-0106), stage advance/regress controls move an item forward (FC-0105/FC-0008), the `Rejected` lifecycle status rejects without deletion and keeps the item reachable for reference (FC-0105), archive/restore is reversible (FC-0104), and stage/status filters support reviewing unprocessed ideas (FC-0107). Two gaps remain: the PRD's **audience** field has no home, and the details pane's friction (hidden inspector, dialog editing, explicit save) made the existing behavior hard to reach — the latter is addressed by `add-inline-details-editing`.

## What Changes

- Add **audience** as an optional idea-stage field on the listing, stored as the documented `idea.audience` listing metadata key, shown and edited in the Listing Inspector's Idea section with automatic save.
- Define idea-stage triage as a presentation of existing controls in the details pane: promote through the explicit stage advance/regress controls, reject through the lifecycle status selector (`Rejected`, reactivatable through `Draft`), and archive through the inspector lifecycle action — no new surface, entity, marker, or status value.
- Document that reviewing unprocessed ideas uses the existing workflow-stage (`Idea`) and lifecycle-status (`Draft`) navigation filters.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `listing-inspector`: The inspector gains the optional audience field for idea-stage context and presents idea triage (promote, reject, archive) through the existing document-surface controls.

## Impact

- `ListingInspectorService` state and save contract gain audience; `ListingMetadataCodec` documents the `idea.audience` key. No schema migration (metadata keys only).
- The inspector view shows an Audience field in the Idea section; idea-stage emphasis already exists.
- Application, app view-model, and integration tests cover audience round-trip, omission, unknown-key preservation, and persistence reload.
- Depends on `add-inline-details-editing` for the visible, auto-saving details pane; the two changes ship together on this branch.
- Explicitly does **not** introduce a separate Idea Inbox surface, a dedicated Idea entity, an `idea.rejected` marker, or duplicate `idea.phraseFragments`/`idea.visualDirection` keys — the existing `phrase` and `graphicDirection` fields carry those concepts.
