## Context

FusionCanvas has local fake-handler tests for production OpenRouter and Printify adapters, but no live lane that finds provider mismatches and drives corrections back into those adapters. The intended outcome is not a report alone: a discrepancy in types, headers, parameters, serialization, or response handling must identify the affected production operation, result in a service correction and deterministic regression test, and then be rechecked live.

Current application call inventory (to be verified against source and kept in a versioned inventory):

| Provider | Application operation / endpoint | Notes |
| --- | --- | --- |
| OpenRouter | `GET /api/v1/key` | Credential validation |
| OpenRouter | `GET /api/v1/models` | Text model catalog and model pricing/capability metadata |
| OpenRouter | `GET /api/v1/models/{model}/endpoints` | Text endpoint catalog |
| OpenRouter | `GET /api/v1/endpoints/zdr` | Zero-data-retention endpoint list |
| OpenRouter | `GET /api/v1/images/models` | Image model catalog |
| OpenRouter | `GET /api/v1/images/models/{model}/endpoints` | Image endpoint catalog |
| OpenRouter | `POST /api/v1/chat/completions` | Text generation with effective model profile and routing |
| OpenRouter | `POST /api/v1/images` | Image generation |
| Printify | `GET /v1/shops.json` | Credential and selected-shop verification |
| Printify | `GET /v1/blueprints.json`, `/blueprints/{id}.json` | Catalog retrieval |
| Printify | `GET /v1/blueprints/{id}/print_providers.json`, `/print_providers/{id}/variants.json` | Provider and variant dependency retrieval |
| Printify | `GET /v1/shops/{shop}/products.json`, `/products/{id}.json` | Listing/import/catalog product inventory and detail |
| Printify | `GET` provider-supplied artwork URL | Artwork download path used by listing import; host validation is in the production adapter |
| Printify | `POST /v1/uploads/images.json` | Artwork upload used by lifecycle |
| Printify | `POST /v1/shops/{shop}/products.json` | Product creation |
| Printify | `PUT /v1/shops/{shop}/products/{id}.json` | Product update |
| Printify | `DELETE /v1/shops/{shop}/products/{id}.json` | Product deletion |
| Printify | `POST /v1/shops/{shop}/products/{id}/publish.json` and `/unpublish.json` | Publication operations |

The operation inventory is a starting source map, not a substitute for confirming method, query, headers, body, and response mappings in source. The session tool should be provider-organized but cover every application-used operation through live scenarios, with explicit restrictions where a call is not safe/cost-bounded.

FusionCanvas already has `OpenRouterClient`, `PrintifyCatalogClient`, `PrintifyListingImportClient`, `PrintifyCredentialVerifier`, `PrintifyListingClient`, and `ListingLifecycleService`. OpenRouter profiles support dynamic per-model capabilities including token limits, sampling parameters, stop sequences, and reasoning. `AiParameterRegistry` filters and validates effective profiles from model metadata. Qualification must test whether that metadata/filtering/request mapping works against real model behavior, not only whether one generic model accepts a completion.

## Goals / Non-Goals

**Goals:**

- One opt-in manual CLI project for all providers, with provider-specific suites and complete operation inventory.
- Exercise actual Application/Integration behavior for every currently used API operation, not parallel handcrafted HTTP requests.
- Build an on-demand, resumable per-model OpenRouter qualification lane across text and image capabilities and the effective parameters the app sends.
- Keep a hard cumulative cost ceiling for every OpenRouter run; routine smoke sessions remain small and do not automatically run the model matrix.
- Test all non-cost-incurring Printify operations against the exact FusionCanvas DevTest store, including create/update/publish/unpublish/delete where applicable; never submit orders or trigger chargeable work.
- Turn every live defect into a production service fix, deterministic regression test, targeted live rerun, and resolved or explicitly restricted finding.
- Make cleanup best-effort and journaled; an orphaned DevTest resource is visible and recoverable but does not invalidate unrelated scenario results.
- Keep the regular solution test command and CI offline and deterministic.

**Non-Goals:**

- Shopify, order submission, any cost-incurring Printify operation, production-store writes, scheduled/unattended sessions, or UI entry points.
- Automatically testing every imaginable parameter combination. Qualification covers the effective profile configurations and operation variants the application actually sends, keyed to real catalog capabilities.
- Treating provider model metadata, mocks, or a successful generic API request as proof every model-specific behavior is correct.

## Decisions

1. **Use one standalone `FusionCanvas.ApiVerification` CLI project with provider-scoped scenario modules and a versioned operation inventory.** Shared preflight, consent, cost accounting, reports, and findings belong in one place. Keep the executable outside `FusionCanvas.sln` and CI. This provides explicit/manual execution and reliable confirmation while preventing accidental baseline runs.

2. **Call production application services and adapters.** OpenRouter uses `OpenRouterClient` and the actual model/profile/request-resolution behavior used by generation. Printify uses the actual `PrintifyCatalogClient`, `PrintifyListingImportClient`, `PrintifyCredentialVerifier`, `PrintifyListingClient`, and `ListingLifecycleService` paths. A generic curl/HTTP test can diagnose a failure but cannot mark an application operation live-verified.

3. **Make operation coverage reviewable.** Each inventory row carries stable operation ID, provider method/path, production type/method, required preconditions, scenario IDs, effects/cost classification, latest result, and explicit skip/restriction reason. Deterministic source-level tests compare inventory to registered production operations where practical; completion review checks the endpoint inventory against actual production code.

4. **Use a plan/confirm/run state machine.** `list` and `plan` do not contact APIs. A confirmed session shows exact provider, account/shop/model targets, operation list, write list, upper-bound costs, and cleanup effects. Confirm each Printify write with a typed action phrase. Cancellation stops subsequent writes and leaves state in the report/journal.

5. **Test OpenRouter compatibility in tiers.**
   - Routine smoke run: key check, text/image model catalogs, endpoint catalogs and ZDR discovery, plus a small selected text generation call when explicitly requested.
   - Qualification campaign: discover compatible models from the production catalog clients; produce a resumable matrix of model × text/image operation × effective parameter-profile fingerprint. Include values/features actually supported and emitted by FusionCanvas: token caps, sampling parameters, penalties, seed/stop, reasoning modes and routing where applicable. Do not generate unused speculative combinations.
   - Store evidence by model identity, operation, effective-profile fingerprint, catalog capability fingerprint, and adapter revision. Routine runs reuse valid evidence; a new model/profile/client mapping or `--force` schedules a new costed live request. Unsupported and provider-rejected parameters are distinct outcomes, not passes.
   - Preflight the full selected matrix and maximum cost before generation. Require a user-supplied ceiling, explicit model selection (or confirmation of selected matrix), bounded prompt/output, route, and request count. Use current provider pricing and conservative bounds; block any request whose maximum cost cannot be proved to fit the remaining session budget. If no reliable bound exists for an image model, mark cost-blocked pending a safe provider-side control rather than guessing.

6. **Exercise Printify only in FusionCanvas DevTest.** Resolve the selected account/shop through the production shop verifier and require exact configured shop identity. Test all listed non-cost API operations, with user confirmation before every write. Publication calls are allowed by the user for DevTest but the preflight must state that publishing may expose the test listing on its connected sales channel. Never invoke order endpoints or operations classified as chargeable.

7. **Journal ownership; best-effort cleanup.** Before creating/updating a test resource, record intent and session/run ID; record returned IDs immediately. Only mutate/delete products positively linked to this session. Attempt cleanup and read-back; if unavailable or declined, leave an actionable recovery entry. Cleanup failure is reported independently and does not hide completed operation findings.

8. **Treat findings as a fix loop.** Each failure becomes a structured finding with operation ID, request parameter/header/type concern, expected/actual outcome, safe provider diagnostics, and reproducibility notes. The implementer fixes the owning Application/Integration code, adds/updates deterministic fake-handler or application-level regression tests, reruns the affected live scenario, then closes the finding with evidence. A provider restriction/quirk may remain open only with evidence and an explicit restricted status. Reports never auto-resolve findings.

9. **Keep secrets and useful evidence separate.** Use dedicated test credentials from environment variables or ignored local config; never read normal saved application credentials. Save JSON and concise Markdown summaries under an ignored local verification directory. Store counts, IDs that are non-secret, operation/status, safe error categories, bounded sanitized snippets/hashes, cost, findings, and cleanup journal; redact keys, auth headers, artwork bytes, prompts, generated images, and arbitrary full bodies.

## Risks / Trade-offs

- [The application inventory drifts from actual endpoint use] → Keep operation IDs beside provider calls where practical, compare inventory with production source in deterministic tests, and review it on every adapter change.
- [A full model campaign has high or unpredictable cost] → Separate it from routine runs, require an explicit model matrix and cumulative hard ceiling, cap requests/tokens, preflight costs, and block models without reliable cost bounds.
- [Model metadata omits a provider quirk] → Record provider responses as empirical evidence, add compatibility fixtures/regressions, and preserve mismatches as findings rather than treating catalog support as proof.
- [Publication makes a DevTest listing visible] → Display connected channel and visibility effects before each publish action, require typed confirmation, and provide a separate unpublish action.
- [A Printify mutation times out after succeeding remotely] → Journal intent before request, recover using exact session marker/shop scope, and require positive identity plus confirmation before further mutation.
- [Cleanup cannot finish] → Treat cleanup as best-effort, record ownership and retry instructions, and report unresolved resources without claiming deletion.
- [Logs reveal sensitive design data] → Keep prompt, raw generated content, file bytes, and response bodies out of reports by default; report bounded sanitized fields and hashes.

## Migration Plan

No database migration or normal user workflow change. Add the standalone CLI, versioned API inventory, runbook, ignored output location, and local deterministic tests for the verifier. Rollback removes the CLI and documentation without changing production service behavior. Adapter fixes discovered during the first live runs are separate focused corrections with their own regression coverage and retained finding provenance.

## Open Questions

None for the requested scope. The session requires a configured cumulative OpenRouter cost ceiling; no fixed dollar amount is stored in source control. The model campaign can be selected by text models, image models, or all compatible catalog entries, with budget-based blocking before calls.

## Implementation Plan

1. **Build the baseline inventory.**
   - Verify every row in the Context table against `OpenRouterClient`, the Printify adapters, and the Application service call sites. Capture HTTP method, headers, query/body fields, response parser, and responsible production method.
   - Add `docs/api-verification.md` and update `docs/external-api-mocking.md` to distinguish mocks, fake-handler contract/regression tests, live routine scenarios, and per-model qualification.
   - Add standalone `src/FusionCanvas.ApiVerification/` project referencing Application/Integration, not the solution or CI. Organize `Sessions/`, `Inventory/`, `Findings/`, `OpenRouter/`, and `Printify/` capabilities.

2. **Session, inventory, report, and finding support.**
   - Add `list`, `plan`, `run`, `qualify`, and `recover` commands with exact provider/scenario filters and network-free listing/planning.
   - Add typed operation inventory and session/report/findings records; statuses include implemented, contract-verified, live-verified, failed, restricted, skipped, cost-blocked, unresolved, and cleanup-pending.
   - Add fail-closed credentials/target validation, per-write console confirmation, cancellation, safe local paths, report redaction, and durable resource journal.
   - Unit-test all shared rules with fakes and assert that plan/list and blocked sessions make zero provider calls.

3. **Routine OpenRouter operation coverage.**
   - Map credential validation, model catalog, text endpoint catalog, ZDR list, image catalog, image endpoint catalog, text generation, and image generation to a production service scenario.
   - Read-only catalog/credential scenarios may run together after explicit provider selection. Text/image generation require explicit selected scenario/model, cost plan, token/request limits, and confirmation.
   - Add fake-handler tests for every request/response mapping and inventory assertion; run the manual live scenarios only when the user explicitly launches them.

4. **Per-model OpenRouter qualification.**
   - Use production catalogs and `AiParameterRegistry`/configuration resolution to select models, discover supported parameters, calculate effective profiles, and create a reviewable matrix.
   - Fingerprint model identity/capabilities, effective settings, routing, operation, and adapter code revision. Persist pass/fail/restricted/inconclusive evidence locally to avoid repeated billing.
   - Bound text calls by input/output and exact routing; bound image calls by explicit output dimensions/quality and conservative model pricing. Sum worst-case bounds across selected models, block overflow/unknown costs, and allow partial model selection/resume.
   - For each model operation, record whether specific parameter values were sent, omitted because unsupported, accepted, rejected, or ignored/altered. Add a supported parameters test bundle and representative edge-value cases from the app's valid profile space; do not exhaustively Cartesian-product all values.

5. **Printify operation coverage in DevTest.**
   - Verify the exact `FusionCanvas DevTest` account/shop and map credential verification, blueprint/catalog/provider/variant retrieval, shop product pagination and selected-product retrieval, product detail/import, artwork download, upload, product create/update/delete, and publish/unpublish.
   - For every write show target and effect, get its own confirmation, exercise through the actual application/integration services, and verify resulting state by a read operation.
   - Inspect operation cost and downstream effects; exclude order creation and any chargeable endpoint before the scenario becomes selectable.
   - Journal test resources; perform best-effort update/restore, unpublish, and delete cleanup when applicable. Keep cleanup-pending products visible in recovery output.

6. **Service improvement and regression closure.**
   - Add a finding triage report linking each failed live scenario to the production class/method and likely request/response dimension (type, headers, parameters, body/schema, identity, status/error handling).
   - For defects in existing services, update production handling and add a local deterministic regression test before rerunning the affected live case. Retain the original finding and link the fix/rerun evidence.
   - Keep explicit unknown/restricted questions open; don't infer that a test passed because the endpoint was skipped or a different client succeeded.

7. **Criterion verification and handoff.**
   - Test list/plan/fail-closed paths and all session, inventory, report, model matrix, budget, confirmation, journal, and finding rules without external I/O.
   - For every inventory endpoint ensure fake-handler tests exercise request construction and response parsing. Validate inventory completeness against production source.
   - Run strict `openspec validate`, changed-project checks, and `dotnet test .\FusionCanvas.sln -m:1`. Do not call live endpoints during ordinary completion verification.
   - Record criterion-level deterministic evidence and provide run instructions for manual live operation coverage and the separately selected model qualification campaign.

## Acceptance-to-Verification Mapping

| Scenario | Planned verification |
| --- | --- |
| A developer lists verification scenarios | CLI tests prove listing is network-free and gives operation effects |
| A developer starts a session | Preflight tests cover provider/model/shop selection, credentials, bounds, and confirmation |
| A live scenario is invoked through the solution baseline | Project/CI exclusion inspection and deterministic baseline |
| An API operation is implemented or changed | Inventory parity assertion plus changed-scope source review |
| A live session reports coverage | Report tests validate coverage statuses and no implicit pass for skips |
| A live request or response exposes a defect | Finding workflow tests preserve evidence and require fix/regression/rerun status to close |
| Observed behavior is a restriction or provider quirk | Finding tests ensure evidence-backed restricted/unresolved outcomes remain distinct |
| Developer plans model qualification | Matrix tests cover text/image catalog enumeration, profile fingerprint, count, and cost estimate |
| Model supports an application profile | Production request fake-handler assertions and parameter matrix orchestration tests; first live evidence is manual |
| Qualification exceeds budget | Budget tests assert selected partial run and zero calls for blocked entries |
| Qualification evidence already exists | Fingerprint tests skip unchanged entries; explicit force schedules a new call |
| Model-specific parameter fails | Finding/result tests identify model/profile and retain failed outcome |
| Planned OpenRouter calls fit ceiling | Upper-bound calculator tests and fake-handler tests for exact token/route limits |
| OpenRouter price/limit is unknown or excessive | Fail-closed tests assert no billable request |
| Printify run is planned | Plan tests prove exact DevTest target and no order/chargeable scenario is selectable |
| A Printify write is executed | Scenario tests prove production-service use, per-write confirmation, and read-back |
| Publication operation is executed | Tests verify typed confirm and state verification for publish/unpublish |
| Printify write or cleanup fails | Journal/report tests preserve resource identity and independent cleanup-pending result |
| A session report is written | Serialization/redaction tests cover cost, findings, inventory, outcomes, and no secrets/raw data |
