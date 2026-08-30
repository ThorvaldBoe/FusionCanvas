# Verification

## Status

The package was revised after manual UX feedback. Earlier focused passes remain useful implementation history, but they do not satisfy the revised acceptance criteria. Every result below is pending until the master-detail workflow and incomplete-entry model are implemented and reverified.

## Acceptance-to-evidence map

| Capability and scenario | Planned deterministic evidence | Result |
| --- | --- | --- |
| Source images — Creator adds a local source image | Application service tests for managed import and stable Asset identity; SQLite round trip | Pending |
| Source images — Creator saves uploaded images before configuring them | Domain optional-state tests; Application save/reload test; SQLite incomplete-row round trip; headless row-status test | Pending |
| Source images — Import cannot complete | Application fake file-store/inspector failure tests and cleanup assertions | Pending |
| Source images — Creator replaces a source image | Application revision-provenance test and SQLite current/history reconstruction | Pending |
| Source images — Creator archives a source image | Domain/Application archive and readiness tests; ViewModel selection-aftermath test; headless confirmation/action-state test | Pending |
| Applicability — Creator configures common color-specific source images | Domain one-Color/all-Sizes resolution test; ViewModel default-control test | Pending |
| Applicability — Creator selects alternatives within and conditions across Options | Domain parameterized OR-within/AND-between tests using stable Option/Value identities | Pending |
| Applicability — Creator attempts an invalid applicability assignment | Domain/Application ownership, archived-record, empty-group, and duplicate validation tests | Pending |
| Applicability — Creator leaves applicability unconfigured | Domain no-match test; Application completeness summary test; SQLite zero-condition round trip | Pending |
| Resolution — Color-only images cover every compatible variant | Domain exact-one policy test and Application ready summary | Pending |
| Resolution — A variant has no matching image | Domain missing-result test asserting other exact-one results remain intact | Pending |
| Resolution — A variant matches more than one image | Domain ambiguity test asserting no implicit choice and unaffected results remain intact | Pending |
| Resolution — A future consumer requests an unresolved variant | Domain/Application result-contract test asserting recoverable per-Variant outcome without aggregate exception | Pending |
| Mapping — Creator adds a source image | Application/ViewModel test asserting inspected dimensions and absent initial mapping | Pending |
| Mapping — Creator configures placement for one image | Domain bounds test; ViewModel selection/draft-retention test; SQLite source-specific mapping round trip | Pending |
| Mapping — Creator enters an invalid mapping | Domain/Application validation test and headless inline-error/save-state test | Pending |
| Revision — Creator changes source applicability | Domain revision-copy test and SQLite immutable-history round trip | Pending |
| Asset protection — Creator attempts to delete a referenced asset | Application dependency-guard tests for current and historical source references | Pending |
| Editor — Creator opens a new Template dialog | Headless construction, accessible-name, focus, and provider-state-absence test | Pending |
| Editor — Creator uploads images independently of metadata | ViewModel picker/draft test and headless table/lower-editor binding test | Pending |
| Editor — Creator selects an image row | ViewModel per-row draft-retention test and headless selection test | Pending |
| Editor — Editor reports image completeness | Framework-free completeness tests and headless complete/incomplete/status presentation test | Pending |
| Editor — Creator cancels while configuring sources | ViewModel discard-state test and headless Escape/close/focus test | Pending |
| Editor — Archived Store is reviewed | ViewModel command-state test and headless read-only presentation test | Pending |
| Supplier setup — User opens local source-image configuration | Headless persistent-label, instruction, and upload-action accessibility test | Pending |
| Supplier setup — Template has no source images | ViewModel empty-state test and headless upload-enabled test | Pending |
| Supplier setup — Template source configuration is incomplete | Application per-Variant summary test; headless actionable state; save-remains-enabled test | Pending |
| Supplier setup — Template source configuration is ready | Application readiness test and headless selected-image placement presentation | Pending |
| Supplier setup — Local source import fails | Application failure-preservation test and headless recoverable-error presentation | Pending |

## Design evidence

- Validate `docs/Visuals/ui-descriptions/mockup-template-image-editor.ui.yaml`.
- Render and retain `selected-incomplete`, `selected-complete`, and `no-selection` states.
- Compare the implemented AXAML hierarchy and interaction states with the approved semantic source; document intentional differences.

## Required commands

- `dotnet run --project .\tools\FusionCanvas.UiDescription -- validate .\docs\Visuals\ui-descriptions\mockup-template-image-editor.ui.yaml`
- Render all three declared UI-description states with the documented renderer.
- `dotnet test .\tests\FusionCanvas.Domain.Tests\FusionCanvas.Domain.Tests.csproj`
- `dotnet test .\tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj`
- `dotnet test .\tests\FusionCanvas.Integration.Tests\FusionCanvas.Integration.Tests.csproj`
- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj`
- `dotnet test .\tests\FusionCanvas.UiDescription.Tests\FusionCanvas.UiDescription.Tests.csproj`
- `openspec validate add-local-mockup-template-sources --strict`
- `dotnet test .\FusionCanvas.sln`

## Changed-scope review

Before completion, review architecture placement, optional-state invariants, SQLite migration compatibility, file and image-input safety, Asset/revision retention, keyboard/focus behavior, UI-description drift, and delta-to-implementation drift. Printify/API retrieval, credentials, drag-and-drop, rendering/composition, Listing integration, and marketplace publication remain excluded.
