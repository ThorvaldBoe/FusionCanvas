## 1. Domain review model

- [x] 1.1 Add immutable Content Risk value types for review state, category, severity, finding, review target role, and non-secret provenance.
- [x] 1.2 Implement deterministic content fingerprinting for normalized text and final managed image bytes, including the customer-facing target role.
- [x] 1.3 Add domain policy tests for default `Unreviewed`, `ReviewUnavailable`, `PotentialRisk`, `NoObviousSignalDetected`, category separation, and fingerprint mismatch/stale behavior.

## 2. Workspace persistence and transfer

- [x] 2.1 Extend `WorkspaceSnapshot` with current Content Risk review records and add the SQLite schema/table with a backward-compatible migration.
- [x] 2.2 Persist review targets, fingerprints, findings, timestamps, analyzer identity, and optional bounded usage/cost metadata without credentials, raw provider payloads, or source paths.
- [x] 2.3 Include Content Risk review records in workspace package export/import filtering, compatibility checks, and normalized round-trip comparisons.
- [x] 2.4 Add isolated Integration tests for fresh-database migration, save/load round trips, malformed finding data, unknown review versions, and workspace transfer.

## 3. Analyzer boundary and safe provider adapter

- [x] 3.1 Add Application-owned `IContentRiskAnalyzer` request/result contracts for bounded text and image review with typed failure and unavailable outcomes.
- [x] 3.2 Add a strict bounded analyzer response codec that accepts only known categories/severities, limits findings and evidence, and maps malformed or excessive responses to `ReviewUnavailable`.
- [x] 3.3 Implement the initial advisory analyzer over the existing configured AI text/image boundary with a dedicated review purpose, privacy-policy propagation, no automatic retry, and optional provider usage/cost capture.
- [x] 3.4 Add deterministic fake analyzers and Application tests for IP findings, safety findings, no-obvious-signal results, provider failure, cancellation, privacy-field exclusion, and non-blocking behavior.
- [x] 3.5 Add Integration tests for provider request construction, image byte bounds, response bounds, safe error messages, and exclusion of credentials, paths, identifiers, and raw provider payloads.

## 4. Review orchestration and content boundaries

- [x] 4.1 Implement `ContentRiskReviewService` to create `Unreviewed` records, run explicit analysis, persist typed results, retain warnings on failure, and invalidate results when fingerprints change.
- [x] 4.2 Integrate review-target creation with Concept refinement for applied Phrase and Graphic direction values without changing existing draft, history, or save semantics.
- [x] 4.3 Integrate review-target creation with AI title optimization for accepted customer-facing titles without making optimization failure depend on review availability.
- [x] 4.4 Integrate review with final generated artwork bytes before or during customer-facing Design-slot assignment and preserve the existing artwork transaction/cancellation guarantees.
- [x] 4.5 Integrate review with customer-facing PNG Design-file import or assignment while leaving private reference assets outside the review boundary until promotion.
- [x] 4.6 Add Application tests covering every boundary, replacement with a new fingerprint, working-only content, review retry, cancellation, and confirmed content state preservation.

## 5. Application UI and accessibility

- [x] 5.1 Add a compact shared content-risk warning projection with explicit copy for unreviewed, findings, no-obvious-signal, and unavailable states.
- [x] 5.2 Integrate the warning with Item Inspector customer-facing text, Concept refinement results, Design artwork slots, and relevant Asset rows without duplicating classification logic in view models.
- [x] 5.3 Add keyboard-reachable progressive disclosure for review details, bounded evidence, method, timestamp, limitations, and retry guidance; restore focus to the invoking control when details close.
- [x] 5.4 Apply shared Light/Dark warning, finding, and unavailable theme resources and verify that state is not communicated by color alone.
- [x] 5.5 Add focused Avalonia headless tests for warning visibility, details expansion, keyboard traversal, focus return, theme distinction, empty/unavailable states, and content remaining usable.

## 6. Criterion-level verification and delivery gates

- [x] 6.1 Map every Content Risk acceptance scenario to a passing domain, Application, Integration, or headless UI test and record the evidence in the change verification record.
- [ ] 6.2 Run representative text/image benchmark cases through the configured analyzer path, record false-positive/false-negative observations, latency, privacy behavior, and reported cost, and keep the results advisory.
- [x] 6.3 Decide and document the supplemental Appium scenario pack for customer-facing artwork warning, findings, and unavailable review; keep it isolated from the deterministic baseline.
- [x] 6.4 Run strict OpenSpec validation with `openspec validate` and resolve every proposal/spec/design/task issue before implementation handoff.
- [x] 6.5 Run the repository baseline `dotnet test .\\FusionCanvas.sln` and record the result before the change is considered ready for apply/verification.
