# Verification: app-wide-terms-consent

## Evidence status

The implementation is complete for the consent contract, application-settings persistence, startup gate, consent UI, and Settings review surface. The architecture and UI audit corrections are implemented: acceptance persistence is now owned by `TermsConsentService`, and the consent surface uses semantic design tokens. Consent-focused verification is clean. The current merged checkout's full solution baseline remains blocked by an unrelated pre-existing mockup-source headless test failure.

## Acceptance criteria

| Criterion | Evidence | Result |
| --- | --- | --- |
| First launch gates normal workspace | `App.InitializeStartupAsync` loads settings, shows `TermsConsentWindow`, and calls normal `AppServicesFactory`/`MainWindow` composition only after accepted settings are saved. | Passed by focused startup tests; prior isolated full baseline passed. |
| Current acknowledgement continues startup | `TermsConsentPolicy.IsCurrent` is checked before showing the consent surface. | Passed by focused startup tests; prior isolated full baseline passed. |
| Decline/close quits without workspace composition | `TermsConsentViewModel.QuitRequested` completes startup with no settings mutation; `App` shuts down before constructing services. | Passed by focused startup tests; prior isolated full baseline passed. |
| Four responsibilities are separate | `TermsConsentWindow.axaml` contains four individually bound `CheckBox` controls and official Printify/Shopify links. | Passed; focused and full App/headless tests passed. |
| Primary action is gated | `TermsConsentViewModel.CanAgree` requires all four selections and the XAML binds `IsEnabled`. | Passed; focused App/headless tests passed. |
| Provider acknowledgement is honest | Consent copy states provider terms remain the user's responsibility and that FusionCanvas does not accept agreements on the user's behalf. | Passed by XAML and bundled policy copy. |
| Consent is minimal and versioned | `TermsConsentRecord` stores only terms version, acknowledgement-policy version, and UTC timestamp. | Implemented; integration round-trip/malformed/missing tests added. |
| Missing/stale consent is required | `TermsConsentPolicy.IsCurrent` rejects missing or mismatched versions. | Implemented; application tests added. |
| Save failure retains the form | `TermsConsentViewModel` keeps selections, reports the save warning, and raises `Accepted` only after success. | Implemented; focused App test passed. |
| Offline FusionCanvas policy remains readable | `FusionCanvasTermsOfUse.md` is an Avalonia resource loaded locally; provider links are explicitly external. | Passed; local policy resource and policy-presentation tests passed. |
| Settings reviews current state | `SettingsSection.Terms`, `TermsConsentSummary`, and the review route use `SettingsViewModel.CreateTermsConsentViewModel`, preserving one persistence owner. | Passed; Settings summary and review tests passed. |
| Existing layout behavior remains after consent | Normal startup still creates the existing `MainWindow` and passes the loaded settings through `AppServicesFactory`. | Passed by focused startup/layout tests; prior isolated full baseline passed. |

## Commands

- `openspec validate app-wide-terms-consent --strict` — passed.
- `dotnet build .\src\FusionCanvas.Application\FusionCanvas.Application.csproj --no-restore` — passed.
- `dotnet test .\tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj --no-restore -m:1 --filter FullyQualifiedName~TermsConsent` — passed: 2 service tests; existing analyzer warnings remain.
- `dotnet test .\tests\FusionCanvas.Integration.Tests\FusionCanvas.Integration.Tests.csproj --no-restore -m:1 --filter FullyQualifiedName~JsonApplicationSettingsStore` — passed: 30 persistence tests; existing analyzer warnings remain.
- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore -m:1 --filter FullyQualifiedName~TermsConsent` — passed: 10 consent/headless tests.
- `dotnet test .\FusionCanvas.sln -m:1` — current merged-checkout attempt: Domain 279, Application 603, Integration 315, and UiDescription 29 passed; App had 926 passed and 1 failed. The failure is `StoreEditorHeadlessTests.MockupSourceRow_SelectsFromCellsWhitespaceAndKeyboard_WithoutArchiving` in the mockup-template surface, which is outside this change's scope and predates the consent implementation. The prior isolated consent worktree baseline passed 2,118 tests.

### Scenario coverage mapping

- `terms-consent`: missing/current/declined startup state — `TermsConsentStartupGateTests`; four responsibilities, honest provider wording, selection gating, policy presentation, focusable links, error retention, and acceptance — `TermsConsentViewModelTests` and `TermsConsentWindowTests`; version evaluation and persistence orchestration — `TermsConsentServiceTests` and `TermsConsentSettingsTests`.
- `application-settings`: missing, current, stale, malformed, round-tripped, and save-failure consent state — `JsonApplicationSettingsStoreTests` and `TermsConsentServiceTests`.
- `desktop-application-foundation`: valid, legacy, invalid, off-screen, and clamped layout behavior — `MainWindowLayoutNormalizerTests` and `MainWindowLayoutTests`; consent-gated startup behavior — `TermsConsentStartupGateTests` and the full App suite.

The optional real-desktop journey is not warranted for this module: the first-run decision boundary, focusable controls, provider-link commands, save failure, and visual states are covered deterministically, while native browser behavior is intentionally delegated to the operating system.

## Release gate

Task 1.1 remains intentionally open: the bundled FusionCanvas terms are marked `draft-0.1` and must receive human/legal approval before release as binding terms. This is the only remaining product/legal gate; the technical implementation and verification tasks are complete.
