## 1. Grouped value rows

- [x] 1.1 Replace the positional choice-group record with a focused view model that exposes an in-memory provider of active value rows in saved order and descriptive grid/row automation text.
- [x] 1.2 Verify group providers refresh from the selected Offering's active Size/custom values without changing Color setup or persisted ordering.

## 2. Bounded card grids

- [x] 2.1 Replace only Size/custom `ValuesSummary` markup with an empty state or a 120 px read-only virtual grid using 32 px rows; keep Color markup and card actions unchanged.
- [x] 2.2 Bind truncated visible row text to the full tooltip and automation name/help text, and verify the grid remains inside the existing wrapped card width.

## 3. Acceptance and regression verification

- [x] 3.1 Add headless view coverage for Size/custom order, empty values, long values, scrolling/realized-row virtualization, accessibility properties, card wrapping, and unchanged Color/Manage values/archive actions.
- [x] 3.2 Record criterion-level results and evidence in `verification.md`; fix any uncovered acceptance gaps and rerun affected checks.
- [x] 3.3 Run focused App tests and `dotnet test .\FusionCanvas.sln -m:1`.
- [x] 3.4 Run `openspec validate --all --strict` and `git diff --check`.
