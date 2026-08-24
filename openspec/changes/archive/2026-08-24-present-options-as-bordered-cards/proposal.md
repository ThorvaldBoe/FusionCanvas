## Why

On Manage Variants → Available choices, Color, Size, and other Options currently run together against the page background without a clear visual boundary, making the section harder to scan than the approved mockup. The repository's `manage-variants.ui.yaml` already models Options as `choice-card` panels; this change aligns the Avalonia presentation with that established design direction without changing domain or persistence behavior.

## What Changes

- Add a shared `choiceCard` presentation style built from existing semantic theme resources (elevated surface background, a subtle border brush that remains visible in both Light and Dark appearance, consistent corner radius, and 1px boundary).
- Render every available Option — Color, Size, Other/custom kinds, and empty Options — as a distinct compact bordered card that contains the Option name, kind label, current value summary, the Manage values action, and the Archive Option action within one boundary.
- Keep cards aligned cleanly in the available width (multiple cards per row at the default window width) and wrap/stack them gracefully at narrower supported widths.
- Wrap long Option names and value summaries so card layout does not clip them.
- Add a stable automation identifier for each choice card and focused headless view coverage verifying card creation for multiple Option kinds and responsive stacking.
- No new domain concepts, database fields, schema migrations, or behavioral rule changes.

## Capabilities

### New Capabilities

None. This change only refines existing catalog management presentation.

### Modified Capabilities

- `variant-management`: adds observable requirements that the Available choices region renders each Option as a bordered choice card with a theme-driven boundary, consistent geometry, no clipped names or summaries, and headless-verifiable responsive alignment and stacking.

## Impact

- `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml` — new `Border.choiceCard` style and the Available choices card template (boundary, geometry, wrapping, text wrapping, automation id).
- `tests/FusionCanvas.App.Tests/StoreEditorHeadlessTests.cs` — headless view test covering multiple Option kinds, applied theme border/background/corner radius, side-by-side alignment at the default width, and stacking at the minimum supported width.
- Composes with the on-going normalized Variant/Option presentation from catalog-management changes; no Domain, Application, Integration, schema, or data changes are required.
- The Option-level Archive action stays on the card for now; moving it into an overflow menu is Issue #192 and remains out of scope.

## Origin

- GitHub Issue [#193](https://github.com/ThorvaldBoe/FusionCanvas/issues/193) — `[Feature]: Present each available Option in a bordered card`