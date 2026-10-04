# Verification: app-wide-terms-consent

## Evidence status

The consent contract, application-settings persistence, startup gate, consent UI, and Settings review surface are implemented. The architecture and UI audit corrections are implemented: acceptance persistence is owned by `TermsConsentService`, the startup decision is isolated behind `TermsConsentStartupCoordinator`, and the consent surface uses semantic design tokens. The product-owner-approved policy is bundled as version `0.1` without a draft marker; separate legal-counsel review remains external to this implementation evidence.

## Acceptance criteria

| Criterion | Evidence | Result |
| --- | --- | --- |
| First launch gates normal workspace | `TermsConsentStartupCoordinator` requires a consent decision before `App.InitializeStartupAsync` calls normal `AppServicesFactory`/`MainWindow` composition. | Passed; startup coordinator tests cover missing, accepted, and declined decisions. |
| Current acknowledgement continues startup | `TermsConsentPolicy.IsCurrent` is checked before showing the consent surface. | Passed; the current record bypasses the consent surface. |
| Decline/close quits without workspace composition | A declined consent decision returns `null`; `App` shuts down before constructing services. | Passed; the composition-blocking result is covered. |
| Four responsibilities are separate | `TermsConsentWindow.axaml` contains four individually bound `CheckBox` controls and official Printify/Shopify links. | Passed; rendered headless test activates all four controls. |
| Primary action is gated | `TermsConsentViewModel.CanAgree` requires all four selections and the XAML binds `IsEnabled`. | Passed; focused App/headless tests passed. |
| Provider acknowledgement is honest | Consent copy states provider terms remain the user's responsibility and that FusionCanvas does not accept agreements on the user's behalf. | Passed by XAML and bundled policy copy. |
| Consent is minimal and versioned | `TermsConsentRecord` stores only terms version, acknowledgement-policy version, and UTC timestamp. | Passed; integration round-trip/malformed/missing tests passed. |
| Missing/stale consent is required | `TermsConsentPolicy.IsCurrent` rejects missing or mismatched versions. | Passed; application tests passed. |
| Save failure retains the form | `TermsConsentViewModel` keeps selections, reports the save warning, and raises `Accepted` only after success. | Passed; focused App test passed. |
| Offline FusionCanvas policy remains readable | `FusionCanvasTermsOfUse.md` is an Avalonia resource loaded locally; provider links are explicitly external. | Passed; local policy resource and policy-presentation tests passed. |
| Settings reviews current state | `SettingsSection.Terms`, `TermsConsentSummary`, and the review route use `SettingsViewModel.CreateTermsConsentViewModel`, preserving one persistence owner. | Passed; Settings summary and review tests passed. |
| Existing layout behavior remains after consent | Normal startup still creates the existing `MainWindow` and passes the loaded settings through `AppServicesFactory`. | Passed by focused startup/layout tests and the full baseline. |

## Commands

- `openspec validate app-wide-terms-consent --strict` — passed.
- `dotnet build .\src\FusionCanvas.Application\FusionCanvas.Application.csproj --no-restore` — passed.
- `dotnet test .\tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj --no-restore -m:1 --filter FullyQualifiedName~TermsConsent` — passed: 2 service tests; existing analyzer warnings remain.
- `dotnet test .\tests\FusionCanvas.Integration.Tests\FusionCanvas.Integration.Tests.csproj --no-restore -m:1 --filter FullyQualifiedName~JsonApplicationSettingsStore` — passed: 30 persistence tests; existing analyzer warnings remain.
- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore -m:1 --filter FullyQualifiedName~TermsConsent` — passed: 18 consent/startup/headless tests.
- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-build --no-restore -m:1 --filter FullyQualifiedName~BlueprintDetailContext_UsesTheTabContentInset` — passed: 1 Store Editor inset test.
- `dotnet test .\FusionCanvas.sln -m:1 -p:UseSharedCompilation=false -v minimal` — passed: Domain 284, Application 603, Integration 315, App 942, and UiDescription 29; 2,173 total tests.

### Scenario coverage mapping

- `terms-consent`: missing/current/declined startup state — `TermsConsentStartupCoordinatorTests`; four responsibilities, honest provider wording, selection gating, keyboard focus, close behavior, save progress, error retention, and acceptance — `TermsConsentViewModelTests` and `TermsConsentWindowTests`; version evaluation and persistence orchestration — `TermsConsentServiceTests` and `TermsConsentSettingsTests`.
- `application-settings`: missing, current, stale, malformed, round-tripped, and save-failure consent state — `JsonApplicationSettingsStoreTests` and `TermsConsentServiceTests`.
- `desktop-application-foundation`: valid, legacy, invalid, off-screen, and clamped layout behavior — `MainWindowLayoutNormalizerTests` and `MainWindowLayoutTests`; consent-gated startup behavior — `TermsConsentStartupCoordinatorTests` and the full App suite.

The optional real-desktop journey is not warranted for this module: the first-run decision boundary, focusable controls, provider-link commands, save failure, and visual states are covered deterministically, while native browser behavior is intentionally delegated to the operating system.

## Audit correction evidence

Audit finding #802 is addressed by the startup consent lifecycle correction:

| Finding criterion | Evidence | Result |
| --- | --- | --- |
| Quit/close cannot race an in-flight consent save | `TermsConsentViewModelTests.RequestQuit_DuringSave_DoesNotRaceThePendingAcceptance` verifies `QuitCommand` is disabled and `RequestQuit()` emits no shutdown decision while saving | Pass |
| Startup cancellation reaches the settings save and is awaited before shutdown | `TermsConsentViewModelTests.StartupCancellation_CancelsSaveAndPendingSaveWaitCompletes`; `App.InitializeStartupAsync` passes the startup token and awaits `WaitForPendingSaveAsync()` before shutdown | Pass |

Focused verification after the correction:

- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore -m:1 --filter FullyQualifiedName~TermsConsentViewModelTests` — passed: 6 tests.

## Release gate

Task 1.1 is complete for the requested product decision: the user-approved wording is bundled as FusionCanvas terms version `0.1` without a draft marker. This records product-owner approval; any separate legal-counsel review remains outside the implementation evidence.

Audit finding #803 is addressed by the startup coordinator and rendered consent coverage:

| Finding criterion | Evidence | Result |
| --- | --- | --- |
| Missing, accepted, and declined startup decisions are covered | `TermsConsentStartupCoordinatorTests` covers the three decision paths and verifies whether the composition result is `null` or accepted settings. | Pass |
| Rendered checkbox interaction is covered | `TermsConsentWindowTests.Window_ShowsOfflinePolicyFourCheckboxesAndGatedActions` selects each rendered checkbox through headless pointer input and verifies the gated action becomes enabled. | Pass |
| Keyboard-reachable quit and visible save/error states are covered | `TermsConsentWindowTests` covers keyboard activation of Quit, the visible saving progress bar while persistence is blocked, and the visible persistence error while selections remain checked. | Pass |

Audit finding #811 is addressed by the shared `Token.Layout.PageInset` margin on the Blueprint detail context and its focused headless inset assertion.
