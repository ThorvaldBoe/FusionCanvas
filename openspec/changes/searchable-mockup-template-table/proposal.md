## Why

The Offering-scoped Mockup Template list repeats several fields and actions in unaligned cards, making catalogs harder to scan as they grow. A compact searchable table will make templates easier to compare while preserving their existing editing, duplication, archive, readiness, and Design Area setup workflows.

## What Changes

- Replace the Mockup Template card list with an aligned, bounded virtual table showing template name, target Design Area, Color applicability, compatible Variant summary, and revision/readiness status.
- Add transient case-insensitive search across the displayed template fields, with clear no-results guidance; do not add sorting.
- Preserve full summary text through accessible names and tooltips when cells truncate.
- Keep Edit, Duplicate, and Archive available for the selected row in a compact action strip; show the selected row's complete summary below the table and add a Cancel-first confirmation before the reversible soft archive.
- Preserve the existing empty collection state, provider message, and blocked warning and route when the Offering has no Design Areas.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `mockup-template-management`: define searchable aligned table presentation, full-text access, retained row actions, empty/no-results and no-Design-Area states, and guarded archive behavior.

## Impact

- App UI and presentation state: `StoreEditorWindow.axaml`, `StoreEditorWindow.axaml.cs`, `CatalogSetupViewModel`, and focused Store Editor headless tests.
- OpenSpec delta for `mockup-template-management`; no domain, application, persistence, API, migration, or dependency changes.
- Verification covers transient filtering, row-summary visibility/accessibility, action routing and confirmation/cancel behavior, blocked Design Area guidance, and the solution baseline. Headless Avalonia coverage is warranted for grid layout, routed action input, and dialog focus. A real-desktop Appium journey is not warranted for this focused catalog-list replacement; existing integration coverage owns archive persistence semantics.

This is one reviewable workspace presentation change: it keeps the current Offering-scoped workflow and uses the existing grid component and soft-archive service without introducing a new catalog capability.
