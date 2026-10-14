# Verification evidence

## Deterministic verification

| Check | Result | Evidence |
| --- | --- | --- |
| Strict OpenSpec validation | Passed | `openspec validate manual-api-verification --strict` — “Change is valid”. |
| Solution build | Passed | `dotnet build .\FusionCanvas.sln --no-restore -m:1` — exit 0, 0 errors. Existing ImageSharp vulnerability advisories and NuGet vulnerability-feed connectivity warnings were reported. |
| Offline list/plan commands | Passed | `dotnet run --no-build --project .\src\FusionCanvas.ApiVerification -- list` and provider-specific `plan` commands ran without credentials and made no provider calls. |
| Full solution re-run | Passed | After configuring `HeadlessTestApp` directly with Avalonia's headless platform, `dotnet build .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore -m:1` exited 0 and `dotnet test .\FusionCanvas.sln -m:1 --no-restore --no-build` passed Domain 294, Application 700, Integration 434, App 1023, and UiDescription 29; total 2480 passed, 0 failed, 0 skipped. |
| Live OpenRouter / Printify calls | Partially passed; findings retained below | Controlled calls were run on 2026-10-14. OpenRouter reads and representative text/image endpoints passed; the Printify lifecycle passed. The full OpenRouter endpoint/model matrix remains larger than one $2 session. |

## Acceptance scenario mapping

| Acceptance area | Deterministic evidence or live status |
| --- | --- |
| Network-free list and plan | `ApiVerificationCommand` and command tests cover list/plan selection and no-network behavior. CLI list and provider-specific plan commands were also run offline. |
| Explicit run, credentials, target, confirmation, cancellation | Fail-closed setup, missing credentials, cancellation before provider access, and exact Printify DevTest shop matching are covered by orchestration tests. OpenRouter key validation passed and the Printify lifecycle resolved the exact `FusionCanvas DevTest` shop. |
| No live work in solution baseline | `FusionCanvas.ApiVerification` is not in `FusionCanvas.sln`; it is referenced only by the offline integration-test project. The complete solution baseline passed without credentials or provider traffic. |
| Operation inventory | `ApiOperationInventory` maps production OpenRouter and Printify operations to registered scenarios. Tests check scenario registration and inventory parity. Each report records uncovered operations as `not-run`; unsupported operations are not counted as passed. |
| Findings and production fix loop | Structured findings identify the operation, expected/actual concern, sanitized diagnostic, and production location. Live runs exposed an OpenRouter endpoint capability mismatch and transient 429 responses; details and safe parameter-level evidence are below. |
| OpenRouter model qualification | Production catalogs and endpoint-specific parameters build a bounded profile for each priced provider route. Deterministic tests cover per-endpoint parameter intersections, token bounds, normalized evidence fingerprints, and safe error diagnostics. Live route results are below; full model coverage needs multiple sessions. |
| Qualification cost control | Conservative endpoint-level bounds and per-generation confirmation were exercised live. The fresh all-model/all-endpoint preflight found 1,330 priced routes at a $12.099899 conservative maximum, so qualification is split under the configured $2 per-session cap. |
| Printify DevTest coverage | The latest live lifecycle report passed all 13 exercised operations in `FusionCanvas DevTest`. Six non-lifecycle operations were not-run in that report and are covered by earlier read-only/catalog/import reports. No order path was called. |
| Printify recovery | Recovery checks the versioned journal, exact DevTest shop identity and run-owned product; a present report must corroborate the journal. It refuses published products and verifies deletion. Deterministic ownership and eligibility tests pass; recovery has not been exercised live. |
| Reports and evidence | Writer and store tests cover JSON/Markdown report generation, sanitization/redaction, coverage status, fingerprinted qualification evidence and owned-resource journaling. |

## Inventoried production operations

All entries in `src/FusionCanvas.ApiVerification/Inventory/ApiOperationInventory.cs` have provider-specific scenario registration or an explicit restricted status. Read-only operations run only when their scenario is selected. OpenRouter text/image endpoint discovery requires selected model IDs. Printify artwork download is reported as `not-run` if the selected shop has no product with artwork. Publish and unpublish require a confirmed product lifecycle run. The Printify product lifecycle invokes the application `ListingLifecycleService` and the production Integration client; artwork upload is a separately confirmed prerequisite and is then represented in the local test workspace for lifecycle orchestration.

## Remaining manual evidence

- Full per-model/per-endpoint qualification remains an on-demand campaign. The fresh all-route conservative maximum is $12.099899; each session remains capped at $2, so coverage needs multiple sessions.
- DeepInfra's `min_p` mismatch and the two rate-limited `deepseek/deepseek-v4-flash` endpoints remain open with live evidence; see the table below. Other individual model/provider routes have also returned rate limits, permission denials, or provider errors while their alternate routes succeeded.
- Printify create/update/publish/unpublish/delete and artwork upload are available only through the manually confirmed DevTest workflow. No order or chargeable operation is available in the runner.
- Existing restore/build warnings mention unavailable NuGet vulnerability metadata and known ImageSharp advisories; these did not fail compilation or tests.

## Live verification (2026-10-14)

| Run | Result | Evidence |
| --- | --- | --- |
| OpenRouter read-only coverage | Passed | `2732241e48d44663a2eb715c1708d737`: key validation; 381 text models; 55 image models; both ZDR catalog paths; 16 text endpoints for `deepseek/deepseek-v4-flash`; 2 text and 2 image endpoints for `google/gemini-3-pro-image-preview`. |
| OpenRouter text endpoint matrix | Partial | `c483bd02ad3d47ea9a32935048e42c59`: 16 priced endpoints for `deepseek/deepseek-v4-flash`; 14 passed; DeepInfra and Alibaba were rate-limited (429). Conservative maximum $0.004625; actual reported cost $0.000166. The same catalog preflight blocked `qwen/qwen3-coder-30b-a3b-instruct` / `amazon-bedrock` because no positive completion-token bound was advertised; no generation was sent to that route. |
| OpenRouter image endpoint matrix | Passed | `3dd479351cf44a2ab869874d8191da9e`: both text and both image endpoints for `google/gemini-3-pro-image-preview` passed. Conservative maximum $0.26363625; actual reported cost $0.13707. Generated images were discarded. |
| OpenRouter endpoint parameter discrepancy | Provider limitation remains open | `28e3e19d92d843bd81e417a4f026b6dd`: GMICloud and SiliconFlow passed for `deepseek/deepseek-v4-flash-vision-exp`; DeepInfra returned an HTTP-200 error envelope (`code=400`, `metadata.error_type=invalid_request`). Parameter-isolation probes found that `min_p=0.05` alone caused the error, while baseline and each other advertised parameter passed individually. The full profile passed when only `min_p` was omitted. Actual reported cost $0.00018436. |
| OpenRouter evidence reuse | Verified | `2d5423aff74c4a2fa9eaae8b67697b31`: a fresh preflight reused GMICloud and SiliconFlow evidence at zero estimated cost; DeepInfra remained eligible for retry. Qualification was declined after preflight, so no generation was repeated. |
| Printify product lifecycle | Passed | `20261014-110848-Printify-printify-product-lifecycle-1897609133bf4c19a74ae3d4c3086250`: 13 lifecycle operations passed on shop `FusionCanvas DevTest`; six operations outside that lifecycle were not-run. The run created/read/updated/published/unpublished/deleted a test product and did not create an order. Earlier reports cover catalog/product/artwork reads and import. |
| Previous all-model OpenRouter sweep | Partial | `20261014-155501-OpenRouter-openrouter-qualification-b44aafe4952248b9948695edb6265df7`: 311 passed, 22 failed, 17 rate-limited, 86 budget-deferred, 5 not-run; estimated maximum $1.680586, actual $0.28205591539. It tested one endpoint per model. The newer all-route preflight enumerated 436 model/modality pairs and 1,330 priced endpoint routes at $12.099899 aggregate, so complete endpoint qualification cannot fit one $2 session. |
| OpenRouter multi-endpoint/model profile batch | Partial | `20261014-192932-OpenRouter-openrouter-qualification-f608e4ff3e924eaeadec9b53d792454b`: 18 model/modality pairs, with 17 passed, 4 reused, 3 rate-limited, and 2 provider permission denials; estimated selected-route maximum $0.042042, actual reported cost $0.00051408. The preflight's new-call maximum was $1.687042. Text profiles confirmed endpoint-specific token field selection (`max_completion_tokens` on Azure, `max_tokens` on Anthropic and Google) and successful Mistral/Liquid parameter profiles. Google AI Studio and Poolside free routes were rate-limited; both Thinking Machines routes returned 403. These denials indicate route access/capacity restrictions, not evidence that the serialized parameters are invalid. |

The OpenRouter findings are provider-specific: the supported-parameter list for the tested DeepInfra route disagrees with its request validator, while the same model profile passed on the other two advertised providers. The additional selected routes confirmed endpoint-specific token field selection. Rate-limited or permission-restricted routes and the remaining model/endpoint pairs still need later bounded runs; do not treat those provider restrictions as a general model-profile failure.

Harness changes made during verification include testing each priced endpoint separately, enforcing positive joint model/endpoint token bounds, continuing past malformed catalog profiles, recognizing 2xx provider error envelopes, sanitizing error diagnostics, and excluding volatile latency/throughput from evidence fingerprints.

Existing restore/build warnings mention unavailable NuGet vulnerability metadata, xUnit analyzer warnings, and ImageSharp advisories; they did not fail the integration suite.
