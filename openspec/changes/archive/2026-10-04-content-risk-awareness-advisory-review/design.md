## Context

Issue #318 asks FusionCanvas to warn creators about possible IP infringement. Discovery expanded the outcome to include harmful and otherwise inappropriate content because customer-facing print-on-demand content must be reviewed for both legal/rights signals and marketplace-safety risks.

The current application has several separate boundaries: `ConceptRefinementService` and `TitleOptimizationService` apply AI text; `ArtworkGenerationService` creates managed PNG Assets; `DesignFileService` imports customer-facing PNGs; and `AssetManagementService` imports broader reference or creative assets. The existing AI boundary can carry text and image inputs, and the artwork pipeline already persists provenance, but there is no common review state, provider-neutral analyzer contract, stale-result rule, or persistent warning UI.

The module is advisory. It must raise awareness of obvious signals and preserve awareness when analysis is unavailable, while never claiming legal clearance, complete marketplace compliance, or freedom from infringement.

## Goals / Non-Goals

**Goals:**

- Give every relevant customer-facing AI-assisted value and artwork Asset a persistent default warning.
- Attempt best-effort review at the boundary where content becomes customer-facing.
- Keep IP, harmful-content, and marketplace-suitability findings distinct.
- Support text and image review through an Application-owned, provider-neutral contract.
- Reuse the configured AI boundary for an initial advisory analyzer where image-capable review is available, while preserving a path for dedicated moderation/OCR/logo providers later.
- Persist typed findings, non-secret provenance, content fingerprints, review availability, and stale state.
- Keep review failures recoverable and non-blocking.
- Provide compact, accessible warnings with progressive disclosure in the Item, Design, and relevant asset surfaces.

**Non-Goals:**

- Trademark database search or a standalone trademark-check feature.
- Legal advice, legal clearance, or an infringement verdict.
- Comprehensive copyright, character, logo, or visual-similarity search.
- Automatic blocking, deletion, rejection, or publication prevention.
- Marketplace certification or a claim that one provider's safety categories cover every platform's policy.
- Review of every private reference asset before it becomes customer-facing.
- A new direct OpenAI credential/account setup solely for moderation in this module.

## Decisions

### Use a cross-cutting review capability, not UI-specific checks

Add Domain/Application content-risk types and orchestration under an owned capability, then integrate at existing AI-application, artwork-assignment, and asset-import boundaries. View models render state and invoke details; they do not classify content or decide freshness.

This avoids duplicating policy in `ItemInspectorViewModel`, `DesignStageToolViewModel`, and `AssetsViewModel`, while preserving each existing service's ownership and persistence behavior.

### Default to awareness, never to clearance

Every customer-facing review target starts as `Unreviewed`. A successful analyzer can return `PotentialRisk` or `NoObviousSignalDetected`; a missing or failed analyzer returns `ReviewUnavailable`. All states retain the limitation warning. There is deliberately no `Safe`, `Approved`, or `Cleared` state.

The alternative—showing a green result after a negative classifier response—would communicate more certainty than the system can support and would conflict with the local-first, creator-control principles.

### Separate risk dimensions

Findings use separate categories such as `IpRisk`, `SafetyRisk`, and `MarketplaceSuitability`. Safety categories may include sexual, hate, harassment, violence, graphic violence, self-harm, and illicit content. IP findings may describe possible names, logos, characters, franchises, or confusing similarity. The analyzer explains signals; it does not decide legal status.

Keeping dimensions separate prevents a harmful-content result from being mistaken for an IP result and allows future marketplace-specific rules without changing the IP contract.

### Review at customer-facing boundaries

The initial integration points are:

- AI Concept refinement when Phrase or Graphic direction is applied;
- AI title optimization when a customer-facing title is applied;
- generated artwork after final normalization and before or during Design-slot assignment;
- PNG Design-file import or assignment when it becomes customer-facing.

Ideation text, SLL documents, and private reference assets remain working material until they cross a declared customer-facing boundary. The review target carries a stable role such as `concept.phrase`, `listing.title`, or `design.asset` so later surfaces can reuse the same contract.

### Use one Application-owned analyzer contract with optional providers

Application exposes a small `IContentRiskAnalyzer` accepting bounded text and/or image content plus a review purpose, and returning typed findings or a typed unavailable/failure result. The initial external analyzer reuses the existing AI text provider boundary, including its configured privacy policy and image-input support, with a strict bounded JSON response contract.

This keeps provider SDK and response formats in Integration. It also avoids introducing a second credential setup in the first module. Dedicated moderation, OCR, or logo-detection adapters can be added later behind the same boundary after benchmark evidence justifies their cost and privacy trade-offs.

### Persist current review records as typed workspace data

Add a versioned `ContentRiskReview` record collection to the workspace snapshot and SQLite schema. Each record identifies the content owner and role, stores a content fingerprint, state, findings, analyzer identity, checked time, and optional bounded usage/cost metadata. It does not store credentials, raw provider payloads, source paths, or unbounded extracted content.

The record is separate from Asset metadata because one Item can have multiple independently reviewed text roles and one Asset may move between destinations. Existing assets and Items load with no record as `Unreviewed` when they become relevant.

### Make review failure visible but non-blocking

Review runs may fail, be cancelled, or be unavailable without rolling back a successful AI application, artwork generation, or file import. The content remains usable, but the warning remains prominent and the UI explains that review could not be completed. No automatic retry occurs; an explicit user retry may start a new analyzer request.

This preserves the advisory product decision and local-first usefulness while avoiding a false negative caused by treating “could not check” as “no risk.”

### Use progressive disclosure in the primary workspace

The main Item and Design surfaces get a compact warning row near the affected field or artwork. A `Review risk details` action opens an inline expansion or focused details region rather than a separate administration window. Details include categories, safe explanations, method, time, unavailable guidance, and the limitation statement. The primary creative workflow remains visible and keyboard reachable.

### Treat external review as a privacy and cost decision

The analyzer sends only the reviewed text or final managed image plus minimal review instructions. It honors the existing AI privacy configuration and bounds image bytes and response size. Provider-reported usage/cost may be shown when available, but missing usage is not fabricated. Review telemetry excludes content and raw provider responses.

## Risks / Trade-offs

- **AI review produces false positives or misses subtle risks** → Use advisory wording, separate findings from clearance, persist limitations, and create a representative benchmark before tuning thresholds.
- **Review adds a provider request and latency to creative workflows** → Review only at customer-facing boundaries, show progress, permit cancellation, do not retry automatically, and keep content usable when review is unavailable.
- **External review exposes user content to a provider** → Reuse configured privacy policy, disclose the external check in the UI, send bounded inputs only, and retain a local-only warning path.
- **A dedicated SQLite collection increases migration and workspace-transfer surface** → Version the schema, round-trip records through workspace packages, and keep unknown/missing review records non-fatal.
- **Warnings consume valuable workspace space** → Use compact inline notices and progressive disclosure; reserve detailed evidence for the expanded details region.
- **Marketplace rules vary by destination** → Keep marketplace suitability as an advisory category and do not claim certification for any marketplace.
- **Current provider capabilities may not support reliable image review** → Persist `ReviewUnavailable`, retain the warning, and keep provider-specific enhancements behind the analyzer contract.

## Migration Plan

1. Add the review value types, workspace snapshot collection, SQLite table, migration, and workspace-package transfer support. Existing workspaces load without review records.
2. Add analyzer contracts, bounded request/response parsing, safe failure mapping, and deterministic fake analyzers for tests.
3. Add application orchestration and integrate the four initial customer-facing boundaries.
4. Add warning and details presentation to the Item, Design, and relevant asset surfaces.
5. Validate migration, round-trip, stale-result, provider-unavailable, and failure behavior.

Rollback is code-version rollback with the forward-compatible review table left unused; existing content remains readable. If the feature is disabled, review records remain inert local data and do not change content ownership or publication behavior.

## Implementation Plan

### Affected layers and likely types

- **Domain:** `ContentRiskReview`, `ContentRiskReviewState`, `ContentRiskFinding`, `ContentRiskCategory`, `ContentRiskSeverity`, and fingerprint validation under a focused ContentRisk namespace.
- **Application:** `IContentRiskAnalyzer`, analyzer request/result contracts, `ContentRiskReviewService`, customer-facing review-target resolution, stale-state policy, and integration hooks in `ConceptRefinementService`, `TitleOptimizationService`, `ArtworkGenerationService`, `DesignFileService`, and `AssetManagementService` where appropriate.
- **Integration:** provider-neutral analyzer adapter over the existing AI boundary, bounded JSON codec, safe failure translation, SQLite persistence, workspace snapshot/package transfer, and test fake analyzer.
- **App:** shared warning/detail view model or projection, Item Inspector integration, Design Stage Tool integration, Asset row/status integration, shared theme resources, and accessible commands.

### Persistence and algorithms

1. Give each review target a stable owner/role key and compute a SHA-256 fingerprint from normalized text or final managed image bytes plus the customer-facing role.
2. On successful content application, create an `Unreviewed` record before or within the same logical persistence operation as the content mutation where the existing boundary already supports atomic save.
3. Start analysis after the confirmed content boundary, or as a staged part of an existing operation when the final bytes are already available. Save the typed result against the fingerprint.
4. When content or role changes, discard the previous result by fingerprint mismatch and project `Unreviewed` without deleting useful historical content.
5. Parse only a bounded, schema-validated analyzer response. Unknown categories, excessive evidence, malformed JSON, and provider errors map to `ReviewUnavailable`.
6. Never use a negative analyzer result to suppress the default limitation warning.

### Sequencing

1. Domain contracts and deterministic policies.
2. Persistence and snapshot/package migration.
3. Analyzer boundary, fake provider, and safe response codec.
4. Application orchestration and the four content-application integrations.
5. UI warning/details projection and theme/accessibility coverage.
6. Benchmark representative text/images and document observed cost/latency as verification evidence without changing the advisory contract.

### Verification plan

- Domain tests cover category separation, state transitions, fingerprints, and stale detection.
- Application tests cover boundary triggers, non-blocking failures, cancellation, no-provider behavior, retry, and atomic review/content updates.
- Integration tests cover bounded payloads, malformed/oversized provider responses, privacy-field exclusion, SQLite migration, reload, and workspace transfer.
- Avalonia headless tests cover warning visibility, details expansion, keyboard access, focus return, theme distinction, and review-unavailable presentation.
- One supplemental Appium scenario pack is warranted: create or import customer-facing artwork, observe the default warning, run an available review, inspect findings, and verify that content remains usable after an unavailable review. The deterministic baseline remains authoritative.

### Decisions not to reopen during implementation

- Review is advisory and never a legal-clearance or automatic-blocking system.
- Default warning remains visible after `NoObviousSignalDetected`.
- Trademark search and comprehensive visual-similarity search are separate future work.
- Private reference assets are not reviewed until promoted to a customer-facing role.
- Provider integrations remain behind Application contracts and must not leak provider SDK types into Domain or App.

## Open Questions

None block implementation of this module. The benchmark may inform later provider additions or marketplace-specific adapters, but it must not change the advisory semantics established here.
