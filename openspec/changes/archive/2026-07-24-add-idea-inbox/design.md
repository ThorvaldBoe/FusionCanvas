## Context

FC-0104 made the listing the item that carries an idea: inline tree capture creates a draft listing at the `Idea` workflow stage beneath a resolved topic. FC-0106 gave the selected listing a details pane (the Listing Inspector) that edits title, idea, phrase, graphic direction, notes, and tags. FC-0105 added the independent lifecycle status (`Draft`, `Published`, `Paused`, `Rejected`) with a document-surface selector, explicit adjacent stage advance/regress controls, and rejected-work review. FC-0107 added stage and status filters to the navigation tree.

An earlier interpretation of FC-0201 proposed a separate triage surface with a dedicated inbox service, new `idea.phraseFragments`/`idea.visualDirection` metadata keys duplicating the inspector's existing `phrase`/`graphicDirection` fields, and an `idea.rejected` marker duplicating the `Rejected` lifecycle status. That interpretation was abandoned: it duplicated accepted concepts and specified reuse of stage-transition behavior the workflow stage navigator does not own (the navigator owns view navigation; `ListingManagementService.MoveListingStageAsync` owns transitions).

The PRD's remaining unmet need is the **audience** field. Everything else FC-0201 asks for maps onto accepted behavior.

## Goals / Non-Goals

**Goals:**

- Record an optional audience for an idea-stage listing in the details pane.
- Keep every FC-0201 acceptance criterion reachable through existing, accepted behavior, and document that mapping.

**Non-Goals:**

- No separate Idea Inbox surface, inbox service, or Idea entity.
- No `idea.rejected` marker; the `Rejected` lifecycle status already rejects without deletion.
- No duplicate phrase-fragment or visual-direction keys; `phrase` and `graphicDirection` already carry them.
- No direct stage jumps (skip-ahead); repeated adjacent advance reaches any later stage, consistent with the accepted stage-movement controls.
- No AI idea generation, scoring, bulk import, or marketplace validation.

## Decisions

### 1. Audience is a documented listing metadata key edited in the inspector

`idea.audience` joins the documented idea-stage metadata keys. The inspector loads and saves it alongside the other creative fields, normalized like them, omitted when empty, and preserved alongside unknown metadata keys. It is editable at every workflow stage and visually grouped with the Idea section, which the inspector already emphasizes at the `Idea` stage.

Alternative considered: a first-class domain column. Rejected because optional creative context already lives in listing metadata and no query or invariant requires a column.

### 2. Idea triage is a presentation of existing controls, not new behavior

- **Promote** — the explicit advance control moves the idea to `Concept`; repeating it reaches `Design` and `Listing`. The PRD's "skip ahead when the creator already has enough information" is satisfied by rapid adjacent advancement, which is the accepted stage-movement behavior; no jump controls are added.
- **Reject without deletion** — setting lifecycle status to `Rejected` keeps the idea visible in the tree with an inactive treatment, reviewable, filterable, and reactivatable by returning to `Draft`.
- **Archive** — the inspector's archive action removes the idea from active navigation while preserving it for restore.

Alternative considered: dedicated Promote/Reject buttons duplicating these controls inside the inspector. Rejected because the listing-inspector spec forbids competing lifecycle controls in the same document context, and the existing controls already live directly above the inspector.

### 3. Reviewing unprocessed ideas uses the existing filters

The navigation tree's workflow-stage filter (`Idea`) and lifecycle-status filter (`Draft`) together list exactly the unprocessed ideas, with parent topics preserved for context. No inbox list is needed for the FC-0201 review acceptance criterion.

## Risks / Trade-offs

- [PRD readers expect a literal "inbox" surface] -> The change documents the mapping from each FC-0201 requirement to its accepted behavior so the interpretation is reviewable and revisitable.
- [Adjacent-only promotion feels slow for true skip-ahead] -> Accepted stage controls are adjacent by design; if real usage shows the extra clicks matter, a future change can propose jump controls against the listing-lifecycle-status spec deliberately.

## Migration Plan

No migration. `idea.audience` is a metadata key; existing listings simply have no value for it.

## Open Questions

None.
