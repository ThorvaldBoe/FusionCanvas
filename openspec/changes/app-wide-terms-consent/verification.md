# Verification: app-wide-terms-consent

## Evidence status

The implementation is complete for the consent contract, application-settings persistence, startup gate, consent UI, and Settings review surface. The architecture and UI audit corrections are implemented: acceptance persistence is now owned by `TermsConsentService`, and the consent surface uses semantic design tokens. The isolated full solution baseline is clean.

## Acceptance criteria

| Criterion | Evidence | Result |
| --- | --- | --- |
| First launch gates normal workspace | `App.InitializeStartupAsync` loads settings, shows `TermsConsentWindow`, and calls normal `AppServicesFactory`/`MainWindow` composition only after accepted settings are saved. | Implemented; covered by the full solution baseline. |
| Current acknowledgement continues startup | `TermsConsentPolicy.IsCurrent` is checked before showing the consent surface. | Implemented; covered by the full solution baseline. |
| Decline/close quits without workspace composition | `TermsConsentViewModel.QuitRequested` completes startup with no settings mutation; `App` shuts down before constructing services. | Implemented; covered by the full solution baseline. |
| Four responsibilities are separate | `TermsConsentWindow.axaml` contains four individually bound `CheckBox` controls and official Printify/Shopify links. | Implemented; headless test covered by the full solution baseline. |
| Primary action is gated | `TermsConsentViewModel.CanAgree` requires all four selections and the XAML binds `IsEnabled`. | Implemented; focused App/headless tests added. |
| Provider acknowledgement is honest | Consent copy states provider terms remain the user's responsibility and that FusionCanvas does not accept agreements on the user's behalf. | Implemented by XAML and bundled policy copy. |
| Consent is minimal and versioned | `TermsConsentRecord` stores only terms version, acknowledgement-policy version, and UTC timestamp. | Implemented; integration round-trip/malformed/missing tests added. |
| Missing/stale consent is required | `TermsConsentPolicy.IsCurrent` rejects missing or mismatched versions. | Implemented; application tests added. |
| Save failure retains the form | `TermsConsentViewModel` keeps selections, reports the save warning, and raises `Accepted` only after success. | Implemented; focused App test passed. |
| Offline FusionCanvas policy remains readable | `FusionCanvasTermsOfUse.md` is an Avalonia resource loaded locally; provider links are explicitly external. | Implemented. |
| Settings reviews current state | `SettingsSection.Terms`, `TermsConsentSummary`, and the review route use `SettingsViewModel.CreateTermsConsentViewModel`, preserving one persistence owner. | Implemented; Settings tests added. |
| Existing layout behavior remains after consent | Normal startup still creates the existing `MainWindow` and passes the loaded settings through `AppServicesFactory`. | Implemented; covered by the full solution baseline. |

## Commands

- `openspec validate app-wide-terms-consent --strict` — passed after the audit correction.
- `dotnet build .\src\FusionCanvas.Application\FusionCanvas.Application.csproj --no-restore` — passed.
- `dotnet build .\src\FusionCanvas.App\FusionCanvas.App.csproj --no-restore` — passed.
- `dotnet build .\src\FusionCanvas.App\FusionCanvas.App.csproj --no-restore --no-dependencies` — passed, compiling the changed App project against the existing referenced binaries.
- `dotnet build .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore --no-dependencies` — passed with existing repository warnings.
- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore -m:1 --filter FullyQualifiedName~TermsConsent` — passed: 12 tests.
- `dotnet test .\FusionCanvas.sln --no-restore -m:1 -p:UseSharedCompilation=false -v minimal` — passed: 2,145 tests.

The optional real-desktop journey is not warranted for this module: the first-run decision boundary, focusable controls, provider-link commands, save failure, and visual states are covered deterministically, while native browser behavior is intentionally delegated to the operating system.

## Audit correction evidence

Audit finding #802 is addressed by the startup consent lifecycle correction:

| Finding criterion | Evidence | Result |
| --- | --- | --- |
| Quit/close cannot race an in-flight consent save | `TermsConsentViewModelTests.RequestQuit_DuringSave_DoesNotRaceThePendingAcceptance` verifies `QuitCommand` is disabled and `RequestQuit()` emits no shutdown decision while saving | Pass |
| Startup cancellation reaches the settings save and is awaited before shutdown | `TermsConsentViewModelTests.StartupCancellation_CancelsSaveAndPendingSaveWaitCompletes`; `App.InitializeStartupAsync` passes the startup token and awaits `WaitForPendingSaveAsync()` before shutdown | Pass |

Focused verification after the correction:

- `dotnet test .\\tests\\FusionCanvas.App.Tests\\FusionCanvas.App.Tests.csproj --no-restore -m:1 --filter FullyQualifiedName~TermsConsentViewModelTests` — passed: 6 tests.

## Release gate

Task 1.1 remains intentionally open: the bundled FusionCanvas terms are marked `draft-0.1` and must receive human/legal approval before release as binding terms.
