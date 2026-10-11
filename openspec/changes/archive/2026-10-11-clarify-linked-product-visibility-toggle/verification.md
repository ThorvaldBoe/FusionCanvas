# Verification — Clarify linked-product visibility toggle

| Acceptance scenario | Result | Evidence |
| --- | --- | --- |
| Linked products are hidden by default | Passed | `PrintifyListingImportWindowTests.Dialog_renders_products_and_disables_already_linked_product_selection` verifies only the unlinked row is shown and the button's content and accessible name are “Show linked products”. |
| User shows linked products | Passed | The same headless Avalonia test activates the command, flushes the UI dispatcher, verifies the linked row and “Already linked” marker appear, the linked checkbox is disabled, and both label surfaces read “Hide linked products”. It also verifies a linked-only selection cannot enable import. |
| User hides linked products again | Passed | The same headless Avalonia test activates the command again and verifies the linked row disappears and both label surfaces return to “Show linked products”. |

## Required validation

- Focused headless UI tests: `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore --filter FullyQualifiedName~PrintifyListingImportWindowTests` — passed (2 tests).
- Solution baseline: `dotnet test .\FusionCanvas.sln -m:1` — passed (exit code 0; all solution test projects completed without failures).
- Strict OpenSpec validation: `openspec validate --all --strict` — passed (90 items).
- `git diff --check` — passed.

The test run reports existing SixLabors.ImageSharp package vulnerability advisories (NU1902/NU1903); this change does not alter package references.
