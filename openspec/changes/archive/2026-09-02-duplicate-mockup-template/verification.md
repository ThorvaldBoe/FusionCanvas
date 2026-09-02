# Duplicate Mockup Template Verification

## Acceptance criteria

| Scenario | Method | Result | Evidence | Limitations |
| --- | --- | --- | --- | --- |
| Creator duplicates a configured template | Focused xUnit application test | Pass | `DuplicateTemplateCopiesCurrentConfigurationWithNewTemplateScopedIdentities`; copied target, provider mapping, Color, applicability, mapping, shared Asset, and unchanged source records | Test fixture does not exercise a real SQLite file, which is covered by the existing integration persistence suite |
| Duplicate records are independently mutable | Service identity/record isolation assertions and existing source-image update contract coverage | Pass | Duplicate source-entry and revision IDs differ; original source entry remains attached to original template | Full file-import replacement path remains covered by existing `MockupTemplateSourceImageServiceTests` behavior |
| Duplicate opens as an editable draft | Application/UI integration inspection plus existing focused editor workflow tests | Pass by design | `DuplicateTemplateAsync` applies returned state, clears busy state, calls `BeginEditTemplate`, and raises the existing editor request; existing editor owns draft baseline/cancellation | No new headless test was added for static command markup; current App headless run has unrelated pre-existing StoreEditor failures |
| Archived Store duplication is blocked | Focused application tests and existing read-only command patterns | Pass | `EnsureWritable` rejects before mutation with “Archived Store catalogs are read-only.” | None |
| Missing/out-of-scope source duplication is blocked | Focused application service path review/test coverage | Pass | Source lookup requires active template and matching Store-owned active Offering; failure returns before snapshot mutation | None |
| Copy name collisions remain distinguishable | Focused xUnit application test | Pass | `DuplicateTemplateUsesNextAvailableCopyNameAndRejectsArchivedSource` produces `Copy of Front (3)` and rejects archived source | None |

## Required validation

- `git diff --check`: pass.
- `openspec validate --all`: pass, 58 items.
- `dotnet test .\FusionCanvas.sln -m:1 --no-restore` with `AVALONIA_TELEMETRY_OPTOUT=1`: Domain 240 passed; Application 389 passed; Integration 190 passed; App 584 passed, 11 failed.
- App failures are existing StoreEditor headless assertions around provider data, mapping controls, compact layout, and mockup-template dialog visibility/width; none reference the duplicate command or changed application behavior. The failure output is retained as a completion limitation and does not justify unrelated UI changes.

## Criterion conclusion

Application behavior and the duplicate command path are implemented and focused application tests pass. The solution baseline is partially green because of unrelated existing App headless failures; no acceptance criterion for duplication failed.
