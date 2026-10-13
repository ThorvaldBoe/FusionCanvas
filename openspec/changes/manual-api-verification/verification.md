# Verification evidence

## Deterministic verification

| Check | Result | Evidence |
| --- | --- | --- |
| Strict OpenSpec validation | Passed | `openspec validate manual-api-verification --strict` — “Change is valid”. |
| Solution build | Passed | `dotnet build .\FusionCanvas.sln --no-restore -m:1` — exit 0, 0 errors. Existing ImageSharp vulnerability advisories and NuGet vulnerability-feed connectivity warnings were reported. |
| Offline list/plan commands | Passed | `dotnet run --no-build --project .\src\FusionCanvas.ApiVerification -- list` and provider-specific `plan` commands ran without credentials and made no provider calls. |
| Full solution baseline | Passed | `dotnet test .\FusionCanvas.sln -m:1` — exit 0; Domain 294, Application 688, Integration 382, App 1020, UiDescription 29; total 2413 passed, 0 failed, 0 skipped. The repository enables Microsoft.Testing.Platform for `dotnet test` because VSTest testhost startup stalled in this environment. |
| Live OpenRouter / Printify calls | Deliberately not run | Runs remain manual and controlled. No billable model generation or Printify write was initiated during implementation. |

## Acceptance scenario mapping

| Acceptance area | Deterministic evidence or live status |
| --- | --- |
| Network-free list and plan | `ApiVerificationCommand` and command tests cover list/plan selection and no-network behavior. CLI list and provider-specific plan commands were also run offline. |
| Explicit run, credentials, target, confirmation, cancellation | Fail-closed setup, missing credentials, cancellation before provider access, and exact Printify DevTest shop matching are covered by orchestration tests. Live identity remains pending until a user starts a session. |
| No live work in solution baseline | `FusionCanvas.ApiVerification` is not in `FusionCanvas.sln`; it is referenced only by the offline integration-test project. The complete solution baseline passed without credentials or provider traffic. |
| Operation inventory | `ApiOperationInventory` maps production OpenRouter and Printify operations to registered scenarios. Tests check scenario registration and inventory parity. Each report records uncovered operations as `not-run`; unsupported operations are not counted as passed. |
| Findings and production fix loop | Structured findings identify the operation, expected/actual concern, sanitized diagnostic, and production location. No provider defect was observed because no live session was started. Any defect from the first controlled sessions still requires a production correction or evidence-backed limitation, deterministic regression coverage when reproducible, and a targeted live rerun. |
| OpenRouter model qualification | Production catalogs and `AiParameterRegistry` build per-model effective profiles. Deterministic tests cover profile filtering, capability fingerprints, qualification evidence matching/reuse, and invalid/unknown model selection. Live outcomes for any model remain pending. |
| Qualification cost control | Text/image cost bounds, request limits, over-budget/unknown-price blocks, and qualification matrix behavior are covered by deterministic tests. Each model generation has a separate confirmation after endpoint/profile and conservative maximum are shown. Live provider billing behavior remains unverified. |
| Printify DevTest coverage | Shop/catalog/import/artwork and product lifecycle scenarios are inventoried. Product upload, create, update, publish/unpublish, and delete use production paths with read-back verification and an individual typed confirmation per write. Tests cover target mismatch, cancellation, publication and recovery ownership. Live endpoint behavior remains unverified. |
| Printify recovery | Recovery checks the versioned journal, exact DevTest shop identity and run-owned product; a present report must corroborate the journal. It refuses published products and verifies deletion. Deterministic ownership and eligibility tests pass; recovery has not been exercised live. |
| Reports and evidence | Writer and store tests cover JSON/Markdown report generation, sanitization/redaction, coverage status, fingerprinted qualification evidence and owned-resource journaling. |

## Inventoried production operations

All entries in `src/FusionCanvas.ApiVerification/Inventory/ApiOperationInventory.cs` have provider-specific scenario registration or an explicit restricted status. Read-only operations run only when their scenario is selected. OpenRouter text/image endpoint discovery requires selected model IDs. Printify artwork download is reported as `not-run` if the selected shop has no product with artwork. Publish and unpublish require a confirmed product lifecycle run. The Printify product lifecycle invokes the application `ListingLifecycleService` and the production Integration client; artwork upload is a separately confirmed prerequisite and is then represented in the local test workspace for lifecycle orchestration.

## Remaining manual evidence

- The first controlled OpenRouter and Printify sessions must be started by the user with dedicated credentials. Findings from those sessions are intentionally not fabricated; each actual discrepancy must be fixed and regression-tested before its affected scenario is rerun.
- Full per-model qualification remains a separate on-demand campaign. It can incur provider cost up to the configured hard per-session ceiling, and its findings will be applied to production services before a failed profile is considered resolved.
- Printify create/update/publish/unpublish/delete and artwork upload are available only through the manually confirmed DevTest workflow. No order or chargeable operation is available in the runner.
- Existing restore/build warnings mention unavailable NuGet vulnerability metadata and known ImageSharp advisories; these did not fail compilation or tests.
