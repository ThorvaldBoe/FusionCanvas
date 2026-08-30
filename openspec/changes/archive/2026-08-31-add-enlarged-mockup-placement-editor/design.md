## Context

The Mockup Template editor already owns the selected source image and exposes a compact `MockupPlacementEditor` bound to the view model's image-space mapping values. The placement control already handles letterboxed rendering, drag movement, bottom-right resize, clamping, and arrow-key adjustments. The missing capability is progressive disclosure into a larger working surface without introducing a second draft or persistence path.

## Goals / Non-Goals

**Goals:**

- Make precise placement available from the existing compact preview through one discoverable expand/zoom control.
- Reuse the same image path, image dimensions, mapping bindings, validation, and save/cancel lifecycle.
- Preserve direct manipulation in the compact preview and keep the enlarged editor usable in normal and narrow window sizes.
- Make launch, close, focus, and keyboard activation accessible and testable without an interactive desktop.

**Non-Goals:**

- No new mapping model, persistence table, image-composition/rendering pipeline, numeric-field redesign, zoom scale model, or provider API.
- No changes to source-image applicability, revision semantics, archived-store rules, or template readiness policy.
- No separate saved draft or independent placement editor state.

## Decisions

1. **Use a transient Avalonia window owned by the Mockup Template editor.** A focused window provides substantially more canvas area while preserving the existing template dialog and its context.
2. **Bind the enlarged control directly to the existing view-model mapping properties and selected preview path.** This makes synchronization immediate and prevents divergence between compact and enlarged views. Existing Save and Cancel commands remain the only persistence/discard owners.
3. **Expose one lower-right icon-style Button in the compact preview region.** It uses a recognizable magnifying-glass-plus glyph/text fallback, tooltip, automation name, and keyboard focus. The button is outside the custom placement-control hit area.
4. **Keep the enlarged editor's minimum window size below the template editor's minimum and let the editor stretch.** The preview receives flexible space; close/cancel remains in an always-visible action row.
5. **Focus the enlarged placement editor when opened and return focus to the launch button on close when practical.** Escape and an explicit Close button close only the enlarged surface.

Alternatives considered: numeric placement fields only (less direct), replacing the compact preview (loses context), or copying values into a second modal draft (risks stale mappings).

## Risks / Trade-offs

- [Risk] Two controls bind to the same mapping properties. → Mitigation: existing two-way bindings plus headless synchronization tests.
- [Risk] A narrow owner window may not provide enough room. → Mitigation: smaller minimum, stretch layout, and narrow-layout test.
- [Risk] Custom-drawn interaction has no standard automation peer for the rectangle. → Mitigation: retain its focusable keyboard behavior and add accessible launch/close controls.

## Migration Plan

No data migration or compatibility action is required. Existing mapping records and saved revisions are unchanged. Rollback is a code-only revert.

## Open Questions

None. Product scope, placement interaction, draft ownership, and close behavior are resolved by issue 263 and existing editor conventions.

## Implementation Plan

1. Add an enlarged placement window view and code-behind in `src/FusionCanvas.App/Stores`, using the existing `MockupPlacementEditor` and direct bindings to `CatalogSetupViewModel`.
2. Add transient window lifecycle wiring in `MockupTemplateEditorWindow`/`CatalogSetupViewModel` that opens only for an available selected image and editing state, preserving the invoking button for focus return.
3. Update `MockupTemplateEditorWindow.axaml` with a lower-right icon-style launch button without changing the custom editor's bounds or pointer handling.
4. Ensure close, Escape, Archived/read-only state, empty/no-image state, and owner close behavior do not create a second save/discard path.
5. Add focused placement and headless `StoreEditor` tests covering launch, synchronization, drag/resize continuity, keyboard/accessibility state, cancellation, and narrow layout.
6. Verify criterion-by-criterion in `verification.md`, then run strict OpenSpec validation and `dotnet test .\\FusionCanvas.sln`.

## Acceptance-to-Verification Map

| Acceptance scenario | Planned verification |
| --- | --- |
| Recognizable lower-right launch control | Avalonia headless visual-tree test checks automation name, tooltip/content, and placement region |
| Compact drag/resize unaffected | Existing placement-control tests plus regression headless editor interaction test |
| Larger editor opens | Headless owner/window test checks enlarged window and larger editor bounds |
| Enlarged drag and independent resize | Existing control tests reused at enlarged size plus binding test |
| Selected image/mapping preserved | Headless test opens with seeded selection and asserts path/dimensions/mapping |
| Shared placement data and save semantics | Headless two-way binding test plus existing save tests |
| Keyboard accessibility and focus | Headless test checks automation names, focus, Escape, and button activation |
| Responsive close/cancel path | Headless narrow-layout test checks visible close action and lifecycle |
