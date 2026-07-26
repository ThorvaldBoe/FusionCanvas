## 1. Dependency and Contract Alignment

- [ ] 1.1 Confirm the relevant `basic-product-creation-workflow` Item, Idea metadata, Stage Tool Host, and SQLite migration contracts are complete or stable; reconcile this change to their final names without expanding behavior.
- [ ] 1.2 Reconcile `src/` and `tests/` Ideation file placement with the completed capability-folder reorganization changes and record any artifact-only path corrections before implementation.
- [ ] 1.3 Add focused failing tests that establish the Ideation mode/rejection model and the exact-group versus whole-niche context rules before production implementation.

## 2. Domain Ideation Model

- [ ] 2.1 Add stable `Basic` and `Snowclones` `IdeationMode` values under the final Domain capability folder.
- [ ] 2.2 Add the immutable validated `IdeationRejection` record with store, niche, optional group, normalized text/reason, mode, and timestamp.
- [ ] 2.3 Extend `WorkspaceSnapshot`, default construction, fixtures, and equality/retention coverage with an Ideation-rejection collection without changing unrelated entities.
- [ ] 2.4 Run `dotnet test .\tests\FusionCanvas.Domain.Tests\FusionCanvas.Domain.Tests.csproj` and correct Domain behavior or artifacts for any failed criterion.

## 3. Application Context and Generation Contracts

- [ ] 3.1 Add `IIdeationService`, `IIdeaGenerator`, `ISnowcloneCatalog`, and `IIdeationAccessStatus` plus focused scope, sanitized context, request, candidate, progress, and result contracts.
- [ ] 3.2 Implement authoritative scope resolution for niche, exact group, and Item-parent contexts, including unavailable inactive/missing/cross-store cases and niche-root creation defaults.
- [ ] 3.3 Implement Ideation context assembly for every applicable active Item Idea, Rejected Item Idea, and durable rejection at exact-group or whole-niche scope.
- [ ] 3.4 Implement metadata sanitization that includes user-authored creative fields while excluding credentials, IDs, timestamps, archive/status fields, file paths, and internal provenance.
- [ ] 3.5 Implement count/access/mode/scope validation, at-most-four concurrent operations, request-order output, progress, cancellation, partial/total failure results, and generation-token-safe completion.
- [ ] 3.6 Implement candidate normalization and duplicate suppression across batch results and existing undecided candidates without unbounded replacement attempts.
- [ ] 3.7 Implement candidate Create through `IItemManagementService`, including first-sentence title derivation, full original-Idea metadata, exact-group or niche-root placement, inherited creation behavior, and authoritative success/failure results.
- [ ] 3.8 Implement candidate Reject by reloading authoritative scope, appending one durable `IdeationRejection`, and atomically saving without creating a Rejected Item.
- [ ] 3.9 Add application tests covering scope, every-active/every-rejected selection, empty Ideas/reasons, sanitization, access recheck, concurrency, ordering, duplicate normalization, cancellation, partial/total failure, Create, Reject, stale scope, and atomic failure.
- [ ] 3.10 Run `dotnet test .\tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj` and correct Application behavior or artifacts for any failed criterion.

## 4. Placeholder Integration Adapters

- [ ] 4.1 Implement `EnvironmentIdeationAccessStatus` so only a non-whitespace `FUSIONCANVAS_AI_API_KEY` produces availability and no API-key value leaves the adapter.
- [ ] 4.2 Implement the immutable in-memory Snowclone catalog with seeded/injectable ordering and exhaust-before-repeat batch selection.
- [ ] 4.3 Implement a genuinely asynchronous, cancellation-aware fake Basic generator that produces concise contextual candidates without network access.
- [ ] 4.4 Implement fake Snowclone placeholder filling for X/Y/Z variables with relevant guidance/group/niche/store terms and distinct output attempts.
- [ ] 4.5 Add Integration tests for access presence/absence, key non-exposure, deterministic catalog ordering/cycling, contextual Basic output, Snowclone filling, concision, and cancellation without real sleeps.

## 5. SQLite Rejection Persistence

- [ ] 5.1 Add `ideation_rejections` to current schema creation, repository validation, transactional clear/insert order, save mapping, and load reconstruction.
- [ ] 5.2 Add the next-free-version transactional migration with store/niche ownership, nullable group association, foreign-key integrity checks, and rollback on failure.
- [ ] 5.3 Update group/store/niche deletion handling so permanent group removal nulls the optional association while workspace-owned deletion follows existing store/niche behavior.
- [ ] 5.4 Add SQLite round-trip tests for group and niche-root rejections, optional reasons, defined modes, timestamps, and later context availability.
- [ ] 5.5 Add previous-version migration fixtures proving all pre-existing Item/workspace data and relationships survive, new databases contain the table, migration failure rolls back, and newer versions remain refused.
- [ ] 5.6 Run `dotnet test .\tests\FusionCanvas.Integration.Tests\FusionCanvas.Integration.Tests.csproj` and correct Integration behavior or artifacts for any failed criterion.

## 6. Dialog View Models and Composition

- [ ] 6.1 Implement `IdeationViewModel` with frozen visible scope, default Basic mode, empty Guidance, default count 5, count validation, progress/busy/error state, and command availability.
- [ ] 6.2 Implement candidate-row state and commands so Create/Reject disable conflicting submission and remove a row only after a successful durable result.
- [ ] 6.3 Implement rejection reason state plus Clear All and combined Close/cancel confirmation state, preserving all input/candidates/progress/selection/focus intent when declined.
- [ ] 6.4 Implement batch cancellation and generation-token invalidation so confirmed Close ignores late results and an ordinary failure leaves the session recoverable.
- [ ] 6.5 Compose Ideation services/adapters through the final application factory and expose Main-window action visibility, enabled state, unavailable guidance, open state, and authoritative refresh coordination.
- [ ] 6.6 Preserve the pre-dialog navigation/tab/tool context while creating/rejecting candidates and restore the stable host focus target when the dialog closes.
- [ ] 6.7 Add framework-free App tests for defaults, validation, command predicates, progress, append/deduplicate behavior, per-row busy state, success/failure row handling, confirmations, cancellation tokens, and context preservation.

## 7. Avalonia Ideation Surfaces

- [ ] 7.1 Add the `Ideation…` action to the consistent Idea-stage Stage Tool Host action area without registering it as a replacement `StageToolDescriptor`.
- [ ] 7.2 Add one owned modal `IdeationWindow` with compact visible scope, extensible mode selector, optional multi-line Guidance, bounded count input, Generate, spinner/progress, Ideas candidate list, Clear All, and Close.
- [ ] 7.3 Add the owned Reject dialog with optional Reason, OK, and Cancel, plus reusable focused discard confirmation behavior for Clear All and combined Close.
- [ ] 7.4 Route operating-system close through the same confirmation/cancellation path and prevent duplicate Ideation or rejection window instances.
- [ ] 7.5 Implement logical tab order, meaningful accessible names/status text, initial Guidance focus, destructive-confirmation Cancel focus, next-row focus after decisions, and launch-action focus return.
- [ ] 7.6 Apply shared semantic theme resources and verify wrapped candidate content plus scroll reachability at the supported minimum window size.
- [ ] 7.7 Add Avalonia headless tests for action stage/context/access states, modal construction/ownership, single-instance behavior, visible scope, invalid count, busy controls, candidate actions, nested rejection, clear/native-close confirmations, focus preservation/return, accessibility names, theme switching, and minimum-layout reachability.
- [ ] 7.8 Run `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj` and correct App behavior or artifacts for any failed criterion.

## 8. Documentation, Security, and Criterion Verification

- [ ] 8.1 Document `FUSIONCANVAS_AI_API_KEY` as a non-secret placeholder availability switch and state that the fake generator is local and sends no workspace content over the network.
- [ ] 8.2 Review every scenario in all four delta specs against focused automated evidence; correct implementation or approved artifacts and rerun the affected criterion plus relevant regression tests for every mismatch.
- [ ] 8.3 Create `verification.md` with criterion-level commands/results/evidence for every acceptance scenario, limitations, and any optional live-desktop observations.
- [ ] 8.4 Run changed-scope architecture and security review, including dependency-direction checks and searches proving the App/Domain do not access SQLite and API-key values cannot enter payloads, logs, errors, or persistence.
- [ ] 8.5 Run `dotnet build .\FusionCanvas.sln` and resolve all errors and warnings introduced by this change.
- [ ] 8.6 Run the mandatory baseline `dotnet test .\FusionCanvas.sln` and resolve all failures introduced by this change.
- [ ] 8.7 Run `openspec validate add-ideation-tool --strict` and correct any specification or artifact drift.
- [ ] 8.8 Optionally perform one disposable live-desktop observation for native modal ownership, native close interception, spinner smoothness, and visual density; record it only as supplemental evidence.
- [ ] 8.9 Complete the scoped module QA review and confirm no unresolved acceptance, migration, security, persistence, UI, or dependency ambiguity remains before requesting archive.
