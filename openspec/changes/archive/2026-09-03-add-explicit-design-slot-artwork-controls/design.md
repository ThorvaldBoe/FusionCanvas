## Context

The Design stage already models one `DesignSlotAssignment` per row and design area. `DesignStageService.AssignSlotImageAsync` imports a PNG as an `ExportedImage`, atomically replaces an existing slot asset when needed, and returns state that the view model reloads into thumbnails. The main gap is in `MainWindow`: the slot border has drop handlers but no `DragDrop.AllowDrop`, the empty state is static text, and populated slots expose View/Download/Remove without a clearly discoverable enlarge label or browse alternative.

The primary workflow is a creator repeatedly filling final-artwork slots while working in the main Design stage. Drag/drop is the frequent primary path; Browse/Upload is the keyboard-friendly and fallback path. These actions belong inline beside the affected slot. Enlarge is an occasional focused preview action and continues to use the existing preview window. Supporting Images remain a separate section and Mockup Templates remain in Store management.

## Goals / Non-Goals

**Goals:**

- Teach the drag/drop interaction in every empty editable slot.
- Provide a per-slot browse path for empty and populated slots.
- Give immediate thumbnail feedback by reusing the existing successful assignment reload.
- Make enlarge, download, and remove actions obvious and accessible.
- Preserve one-artwork-per-slot semantics, independent assignments, replacement, persistence, read-only behavior, and PNG validation.
- Cover meaningful control state and labels with deterministic headless tests.

**Non-Goals:**

- No database/schema changes or new asset kind.
- No multiple artworks within one slot, image processing, validation of image dimensions, version history, or cloud storage.
- No changes to Mockup Template source images or Supporting Images.
- No new focused editor; the existing Design preview window remains the focused preview surface.

## Decisions

1. **Use the existing assignment service for Browse and Drop.** The view code will normalize both entry points to `AssignSlotImageAsync`. That method already replaces a slot's old asset, persists atomically, cleans up the old managed file, and reloads state through the view model. A separate upload service or data model would duplicate behavior and risk divergent persistence.

2. **Make the slot itself a real drop target and label the empty state.** Set `DragDrop.AllowDrop="True"` on each slot border and show copy such as “Drop final design artwork here” plus “PNG only · drag and drop”. The slot retains its local metadata and action row, so the added guidance does not consume a separate permanent workspace section.

3. **Use a visible text action for browse and enlarge.** The per-slot commands will use labels such as “Browse artwork…”/“Replace artwork…”, “Enlarge”, “Download”, and “Remove”, with automation names that include “final design artwork”. This is more robust for keyboard and headless verification than an icon-only glyph while still making the enlarge intent explicit.

4. **Keep browse single-file and validate at the application boundary.** The picker targets one PNG at a time because a slot has one assignment. The UI performs the same extension check as drop for immediate feedback, while the service remains authoritative for file existence and import failures. Cancel leaves state unchanged.

5. **Preserve current replacement and preview behavior.** Browse on a populated slot invokes the same assignment operation and therefore replaces only that slot. Enlarge calls the existing `PreviewSlotImageAsync` and preview window; download and removal continue through the existing service methods and confirmation flow.

## Risks / Trade-offs

- **Risk:** Picker filters are advisory on some platforms and a user can select a non-PNG. → **Mitigation:** retain explicit extension validation in the UI and authoritative service validation, showing the existing recoverable error.
- **Risk:** A malformed managed thumbnail path could prevent thumbnail construction. → **Mitigation:** retain current file-existence guard; preview/export availability continues to come from persisted asset state.
- **Risk:** Extra per-slot controls increase horizontal card height/width. → **Mitigation:** use compact buttons and short labels, retaining the existing slot grid and wrapping behavior.
- **Risk:** `async void` event handlers can receive a second click during import. → **Mitigation:** the existing view-model `IsBusy` guard is reused, and controls bind to read-only/busy state where applicable.

## Migration Plan

No migration is required. Existing `DesignSlotAssignment`, `Asset`, and `AssetLink` records are read unchanged. The change is backward compatible for empty and populated slots. If rolled back, persisted assignments remain usable by the existing preview/export/remove paths.

## Open Questions

None. Issue #276 resolves the product scope: final artwork is distinct from Supporting Images and Mockup Templates, PNG is the accepted format, and the existing replacement semantics remain compatible.

## Implementation Plan

1. Update `DesignSlotViewModel` with derived upload/enlarge labels and use `IsReadOnly`/`IsBusy` consistently for per-slot action state.
2. Update `MainWindow.axaml` slot cards to opt into drop, show final-artwork drop guidance, expose browse/replace and enlarge/download/remove actions, and add descriptive automation names/tooltips.
3. Update `MainWindow.axaml.cs` with a single-slot PNG picker handler that resolves the row and calls the existing assignment path; keep drop validation and error handling local and recoverable.
4. Extend `DesignStageToolHeadlessTests` for empty-state guidance, browse/enlarge/download/remove labels, and distinct Supporting Images labelling. Retain application service tests for replacement, invalid files, multiple slots, and reload persistence; add a focused multiple-slot persistence assertion only if existing coverage does not already prove it.
5. Run focused App/Application tests, `openspec validate`, and the required `dotnet test .\FusionCanvas.sln`; record each acceptance scenario in `verification.md`.

## Acceptance Verification Map

| Acceptance area | Planned evidence |
| --- | --- |
| Drop target guidance and browse alternative | Headless view test inspects slot text, AllowDrop, and Browse/Replace buttons |
| Immediate assignment, invalid files, replacement, persistence, multiple artworks | Existing `DesignStageServiceTests` plus focused additions and solution test |
| Enlarge/download/remove | Headless view labels plus existing preview/export/remove service and view-model tests |
| Read-only and category separation | Headless read-only assertions and distinct Design Slot Grid/Supporting Images headings |
