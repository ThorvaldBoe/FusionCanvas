# Compact Option Value Archive Action Verification

## Acceptance Evidence

| Acceptance scenario | Method | Result | Evidence and limitations |
| --- | --- | --- | --- |
| User scans values for any Option kind | Avalonia headless test | Pass | `StoreEditorHeadlessTests.ManageValues_ArchiveActionsAreCompactTargetSpecificAndKeepLongValuesReadable` opens the focused dialog for Color and Size, then creates and opens a custom Material Option. Every rendered row uses visible `Archive` content and the `compactDanger` class. |
| Long value name shares a row with Archive | Avalonia headless layout assertion | Pass | The same test creates `Extraordinarily long recycled cotton blend material value`, verifies wrapping, and asserts the value bounds do not overlap the action bounds at the supported dialog size. This is deterministic layout evidence; no pixel-perfect visual regression was required. |
| Assistive technology identifies the archive target | Avalonia headless accessibility assertions | Pass | The test asserts `AutomationProperties.Name` resolves to `Archive <value>` for Color, Size, and custom values and that actions follow item order in the visual tree. |
| User invokes compact Archive | Avalonia headless command and state assertions | Pass | The test proves the button reuses `ArchiveOptionValueCommand`, passes the exact row model, invokes it once, and observes only that value leave the collection. Existing catalog tests retain dependency and persistence regression coverage. |

## Required Gates

- Focused command: `dotnet test tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore --filter FullyQualifiedName~ManageValues_ArchiveActionsAreCompactTargetSpecificAndKeepLongValuesReadable --logger "console;verbosity=normal"`
  - Result: 1 passed, 0 failed.
- Solution baseline: `dotnet test .\FusionCanvas.sln --logger "console;verbosity=minimal"`
  - Result: 1,408 passed, 0 failed, 0 skipped (Domain 232; Application 384; Integration 189; App 576; UI-description 27).
- `openspec validate --specs --strict`
  - Result: 43 passed, 0 failed.
- `openspec validate --changes --strict`
  - Result: 11 passed, 0 failed, including this change.
- `git diff --check`
  - Result: passed; Git emitted only expected line-ending normalization notices.

## Supplemental Review

- The UI-description sources contain no prior **Archive Option Value** row label, so no UI-description edit was necessary.
- Live desktop visual review was not required because the layout, styling class, accessibility name, and command behavior are covered deterministically. A future manual review may fine-tune semantic colors without changing accepted behavior.

