# Verification

## Acceptance criteria

| Criterion | Result | Evidence / limitation |
| --- | --- | --- |
| Active Niche opens a focused import dialog with Store/shop context | PARTIAL | Niche-only menu eligibility and the dialog's product selection are covered by headless tests; the Appium journey is compiled but not run here. |
| Missing credential or shop gives setup guidance and makes no product request | PASS | `PrintifyListingImportServiceTests.Missing_credential_stops_preview_before_printify_request`; missing setup routes to Store setup from the dialog. |
| Preview retrieves every paginated product and allows individual selection | PASS | `PrintifyListingImportClientTests.Shop_listing_discovery_follows_pages_and_uses_only_get_requests`; headless dialog selection test. |
| Linked products are hidden by default, can be shown, and remain ineligible | PASS | Application preview test plus `PrintifyListingImportWindowTests` verifies linked checkbox is disabled. |
| Empty shop and retrieval error are distinct and retryable without local mutation | PARTIAL | Empty shop and missing-key states are covered by headless tests; the HTTP retrieval error and retry path is implemented but lacks a focused test. |
| Duplicate check runs only for selected products and searches active Items across the Store | PASS | `PrintifyListingImportServiceTests` covers cross-Niche matching and empty selection rejection. |
| Close title/description matches are advisory and identify candidate context | PASS | Application tests cover near-title and description matches, unrelated products, and cross-Niche candidate context; headless test verifies candidate context and connect selection. |
| User can choose new Item or connect to an unlinked candidate | PASS | Application tests cover new import and connecting a duplicate. |
| Connecting preserves local title, description, stage, status, and topic | PASS | Connect test asserts the complete existing Item record remains unchanged. |
| Already linked candidate is non-linkable; mapped remote product cannot be imported again | PASS | Application tests assert non-linkability and that retrying an already imported product cannot create a second Item. |
| New visible product creates a Published Listing-stage Item with Printify title/description | PASS | Application import persistence test. |
| New hidden product creates a Draft Listing-stage Item | PASS | Application import persistence test. |
| Product snapshot, identity, artwork, and provenance persist as Item-linked data | PASS | Application test verifies Item asset links and external mapping; signed query parameters are removed. |
| Initial import leaves Store catalog and Design-slot records unchanged | PASS | Application test asserts the initial import does not create Blueprint or Design assignment records. |
| Listing-stage action downloads/reuses the linked product's catalog and initializes Item configuration, colors, rows, and representable assignments | PASS | Variant setup Application test verifies configuration, selected color, row, and artwork assignment. |
| Repeating variant-setup download does not duplicate catalog records or Item rows | PASS | Same Application test executes setup twice and asserts singular configuration/row. |
| Variant-setup download preserves local Item fields and mapping and reports retryable failures without partial Item configuration | PARTIAL | Local Item fields, mapping preservation, catalog failure, and offering conflict are tested; local repository save failure is not separately asserted. |
| One finished image per print area/variant maps to a Design slot and variants with matching artwork share rows | PASS | Variant setup test verifies the image assignment and both color variants grouped into one shared row. |
| Multi-layer or text artwork remains preserved as Item assets, leaves affected slots empty, and is reported | PASS | Unsupported-layer test imports the source asset, then verifies partial setup and an empty affected slot. |
| Imported artwork uses FusionCanvas's standard placement; Printify-specific transforms are ignored | PASS | Import/setup assertions verify the locally linked image metadata contains no Printify scale or rotation values; transforms are absent from the import DTO. |
| Dated Printify group is unique and only contains newly created Items | PASS | Application test covers collision suffix and connecting to an existing Item without group placement. |
| Product failures are isolated; successful products remain committed; retry imports only failures | PASS | Application tests cover mixed success/failure, duplicate-safe retry, and staged-file cleanup on persistence failure. |
| Cancellation before confirmation makes no local changes; in-progress cancellation stops between products | PASS | Application tests cover pre-cancel with no mutation and cancellation after one committed product, preserving that success and stopping before the next. |
| Import performs no remote mutation and rejects unsafe artwork payloads | PASS | Integration tests assert GET-only shop reads and reject unsafe URLs and malformed image signatures. |
| Native user journey completes from Niche context menu through import and optional variant setup download | NOT RUN | Appium scenario exists and compiles. Port 4723 was not listening, so the real-desktop journey could not run in this environment. |
| Change artifacts and accepted scenarios validate strictly | PASS | `openspec validate import-printify-listings --type change --strict`. |
| Solution baseline passes | PASS | `dotnet test .\FusionCanvas.sln --no-restore` (current run results below). |

## Test runs

- `dotnet build .\tests\FusionCanvas.UITests\FusionCanvas.UITests.csproj --no-restore` — passed; the Appium scenario compiles. Existing Avalonia version conflict warnings remain.
- `dotnet test .\FusionCanvas.sln --no-restore` — passed: UiDescription 29, Domain 294, Application 684, Integration 356, and App 1,014 tests (2,377 total).
- `openspec validate import-printify-listings --type change --strict` — passed.
- Appium journey — not run because no server was listening at `127.0.0.1:4723`.

Headless UI coverage verifies Niche-only menu eligibility; import-window construction, linked-product disabling, keyboard tab-stop, selection and connect-candidate binding; empty-shop and missing-credential states; mixed success and failure outcomes; a cancellable loading state; and Listing-stage variant-setup action availability. Retrieval retry and local save failure remain partial test-coverage gaps.

The Appium scenario uses a fresh disposable database/workspace and a fake listing/catalog client gated by the UI-test launch arguments. The fake credential is synthetic and never reaches an external service.
