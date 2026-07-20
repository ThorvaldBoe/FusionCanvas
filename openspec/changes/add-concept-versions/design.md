## Context

The accepted core-domain-model defines Workspace, Store, Niche, Group, Listing, Asset, Prompt, and Tag and explicitly defers Concept, Design, and Mockup entities. FC-0104 made the listing the primary item entity with topic-resolved creation, archive-aware projection, atomic snapshot persistence, and a permanent-deletion guard that blocks listings referenced by prompts, asset links, or other dependent creative records. FC-0008 established workflow stage navigation, and FC-0010 established context-aware inherited context. The Phase 2 PRD requires Concept-stage work to be item-bound, to preserve alternate directions, and to expose exactly one selected concept whose idea, phrase, and graphic values drive the Basic Concept Tool.

What is missing is the Concept entity itself, the multi-version model, and the selected-direction invariant that downstream Concept, Design, and Listing tools depend on.

## Goals / Non-Goals

**Goals:**

- Add an item-bound Concept entity with stable identity, timestamps, idea/phrase/graphic direction, audience reaction, risks, quality notes, and design-triangle score metadata.
- Allow multiple concept versions per listing and preserve superseded and rejected versions without deletion.
- Mark exactly one selected concept version per listing as the current direction.
- Allow creation from an existing idea or directly from a phrase, graphic direction, or other later-stage starting point.
- Inherit store, niche, topic path, item, tags, and metadata from the current context through the accepted context-aware boundary.
- Persist concept operations through the existing atomic snapshot model with a forward-only schema migration.

**Non-Goals:**

- Do not implement AI concept refinement, version comparison UI, automatic scoring, or design file versioning.
- Do not implement the Basic Concept Tool UI; that is FC-0210.
- Do not implement Design or Mockup entities; those belong to later changes.
- Do not implement a free-floating concept outside an item.
- Do not implement prompt or asset entities; those already exist and remain unchanged.

## Decisions

### 1. Add a Concept domain entity owned by a listing

A Concept is a first-class domain entity that belongs to exactly one listing. It carries stable identity, created/modified timestamps, the Concept-stage context it was created in, and the documented creative fields. Concepts are not nested; a listing owns a flat set of concept versions. The selected concept is recorded as a listing-level reference to a concept id, not as a flag on the concept, so the invariant "at most one selected" is enforceable at the listing level.

Alternative considered: store the selected direction as a boolean on each concept. Rejected because it requires scanning all concepts to enforce single-selection and risks inconsistent state during partial saves.

### 2. Treat concepts as dependent creative records for deletion guarding

A listing with one or more concept versions is blocked from permanent deletion by the existing listing-management guard language ("prompt, asset link, or other dependent creative record references it"). The concept-versions capability adds a concept-deletion path that archives or rejects a concept version without deleting it; permanent concept deletion, when added by a later change, will reuse the same atomic-snapshot and confirmation pattern.

Alternative considered: cascade-delete concepts when a listing is deleted. Rejected because the Phase 2 PRD requires preserving creative history and rejected alternatives.

### 3. Reuse context-aware inherited context at creation

Concept creation requests resolved context for the selected topic and item from the accepted context-aware boundary. Applicable inherited tags and metadata are copied into the new concept with provenance that distinguishes inherited from explicit values. Explicit values win before save. Movement of a listing does not recalculate inherited context on existing concepts.

Alternative considered: derive inherited values dynamically on every read. Rejected because parent changes and moves would silently alter established concept context.

### 4. Selected concept is an invariant; supersede and reject are explicit

A listing always has at most one selected concept. Creating a new concept from a blank listing selects it automatically. Accepting a large alternate direction from the Basic Concept Tool creates a new concept version and either selects it or leaves the existing selection, according to the user's chosen action. Superseding selects the new version and demotes the prior selection to superseded. Rejecting a selected concept clears the selection and records a rejected marker; the user must select another concept before downstream tools can use it. A listing may have zero concepts and zero selection.

Alternative considered: allow zero or many selected concepts. Rejected because the Basic Concept Tool needs a single current direction and the Phase 2 PRD requires one selected concept.

### 5. Score metadata is advisory and optional

Design-triangle score metadata is stored as a documented metadata block on the concept (`concept.score` with overall, weak element, confidence, and critique hint). Scores are advisory: low scores do not block stage advancement and high scores do not guarantee success. Scores are absent when AI scoring is unavailable.

Alternative considered: enforce score thresholds. Rejected because the PRD treats scoring as creative assistance, not a gatekeeper.

### 6. Forward-only schema migration through the snapshot

A new concept table and a selected-concept reference on the listing are added through the existing snapshot and migration path. Existing listings load with zero concepts and no selection. No down-migration is provided; the snapshot version increments once.

Alternative considered: store concepts inside listing JSON. Rejected because multiple concepts per listing, selected-direction queries, and downstream tool joins benefit from a typed table.

## Risks / Trade-offs

- [Selected-direction invariant can break under partial saves] -> Enforce single-selection at the listing level inside the atomic snapshot and add invariant tests.
- [Concept accumulation may clutter downstream tools] -> The Basic Concept Tool shows the selected concept by default and exposes superseded/rejected concepts only through an explicit versions list.
- [Inherited metadata can become an untyped catch-all] -> Limit FC-0202 concept metadata keys to documented fields and preserve unknown keys during edits.
- [Future Design entity will reference the selected concept] -> Keep the selected-concept reference at the listing level so a later Design entity can depend on it without coupling to concept internals.

## Migration Plan

Increment the workspace snapshot version, add the concept table and listing-level selected-concept reference, and migrate existing snapshots by assigning zero concepts and no selection. No data backfill is required. Down-migration is unsupported.

## Open Questions

- Should a rejected concept be a distinct lifecycle state or an archive-style marker like the idea-inbox `idea.rejected` pattern? (Default: distinct `Rejected` state on the concept, because concept versions need review visibility that archive projection does not provide.)
- Should concept versions be ordered by creation time only, or should manual ordering be supported for later review? (Default: creation-time order with the selected concept surfaced first.)
