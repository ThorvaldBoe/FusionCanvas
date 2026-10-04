# Verification: app-wide-terms-consent

## Evidence status

The consent contract, application-settings persistence, startup gate, consent UI, and Settings review surface are implemented. The architecture and UI audit corrections are implemented: acceptance persistence is owned by `TermsConsentService`, the startup decision is isolated behind `TermsConsentStartupCoordinator`, and the consent surface uses semantic design tokens. Release verification remains incomplete: Task 1.1 requires human/legal approval, Tasks 6.1-6.4 still require their criterion-level evidence to be formally recorded, and Task 6.6 is blocked by an existing unrelated headless Store Editor regression on the current main line.

## Acceptance criteria

| Criterion | Evidence | Result |
| --- | --- | --- |
| First launch gates normal workspace | `TermsConsentStartupCoordinator` requires a consent decision before `App.InitializeStartupAsync` calls normal `AppServicesFactory`/`MainWindow` composition. | Implemented; startup coordinator tests cover missing, accepted, and declined decisions. |
| Current acknowledgement continues startup | `TermsConsentPolicy.IsCurrent` is checked before showing the consent surface. | Implemented; startup coordinator test confirms the current record bypasses the consent surface. |
| Decline/close quits without workspace composition | A declined consent decision returns `null`; `App` shuts down before constructing services. | Implemented; startup coordinator test confirms the composition-blocking result. |
| Four responsibilities are separate | `TermsConsentWindow.axaml` contains four individually bound `CheckBox` controls and official Printify/Shopify links. | Implemented; rendered headless test activates all four controls. |
| Primary action is gated | `TermsConsentViewModel.CanAgree` requires all four selections and the XAML binds `IsEnabled`. | Implemented; focused App/headless tests added. |
| Provider acknowledgement is honest | Consent copy states provider terms remain the user's responsibility and that FusionCanvas does not accept agreements on the user's behalf. | Implemented by XAML and bundled policy copy. |
| Consent is minimal and versioned | `TermsConsentRecord` stores only terms version, acknowledgement-policy version, and UTC timestamp. | Implemented; integration round-trip/malformed/missing tests added. |
| Missing/stale consent is required | `TermsConsentPolicy.IsCurrent` rejects missing or mismatched versions. | Implemented; application tests added. |
| Save failure retains the form | `TermsConsentViewModel` keeps selections, reports the save warning, and raises `Accepted` only after success. | Implemented; focused App test passed. |
| Offline FusionCanvas policy remains readable | `FusionCanvasTermsOfUse.md` is an Avalonia resource loaded locally; provider links are explicitly external. | Implemented. |
| Settings reviews current state | `SettingsSection.Terms`, `TermsConsentSummary`, and the review route use `SettingsViewModel.CreateTermsConsentViewModel`, preserving one persistence owner. | Implemented; Settings tests added. |
| Existing layout behavior remains after consent | Normal startup still creates the existing `MainWindow` and passes the loaded settings through `AppServicesFactory`. | Implemented; full baseline evidence remains outstanding. |

## Commands

- `openspec validate app-wide-terms-consent --strict` — passed.
- `dotnet build .\src\FusionCanvas.Application\FusionCanvas.Application.csproj --no-restore` — passed.
- `dotnet test .\tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj --no-restore -m:1 --filter FullyQualifiedName~TermsConsent` — passed: 2 service tests; existing analyzer warnings remain.
- `dotnet test .\tests\FusionCanvas.Integration.Tests\FusionCanvas.Integration.Tests.csproj --no-restore -m:1 --filter FullyQualifiedName~JsonApplicationSettingsStore` — passed: 30 persistence tests; existing analyzer warnings remain.
- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore -m:1 --filter FullyQualifiedName~TermsConsent` — passed: 16 consent/startup/headless tests.
- `dotnet test .\FusionCanvas.sln -m:1` — pending release verification; the current main line has one known unrelated failure in `StoreEditorHeadlessTests.MockupSourceRow_SelectsFromCellsWhitespaceAndKeyboard_WithoutArchiving`, so this change does not claim a clean full baseline.

The optional real-desktop journey is not warranted for this module: the first-run decision boundary, focusable controls, provider-link commands, save failure, and visual states are covered deterministically, while native browser behavior is intentionally delegated to the operating system.

## Release gate

Task 1.1 remains intentionally open: the bundled FusionCanvas terms are marked `draft-0.1` and must receive human/legal approval before release as binding terms.

## Audit correction evidence

Audit finding #803 is addressed by the startup coordinator and rendered consent coverage:

| Finding criterion | Evidence | Result |
| --- | --- | --- |
| Missing, accepted, and declined startup decisions are covered | `TermsConsentStartupCoordinatorTests` covers the three decision paths and verifies whether the composition result is `null` or accepted settings. | Pass |
| Rendered checkbox interaction is covered | `TermsConsentWindowTests.Window_ShowsOfflinePolicyFourCheckboxesAndGatedActions` selects each rendered checkbox through headless pointer input and verifies the gated action becomes enabled. | Pass |
| Keyboard-reachable quit and visible save/error states are covered | `TermsConsentWindowTests` covers keyboard activation of Quit, the visible saving progress bar while persistence is blocked, and the visible persistence error while selections remain checked. | Pass |

Task 6.2 remains open until every acceptance scenario is reconciled against the final approved policy and criterion-level evidence. Task 6.6 remains open until the unrelated Store Editor regression is resolved or separately dispositioned.
