## Context

The accepted core-domain-model defines a Prompt as preserved prompt-related context that can be associated with store, topic, listing, or asset context and explicitly does not require an AI provider, execution result, token usage, or prompt library entry. FC-0104 made the listing the primary item entity with a permanent-deletion guard that already blocks listings referenced by prompts. The in-flight concept-versions and design-records changes add Concept and Design as item-bound creative records that need prompt associations for AI-assisted refinement and generation. The Phase 2 PRD requires prompt history to preserve inputs, outputs, type, provider/model, and useful rejected or superseded outputs, and to support later reuse and review.

What is missing is the prompt-record behavior itself: saving inputs and outputs, typing, provider/model metadata, concept/design associations, lifecycle preservation, per-context visibility, and atomic persistence.

## Goals / Non-Goals

**Goals:**

- Allow users to save prompt inputs and outputs as Prompt records.
- Associate prompts with stores, niches, listings, concepts, designs, or assets.
- Record prompt type and provider/model information when available.
- Preserve useful rejected or superseded outputs for later review and reuse.
- Surface prompt history per associated context and at store level.
- Persist prompt operations through the existing atomic snapshot model with a forward-only schema migration.

**Non-Goals:**

- Do not implement live AI execution, prompt template libraries, provider configuration, or automatic critique/scoring.
- Do not store API keys, tokens, or secrets.
- Do not implement bulk prompt import or marketplace prompt libraries.
- Do not implement the Basic Concept or Design Tool UIs; those are FC-0210 and FC-0211.

## Decisions

### 1. Prompts are records with input, output, type, provider/model, and associations

A Prompt record carries the prompt input text, the preserved output text or output reference, a prompt type from a small enum (idea, phrase, graphic, image, listing-text, critique, other), optional provider and model strings, optional generation settings metadata, and one or more context associations. Outputs may be referenced rather than embedded when the output is a managed asset (e.g., a generated image), to avoid duplicating large content.

Alternative considered: store only prompt text and a free-form notes field. Rejected because the PRD wants type, provider/model, and output preservation for later reuse.

### 2. Associations reuse the context-link model

Prompt associations use the same context-link approach as assets: a prompt can link to a store, niche, listing, concept, design, or asset, validated against the active workspace and the prompt's store. A single prompt may link to more than one context when one prompt output informs multiple creative records. Removing a context removes its prompt context link without deleting the prompt; the prompt remains reachable at store level.

Alternative considered: embed associations as a JSON array on the prompt. Rejected because context-link queries and deletion-guard integration benefit from typed links.

### 3. Rejected and superseded outputs are preserved, not deleted

A prompt record has a lifecycle marker: active, superseded, or rejected. Rejecting or superseding a prompt output preserves the record and its associations for later review and negative guidance. The active prompt history view excludes rejected and superseded records by default but exposes them through an explicit filter.

Alternative considered: hard-delete rejected prompts. Rejected because the PRD explicitly wants useful rejected outputs preserved.

### 4. No secrets stored

Provider and model are stored as plain strings (e.g., "OpenAI", "gpt-4o"). API keys, tokens, and credentials are never stored on prompt records; provider configuration belongs to future AI-settings work. The prompt-history capability treats all prompt content as untrusted user-controlled input and guards against prompt-injection-style content in any future AI-facing read path.

Alternative considered: store provider credentials for one-click replay. Rejected because the repo is public and the PRD defers provider configuration.

### 5. Per-context and store-level prompt surfaces

The focused prompt surface lists prompts linked to a selected store, niche, listing, concept, design, or asset with their types, providers, models, and lifecycle markers. The store-level view lists every store-owned prompt. Selecting a prompt opens a read-only detail with input, output, associations, and metadata, plus reuse and copy actions.

Alternative considered: show prompts only inside the Concept and Design tools. Rejected because the PRD wants cross-stage prompt review without digging through tools.

### 6. Forward-only schema migration

A new prompt table with input, output, type, provider/model, lifecycle marker, and timestamps is added through the existing snapshot and migration path. Existing snapshots migrate with zero prompts. Prompt context links reuse the existing context-link infrastructure. No down-migration is provided.

Alternative considered: store prompts inside listing JSON. Rejected because cross-context associations and deletion-guard queries benefit from a typed table.

## Risks / Trade-offs

- [Prompt content can include injected instructions] -> Treat prompt text as untrusted input in any future AI-facing read path and document the guard.
- [Lifecycle markers can clutter active views] -> Default to active-only with an explicit superseded/rejected filter.
- [Provider/model strings can drift] -> Keep them free-form for now; a later AI-settings change may normalize them.

## Migration Plan

Increment the workspace snapshot version, add the prompt table and prompt context links, and migrate existing snapshots by assigning zero prompts. No data backfill is required. Down-migration is unsupported.

## Open Questions

- Should prompt outputs be embedded as text or always referenced as assets? (Default: embed text outputs, reference asset outputs via the asset context link, to avoid duplicating large images.)
- Should prompt reuse create a new prompt record or version the existing one? (Default: create a new prompt record with a `reusedFrom` reference, to preserve history.)
