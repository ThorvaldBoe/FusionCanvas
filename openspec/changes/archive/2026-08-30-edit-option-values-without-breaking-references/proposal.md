## Why

Catalog values are often reused by Variants and mockup/template relationships. Today a typo can only be corrected by archiving the value and creating a replacement, which breaks the stable reference workflow and forces manual repair. This module adds safe in-place renaming for Color and Size values in the existing focused management dialog.

## What Changes

- Add an Edit action for each active Option Value in the value-management dialog.
- Open the selected value's current display name in an editable form and save a validated rename.
- Reuse create-value validation semantics: trimmed non-blank names, normalized duplicate detection among active values in the same Option, and the existing recoverable error presentation.
- Persist a rename against the existing Option Value identity so Variant memberships, template/value links, and other relationships remain intact.
- Refresh the dialog list, parent Option summary, and dependent catalog views after a successful rename.
- Leave confirmed data unchanged when editing is cancelled, dismissed, or rejected.

## Capabilities

### New Capabilities

- None. This is a modification to the existing catalog value-management capability.

### Modified Capabilities

- `variant-management`: active Option Values in the focused management dialog can be renamed in place while preserving identity, references, validation, cancellation, and refresh semantics.

## Impact

- `FusionCanvas.Application.Catalog`: expose the existing catalog update boundary for Option Value renames and enforce scoped validation/duplicate rules.
- `FusionCanvas.App.Stores`: add edit draft state, commands, and refresh behavior to `CatalogSetupViewModel` and the Option Value management dialog.
- `FusionCanvas.Domain.Catalog`: preserve the existing immutable value model and stable identity contract.
- Tests in application and app test projects will cover Color and Size parity, validation, duplicate rejection, cancellation, identity/reference preservation, and presentation refresh.
- No database schema or migration is required because relationships already reference Option Value IDs.
