## Context

The selected Mockup Template target is an `OfferingPlaceholder`/Design Area with positive pixel dimensions. The existing compact and enlarged `MockupPlacementEditor` controls manipulate an image-space rectangle, while `CatalogSetupViewModel` owns the numeric mapping draft and template save lifecycle. Issue 265 requires the rectangle to honor the target Design Area ratio without preventing intentional non-proportional placement.

## Goals / Non-Goals

**Goals:**

- Derive a safe ratio from the selected Design Area and expose it to both placement surfaces.
- Enforce ratio-preserving resize and numeric width/height edits when the accessible checkbox is checked.
- Permit independent dimensions when unchecked, including for invalid or missing Design Areas.
- Recalculate behavior when the selected Design Area changes, and retain existing mapping/save/reopen behavior.
- Keep the compact and enlarged editors synchronized and usable at narrow sizes.

**Non-Goals:**

- No artwork warping, perspective correction, crop tool, rotation, or provider rendering.
- No changes to Design Area validation or image-space mapping invariants.
- No new persistence subsystem; the effective setting is restored from the selected Design Area and valid saved placement when the template is reopened.

## Decisions

1. **Use Design Area width ÷ height as the ratio.** These are the authoritative printable dimensions already persisted with the selected target; no image ratio or display-pixel ratio should influence the design-area rectangle.
2. **Keep enforcement in the placement control for pointer/keyboard gestures and in the view model for numeric fields.** This covers both direct manipulation and TextBox edits while keeping business-facing draft values in one owner.
3. **Enable the checkbox by default whenever the selected Design Area has positive dimensions.** Selecting another Design Area recomputes the ratio and resets the default to enabled for valid data; invalid/unavailable data disables enforcement safely.
4. **Resize from the lower-right handle using the dominant requested dimension.** The changed width or height drives the other dimension according to the ratio, then both values are clamped together to the image bounds. Position dragging remains unconstrained by the ratio.
5. **Reuse the existing draft/save path.** The checkbox changes editing behavior, not mapping schema; saved coordinates continue through current revision/source-image persistence and reopening restores the effective ratio from the selected Design Area.

Alternatives considered: constraining only pointer resize (would let numeric edits violate the ratio), storing a second ratio on each image (duplicates Design Area authority), or forcing ratio preservation permanently (prevents perspective/skewed imagery).

## Risks / Trade-offs

- [Risk] Rounding to whole pixels can introduce a one-pixel ratio difference. → Mitigation: calculate in doubles during interaction, clamp, then format numeric values consistently; tests allow pixel rounding tolerance.
- [Risk] A ratio larger than the available image bounds can make a requested resize appear smaller than expected. → Mitigation: clamp both dimensions proportionally and retain a positive in-bounds rectangle.
- [Risk] Changing Design Area can invalidate existing mapping dimensions. → Mitigation: recompute ratio/default behavior without silently rewriting the saved mapping; normal existing mapping validation remains authoritative.

## Migration Plan

No schema migration is required. Existing saved mappings remain valid. Rollback is a code-only revert and leaves stored coordinates untouched.

## Open Questions

None. The issue resolves default-on behavior, explicit opt-out, invalid-ratio safety, and the existing save/reopen path.

## Implementation Plan

1. Extend `MockupPlacementEditor` with `AspectRatio` and `KeepAspectRatio` styled properties, shared proportional resize/clamp logic, and ratio-aware Shift+Arrow handling.
2. Extend `CatalogSetupViewModel` with `PlacementAspectRatio` and `KeepAspectRatio`, recompute them when `SelectedPlaceholder` changes, and make `MappingWidthText`/`MappingHeightText` synchronize the paired dimension while enabled.
3. Bind the checkbox and ratio properties in `MockupTemplateEditorWindow.axaml` and `EnlargedMockupPlacementEditorWindow.axaml`, with accessible label/help text and safe disabled state.
4. Preserve shared bindings and existing save/reopen flows; ensure invalid/missing Design Areas expose independent editing without exceptions.
5. Add framework-free control tests and view-model tests for ratio derivation/enforcement, pointer resize, numeric edits, opt-out, selection changes, invalid ratios, and reopening behavior; add headless accessibility/responsive coverage.
6. Record criterion evidence, run strict OpenSpec validation, and run `dotnet test .\\FusionCanvas.sln`.

## Acceptance-to-Verification Map

| Acceptance scenario | Planned verification |
| --- | --- |
| Enabled by default for valid Design Area | View-model test and headless checkbox binding test |
| Pointer drag/resize preserves ratio | Placement-control tests with a valid ratio |
| Numeric width/height edits preserve ratio | View-model tests for both width and height text setters |
| Uncheck permits independent changes | Control and view-model tests |
| Design Area change updates ratio/behavior | View-model selection-change test |
| Invalid/unavailable ratio safe | View-model/control tests with zero/absent ratio |
| Save/reopen remains correct | Existing save/reopen tests plus draft/reload assertions |
| Accessible/responsive control | Avalonia headless visual-tree test for name, help, enabled state, and narrow layout |
