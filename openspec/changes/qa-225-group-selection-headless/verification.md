# Verification

## Acceptance Evidence

| Acceptance scenario | Verification | Result |
| --- | --- | --- |
| Dialog bindings and destination selection are covered | `GroupSelectionHeadlessTests.Window_BindsNameAndDestinationsAndSelectsDefaultDestination` exercises constructor state, `TextBox.Text`, `ComboBox.SelectedItem`, and layout-bound controls | PASS: focused run passed |
| Invalid confirmation remains open and reports validation | `GroupSelectionHeadlessTests.ConfirmWithMissingName_ShowsValidationAndKeepsWindowOpen` clicks Group with an empty name and asserts the validation message, visible error text, and `IsVisible` | PASS: focused run passed |
| Valid confirmation closes the dialog | `GroupSelectionHeadlessTests.ConfirmWithNameAndDestination_ClosesWindow` clicks Group after setting a valid name and asserts the window is no longer visible | PASS: focused run passed |

## Commands

- Focused test: `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore --filter FullyQualifiedName~GroupSelectionHeadlessTests -m:1` — 3 passed
- Baseline: `dotnet test .\FusionCanvas.sln`
- Strict specs: `openspec validate --specs --strict`
- Strict changes: `openspec validate --changes --strict`
