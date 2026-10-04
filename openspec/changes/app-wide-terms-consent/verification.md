# Verification: app-wide-terms-consent

## Evidence status

The implementation is complete for the consent contract, application-settings persistence, startup gate, consent UI, and Settings review surface. Deterministic test execution is currently blocked by unrelated dirty-worktree changes in `src/FusionCanvas.Integration/Persistence/SqliteWorkspaceRepository.cs`, which reference missing `InsertContentRiskReviewAsync` and `LoadContentRiskReviewsAsync` methods. The affected Integration/App projects cannot compile until that pre-existing work is completed or reverted by its owner.

## Acceptance criteria

| Criterion | Evidence | Result |
| --- | --- | --- |
| First launch gates normal workspace | `App.InitializeStartupAsync` loads settings, shows `TermsConsentWindow`, and calls normal `AppServicesFactory`/`MainWindow` composition only after accepted settings are saved. | Implemented; test execution blocked by unrelated Integration compile errors. |
| Current acknowledgement continues startup | `TermsConsentPolicy.IsCurrent` is checked before showing the consent surface. | Implemented; test execution blocked by unrelated Integration compile errors. |
| Decline/close quits without workspace composition | `TermsConsentViewModel.QuitRequested` completes startup with no settings mutation; `App` shuts down before constructing services. | Implemented; test execution blocked by unrelated Integration compile errors. |
| Four responsibilities are separate | `TermsConsentWindow.axaml` contains four individually bound `CheckBox` controls and official Printify/Shopify links. | Implemented; headless test added, execution blocked by unrelated Integration compile errors. |
| Primary action is gated | `TermsConsentViewModel.CanAgree` requires all four selections and the XAML binds `IsEnabled`. | Implemented; focused App/headless tests added. |
| Provider acknowledgement is honest | Consent copy states provider terms remain the user's responsibility and that FusionCanvas does not accept agreements on the user's behalf. | Implemented by XAML and bundled policy copy. |
| Consent is minimal and versioned | `TermsConsentRecord` stores only terms version, acknowledgement-policy version, and UTC timestamp. | Implemented; integration round-trip/malformed/missing tests added. |
| Missing/stale consent is required | `TermsConsentPolicy.IsCurrent` rejects missing or mismatched versions. | Implemented; application tests added. |
| Save failure retains the form | `TermsConsentViewModel` keeps selections, reports the save warning, and raises `Accepted` only after success. | Implemented; focused App test added. |
| Offline FusionCanvas policy remains readable | `FusionCanvasTermsOfUse.md` is an Avalonia resource loaded locally; provider links are explicitly external. | Implemented. |
| Settings reviews current state | `SettingsSection.Terms`, `TermsConsentSummary`, and the review route use `SettingsViewModel.CreateTermsConsentViewModel`, preserving one persistence owner. | Implemented; Settings tests added. |
| Existing layout behavior remains after consent | Normal startup still creates the existing `MainWindow` and passes the loaded settings through `AppServicesFactory`. | Implemented; full regression execution blocked by unrelated Integration compile errors. |

## Commands

- `openspec validate app-wide-terms-consent --strict` — passed before implementation.
- `dotnet build .\src\FusionCanvas.Application\FusionCanvas.Application.csproj --no-restore` — passed.
- `dotnet build .\src\FusionCanvas.App\FusionCanvas.App.csproj --no-restore` — passed before the unrelated Integration source was recompiled; current rebuild is blocked by the two missing content-risk methods described above.
- `dotnet build .\src\FusionCanvas.App\FusionCanvas.App.csproj --no-restore --no-dependencies` — passed, compiling the changed App project against the existing referenced binaries.
- `dotnet build .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore --no-dependencies` — passed with existing repository warnings.
- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-build --no-restore -m:1 --filter FullyQualifiedName~TermsConsent` — passed: 10 tests.
- `dotnet build .\tests\FusionCanvas.Integration.Tests\FusionCanvas.Integration.Tests.csproj --no-restore --no-dependencies` — blocked by unrelated `ContentRiskPersistenceTests.cs` references to a missing `TemporaryDirectory` type.
- `dotnet test .\FusionCanvas.sln -m:1` — pending the unrelated Integration compile blocker.

The optional real-desktop journey is not warranted for this module: the first-run decision boundary, focusable controls, provider-link commands, save failure, and visual states are covered deterministically, while native browser behavior is intentionally delegated to the operating system.

## Release gate

Task 1.1 remains intentionally open: the bundled FusionCanvas terms are marked `draft-0.1` and must receive human/legal approval before release as binding terms.
