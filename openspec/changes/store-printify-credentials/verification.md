# Verification — store-printify-credentials

Status: implementation verified locally. Live Printify access and contributor credential stores are excluded from routine verification. The provider token is account-wide, while the native copy and selected shop remain Store-scoped by design.

## Acceptance scenarios

| Capability / scenario | Planned method | Result / evidence |
| --- | --- | --- |
| strategy / User configures an existing Store | Domain/Application strategy cases, isolated SQLite round trips, Store VM selection | Pass — 247 Domain, 419 Application, and 620 App tests |
| strategy / User operates the editor by keyboard | StoreEditorHeadlessTests selection and save interaction | Pass — App headless suite |
| strategy / User selects a strategy without Printify | VM visibility and Application/native/HTTP call counters for both strategies | Pass — focused Application/App tests |
| strategy / User saves incomplete Printify configuration | StoreManagementServiceTests and VM tests with missing key/offline collaborators | Pass — focused Application/App tests |
| strategy / Enabled strategy is changed | Store Save confirmation tests; snapshot identity/catalog and fake native-entry comparison | Pass — focused App tests |
| strategy / User cancels a strategy warning | VM declined confirmation, unchanged persisted data and retained draft fields | Pass — focused App tests |
| strategy / User returns to Printify configuration | VM selection lifecycle with retained native entry, no HTTP calls | Pass — focused App tests |
| credentials / Store has no saved key | VM and headless labels, semantic error state, Add visible, Verify hidden | Pass — focused App tests |
| credentials / Store has a saved key | VM and headless Manage/Verify visibility; separate verification status | Pass — focused App tests |
| credentials / Credential storage cannot be read | Native failure mapping plus VM/headless Retry and overwrite guards | Pass — Integration/App tests |
| credentials / User configures a new Store draft | VM/headless save-first guidance; zero native calls | Pass — focused App tests |
| credentials / Archived Store is selected | Application guard and VM/headless disabled actions; retained entry | Pass — Application/App tests |
| credentials / User saves keys for two Stores | Native adapter scope isolation, reload/rename with stable IDs; Application verification key capture | Pass — Integration/Application tests |
| credentials / Workspace is exported or copied to another machine | Transfer/SQLite/settings isolation checks using synthetic sentinel token; new empty backend fixture | Pass — secret boundary inspection and Application tests |
| credentials / Native credential write fails | Adapter replacement failure and VM draft retry tests; inspect absence of fallback writes | Pass — Integration/App tests |
| credentials / Store is deleted and another is created | Service rejects deleted scope; distinct new IDs cannot retrieve retained fake native entry | Pass — Application/Integration tests |
| credentials / User adds or replaces a key | Application and dialog VM offline save, captured identity, cleared draft and verification | Pass — Application/App tests |
| credentials / User enters an unusable value | Parameterized validation tests for empty/whitespace/control characters | Pass — Application tests |
| credentials / User cancels or dismisses a draft | Dialog VM and headless Cancel/Escape/close, accept/decline discard | Pass — App headless tests |
| credentials / User operates the dialog by keyboard | Headless focus-on-open, tab reachability, focus return | Pass — App headless tests |
| credentials / Save is in progress | Controlled native-save task; duplicate/dismiss/edit guards and captured identity | Pass — App tests |
| credentials / Key verifies successfully | Fake HTTP 200 arrays including empty; saved token/request headers; VM status | Pass — Integration tests; shop ID/title parsing covered |
| credentials / User selects and persists a Printify shop | StoreContext round-trip and Store Editor binding tests | Pass — Application persistence test and App headless suite |
| credentials / Previously selected shop is absent | Credential view-model stale-selection test | Pass — selected ID is cleared and guidance is shown |
| credentials / Printify strategy selection is unsaved | Application persisted-strategy guard and headless disabled Verify/guidance | Pass — Application/App tests |
| credentials / Printify rejects authentication or permissions | Fake HTTP 401/403; safe distinct results; token sentinel absent from messages | Pass — Integration tests |
| credentials / Verification cannot complete | Fake HTTP 429/5xx/unexpected/malformed/oversized responses, network/timeout; retry and key retention | Pass — Integration tests |
| credentials / Context changes during verification | Controllable late completions after Store/workspace/strategy/key change and close | Pass — App tests |
| credentials / Verification is already running | VM/headless busy state, duplicate guard, navigation cancellation | Pass — App tests |

## Required completion gates

| Gate | Result / evidence |
| --- | --- |
| Delivery-package approval | Pass — user explicitly authorized implementation and requested the account-level correction |
| `dotnet build .\FusionCanvas.sln` | Pass with `-p:UsedAvaloniaProducts=` because Avalonia telemetry cannot write its protected local log in this environment |
| `dotnet test .\FusionCanvas.sln` | Pass equivalent baseline with `--no-build -p:UsedAvaloniaProducts=`: 1,521 tests passed |
| `openspec validate store-printify-credentials --strict` | Pass — change is valid after shop-selection updates |
| Scoped architecture, security, persistence, UI and drift review | Pass — changed layers reviewed; no secret fallback or provider response persistence |
| Retrospective and archive | Pending implementation and acceptance |

No implementation acceptance is inferred from OpenSpec artifact completeness. Optional native smoke or live desktop observations must use disposable identities/workspaces and are supplemental.
