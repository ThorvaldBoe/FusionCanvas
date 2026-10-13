## 1. Operation inventory and session foundation

- [x] 1.1 Verify the OpenRouter and Printify operation inventory against every production adapter call and record method, endpoint, request/response mapping, risk/cost class, and scenario ID.
- [x] 1.2 Create the standalone `FusionCanvas.ApiVerification` CLI project and provider-scoped scenario structure; verify exclusion from `FusionCanvas.sln` and CI.
- [x] 1.3 Implement network-free list/plan, provider/scenario/model selection, dedicated credential loading, DevTest shop identity validation, explicit confirmation, cancellation, and session reports.
- [x] 1.4 Implement versioned evidence, finding, model-qualification, and Printify resource-journal records with redaction and recovery behavior.
- [x] 1.5 Add deterministic tests for inventory parity, command selection, fail-closed setup, report redaction, journal ownership/recovery, and finding lifecycle.

## 2. OpenRouter operation and model qualification

- [x] 2.1 Implement live scenario mappings for key verification, text catalog, text endpoints, ZDR endpoints, image catalog, image endpoints, text generation, and image generation through production services.
- [x] 2.2 Implement model/profile discovery using production model catalogs and `AiParameterRegistry`; fingerprint model capabilities, routing, effective settings, operation, and adapter revision.
- [x] 2.3 Implement explicit resumable per-model text/image qualification with bounded request parameters and a cumulative cost gate; block unknown or excessive cost before sending.
- [x] 2.4 Add deterministic pricing/upper-bound, model matrix, fingerprint/reuse, request-shape, report, and blocked-call tests using fake handlers and test doubles.

## 3. Printify application-operation coverage

- [x] 3.1 Implement DevTest-only live scenarios for credential/shop verification, blueprints, print providers, variants, shop product pagination/selection/detail, product import and artwork download.
- [x] 3.2 Implement explicit per-write scenarios for artwork upload, product create/update/delete, and publish/unpublish through production application/integration paths, with read-back verification.
- [x] 3.3 Classify every selectable Printify operation as no-direct-charge or sales-visibility risk; exclude order paths and fail closed when the cost class is unknown or billable.
- [x] 3.4 Implement best-effort cleanup, report/journal persistence, and confirmed recovery limited to session-owned resources in FusionCanvas DevTest.
- [x] 3.5 Add deterministic orchestration tests for all listed operations, mismatched shop, cancellation, failure, uncertain write result, publication state, and cleanup-pending behavior.

## 4. Service-fix and regression loop

- [x] 4.1 Implement structured findings that capture operation, expected/actual behavior, request/response concern, safe diagnostic evidence, and production code location.
- [ ] 4.2 For each defect found in the first authorized live sessions, fix the owning production service and add a deterministic regression test before rerunning the affected live scenario.
- [x] 4.3 Keep restrictions, unsupported model parameters, inconclusive results, skipped operations, and unresolved provider behavior explicitly open with evidence instead of marking them passed.

## 5. Documentation and acceptance verification

- [x] 5.1 Document dedicated test account/key setup, manual session commands, per-write confirmation, OpenRouter ceilings and model qualification, DevTest-only scope, evidence reuse, recovery, and report locations.
- [x] 5.2 Update `docs/external-api-mocking.md` to distinguish mocks, local adapter regression tests, routine live verification, and the separately run model qualification campaign.
- [x] 5.3 Map every acceptance scenario and every inventoried production API operation to deterministic evidence or a specifically marked manual live run; report gaps and restrictions.
- [x] 5.4 Run strict `openspec validate`, changed-project checks, and `dotnet test .\FusionCanvas.sln -m:1`; retain criterion-level evidence and unresolved live-service findings in `verification.md`.
