## Context

FC-0104 established the listing as the primary item entity and the unit of idea capture: a one-line idea value becomes a draft listing beneath a resolved niche or group, with archive, restore, movement, duplication, and atomic persistence already accepted. FC-0008 established the workflow stage navigator for advancing items between Idea, Concept, Design, and Listing stages. FC-0010 established context-aware tooling and inherited context. The niche-rooted editable tree remains the canonical creation and navigation surface, and inline listing capture already exists.

What is missing is a focused Idea-stage surface for triage. Inline tree capture is fast, but a creator accumulating many raw seeds needs to review unprocessed ideas together, add lightweight audience/phrase/visual context, and decide which idea to advance, archive, or reject without navigating one topic at a time. The Phase 2 PRD treats ideas as item-bound: idea-stage work begins from a selected topic and creates new items, and ideas can move into Concept, Design, or Listing stages when the creator already has enough information. The PRD does not call for a free-floating idea entity.

## Goals / Non-Goals

**Goals:**

- Provide a focused Idea Inbox surface that lists unprocessed idea-stage listings for an active store, niche, group, or topic context.
- Allow optional idea-stage audience, phrase fragments, and visual direction to be captured as listing metadata without forcing a schema change or a new entity.
- Reuse FC-0104 listing-management topic resolution, atomic persistence, archive/restore, and canonical selection.
- Reuse FC-0008 workflow-stage-navigator advancement so promoting an idea changes its current workflow stage through the accepted behavior.
- Define complete empty, blocked, validation, loading, rollback, cancellation, focus, and destructive-action behavior.

**Non-Goals:**

- Do not introduce a separate Idea entity; the listing remains the item that carries the idea.
- Do not implement AI idea generation, idea scoring, bulk idea import, or marketplace validation.
- Do not replace inline tree capture; the inbox complements it.
- Do not implement Concept, Design, or Listing tooling; promotion only requests stage advancement.
- Do not persist a new sibling-order field; the inbox ordering follows existing listing ordering rules.

## Decisions

### 1. Treat ideas as idea-stage listings, not a new entity

The inbox lists listings whose current workflow stage is Idea and that are not archived or rejected. Optional idea-stage fields are stored as documented listing metadata keys (`idea.audience`, `idea.phraseFragments`, `idea.visualDirection`) preserved by the existing listing-management metadata rules. No new domain entity, table, or foreign key is added.

Alternative considered: introduce a dedicated `Idea` entity that converts to a listing. Rejected because FC-0104 already made the listing the item that carries the idea, and a second entity would duplicate identity, archive, movement, and persistence rules already accepted for listings.

### 2. Add an `IdeaInbox` application service over the existing snapshot

The service exposes loading of idea-stage listings scoped to an active context, optional metadata updates, promote-to-stage requests, archive, and reject. Every mutation reloads the latest snapshot, validates, replaces the snapshot, and saves once through `IWorkspaceRepository.SaveAsync`. Tree view models and the inbox view model own only drafts and optimistic presentation.

Alternative considered: extend `WorkspaceTreeViewModel` to triage idea-stage listings. Rejected because triage is a focused surface concern and would couple business rules to the tree.

### 3. Promote through the accepted workflow-stage-navigator

Promote to Concept, Design, or Listing requests the existing workflow-stage-navigator behavior to advance the listing's current stage. Skip-ahead is allowed when the creator has enough direction, but the inbox does not bypass stage validation; the navigator remains the owner of valid stage transitions.

Alternative considered: let the inbox set the stage field directly. Rejected because it would duplicate transition rules and risk invalid stage jumps.

### 4. Reject is archive-with-a-reason, not deletion

Rejecting an idea sets the listing archive flag and records an `idea.rejected` metadata marker with an optional reason, leaving the record reachable through the existing archived-listing lifecycle surface. Rejected ideas remain available for reference without deletion. Restoring a rejected idea clears the marker through the normal restore flow.

Alternative considered: add a separate `Rejected` flag. Rejected because the archive-aware projection already removes rejected ideas from active navigation and the listing-management deletion guard already protects connected records.

### 5. Reuse canonical selection and the focused-surface pattern

Opening the inbox from a store/niche/group/topic context scopes the list to that context and preselects the active idea-stage listing if one is selected. Selecting an inbox row updates canonical tree selection through the existing normal-selection path so the reusable working tab stays coherent. The inbox is a focused surface, not a permanent workspace region.

Alternative considered: dock the inbox beside the tree permanently. Rejected because FC-0201 is a triage surface used occasionally while capture and stage work remain the primary workflow.

### 6. Reuse optimistic interaction and error-state patterns

The inbox may present drafts or optimistic metadata updates, but it retains a confirmed projection and restores selection, filter, and draft state after validation or persistence failure. Busy operations prevent duplicate submission. Inline errors preserve text and focus. Escape cancels an uncommitted draft; destructive confirmations are keyboard accessible; cancellation leaves persisted and visible state unchanged.

Alternative considered: reload the workspace after every failure. Rejected because it loses user orientation and weakens recovery relative to the established rollback model.

## Risks / Trade-offs

- [Idea-stage metadata can become an untyped catch-all] -> Limit FC-0201 keys to documented idea-stage fields and preserve unknown keys during edits.
- [Skip-ahead may confuse stage ownership] -> Route every promotion through the workflow-stage-navigator and let the navigator reject invalid transitions.
- [Reject semantics may collide with future lifecycle status work] -> Keep reject as archive-with-marker and let future lifecycle-status changes refine it through their own deltas.
- [Inbox context scope may conflict with archive-aware projection] -> Reuse the shared projection so archived or rejected ideas never appear in the active inbox list.

## Migration Plan

No database migration is expected. Idea-stage fields are stored as listing metadata keys. Existing idea-stage listings remain visible in the inbox after the feature ships.

## Open Questions

- Should the inbox surface audience/phrase-fragment/visual-direction as separate required-visible fields or as a single collapsible idea-stage metadata section? (Default: separate compact fields with progressive disclosure.)
- Should rejected ideas remain visible in a dedicated "Rejected" inbox section or only through the archived-listing lifecycle surface? (Default: archived-listing lifecycle surface only, to keep the active inbox focused on unprocessed ideas.)
