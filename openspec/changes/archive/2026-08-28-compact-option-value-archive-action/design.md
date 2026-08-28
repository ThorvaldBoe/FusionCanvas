## Context

Issue #194 moved Option Value management into `OptionValueManagementWindow`, but the row template retained its previous large red **Archive Option Value** button. In a focused dialog, the row and title already establish record type, so repeated full labels and strong fill create unnecessary visual noise. The existing `ArchiveOptionValueCommand` owns behavior and must remain unchanged.

The primary workflow is scanning and maintaining values; archiving is occasional and destructive. The focused dialog is already the correct surface and its footprint remains fixed. Empty, add-draft, blocked, error, cancel, and completion states remain as implemented. Keyboard order follows the visual row order, and dialog focus/cancellation behavior is unchanged.

## Goals / Non-Goals

**Goals:**

- Make the per-row action concise and visually secondary while retaining destructive meaning.
- Keep value text readable at the dialog's supported width.
- Expose a value-specific accessible name and deterministic row order.
- Prove command reuse and presentation behavior through headless tests.

**Non-Goals:**

- Changing archive eligibility, confirmation, service calls, persistence, or error messages.
- Redesigning Option-level archiving or adding an overflow menu.
- Changing dialog lifetime, add-value drafting, or focus restoration.

## Decisions

1. **Use a compact visible button rather than an overflow menu.** The issue favors discoverability, and archive is the only row action. A concise button avoids another interaction layer while reducing prominence.
2. **Use an unfilled restrained destructive style.** A dedicated `compactDanger` class will use danger-colored foreground/border with smaller padding, instead of the solid danger fill used for primary confirmations. This preserves semantics without competing with primary actions.
3. **Use a two-column row grid.** The value occupies the star-sized column and wraps; the action occupies an auto-sized column. This guarantees alignment and prevents long names from colliding with the control.
4. **Derive the accessible name in the App presentation layer.** The row model will expose a small binding-friendly presentation property through an App-owned converter or multi-binding approach, avoiding changes to Domain records. Prefer Avalonia `MultiBinding` with `StringFormat` if compiled binding support is reliable; otherwise introduce one focused App converter.
5. **Keep command and parameter bindings intact.** The button remains bound to the dialog data context's `ArchiveOptionValueCommand` with the row's `OfferingOptionValue` as parameter.

## Risks / Trade-offs

- [A restrained destructive style may look too similar to a neutral secondary action] → retain danger semantic color and verify class resources in headless style resolution plus targeted markup review.
- [Long unbroken names can force measurement pressure] → wrap the value in the star column and keep the action auto-sized with a minimum gap.
- [Accessible-name binding can silently fail in a data template] → assert the resolved `AutomationProperties.Name` for multiple row values in a headless test.

## Migration Plan

No data or API migration is required. The XAML-only presentation can be reverted without affecting confirmed catalog data.

## Open Questions

None. The issue, accepted dialog behavior, and UI/UX guidelines resolve the material decisions.

## Implementation Plan

1. Update `src/FusionCanvas.App/Stores/OptionValueManagementWindow.axaml`:
   - replace the horizontal `StackPanel` row with a star/auto `Grid`;
   - wrap the value text in the first column;
   - change the visible label to **Archive**;
   - apply the compact restrained destructive class;
   - bind a target-specific automation name while preserving command and parameter bindings.
2. Add only the smallest App-layer binding helper if XAML cannot express the target-specific name directly. Do not modify Domain, Application, Integration, or persistence types.
3. Extend `tests/FusionCanvas.App.Tests/StoreEditorHeadlessTests.cs` with a focused dialog test covering Color/Size/custom-equivalent row rendering, long names, visible label, style, automation names, ordering, and the existing command binding/parameter.
4. Update the relevant UI description only if it explicitly names the old row action.
5. Run the focused App test project, the solution test baseline, and strict OpenSpec validation; record criterion-level evidence in `verification.md`.

## Acceptance-to-Verification Mapping

| Scenario | Verification |
| --- | --- |
| User scans values for any Option kind | Headless dialog test creates representative values and asserts one consistent compact action per row plus the restrained class. |
| Long value name shares a row with Archive | Headless layout test measures the dialog at its supported width and asserts value/action bounds do not overlap or clip. |
| Assistive technology identifies the archive target | Headless test asserts target-specific automation names and visual/focus ordering. |
| User invokes compact Archive | Headless test inspects command/parameter binding and invokes the action against a deterministic view model; existing application/view-model regression tests cover safeguards. |

