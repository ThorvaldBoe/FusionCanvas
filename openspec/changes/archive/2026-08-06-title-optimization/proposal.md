## Origin

- GitHub Issue: [ThorvaldBoe/FusionCanvas#115 - Title generation](https://github.com/ThorvaldBoe/FusionCanvas/issues/115)

## Why

Auto-generated item titles are set to the first sentence of the full generated idea (see `IdeationService.CreateAsync`), so they quickly become long and hard to read in the navigation pane. Creators need a fast, local-first way to collapse the essence of an item's creative content into a short, unique working title.

## What Changes

- Add an **Optimize** action next to the **Working title** field in the listing inspector Overview surface.
- On activation, use AI to generate a short title from the item's existing creative content (Idea, and — when present — Concept idea, Phrase, and Graphic direction).
- Ensure the resulting title is **unique across the whole store** using an AI refinement loop:
  1. Generate a short name with AI.
  2. Check the store for an existing title collision (excluding the active item, archived items, and rejected items).
  3. If collided, ask AI to add one relevant distinguishing word and check again.
  4. Repeat until unique (bounded), falling back to a numeric suffix when the bound is reached — the canonical case being two items with genuinely identical data.
- On success, **immediately overwrite and persist** the working title through the existing automatic-save path.
- **Disable the action with a tooltip** when the required AI purpose is unavailable or the item has no creative content to draw from.
- Allow only **one in-flight operation per item**, cancel it on item switch/close, and never apply a late result. Failures surface inline without overwriting the title.

## Capabilities

### New Capabilities

- `title-optimization`: AI-assisted generation of a short, store-unique working title from an item's creative content, with an Optimize command in the listing inspector Overview, a bounded uniqueness loop with numeric fallback, availability gating, immediate persistence, and single-operation concurrency with cancellation.

### Modified Capabilities

- none (the listing inspector Overview gains the action, but the Overview UI rules for this command live in the new `title-optimization` capability. The new command reuses the `listing-inspector` autosave / expected-state and "commits pending edits when the context changes" rules rather than redefining them; no `listing-inspector` requirement is changed here.)

## Impact

- **FusionCanvas.App**: `MainWindow.axaml` Overview surface (Optimize command alongside the Working title field), `ItemInspectorViewModel` (command, busy/error/cancellation state, availability + tooltip, focus/save interaction).
- **FusionCanvas.Application**: a new `TitleOptimization` service/contract behind the AI text-generation boundary; reuse `IAiTextGenerationService` with a new `AiRequestPurpose.Title` value that reuses the existing General purpose profile (extend `AiConfigurationResolver` to map `Title` → `settings.General`); a store-wide collision check against other active items' titles (excluding the active item, archived items, and rejected items).
- **FusionCanvas.Domain**: deterministic, framework-free pieces of the optimization policy — the creative-content-availability predicate (`HasCreativeContent`), collision exclusion semantics, and the bounded termination rule with numeric fallback — kept testable at the lowest layer and reused by both the availability gate and the orchestrator.
- **Tests**: domain tests for the uniqueness/termination policy, application use-case tests with a deterministic AI collaborator, and (framework-risk-motivated) Avalonia headless view tests for Optimize gating, busy state, and keyboard reachability.
- **Dependencies**: existing AI settings (credential + model). No new NuGet packages.
- **Non-goals**: marketplace-ready SEO titles, multi-candidate pickers, optimize-for-listing copy beyond the working title, and any change to how Ideation initially seeds titles (that seed behavior is unchanged by this module).
