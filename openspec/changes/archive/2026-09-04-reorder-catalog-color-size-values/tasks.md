## 1. Catalog ordering contract

- [x] 1.1 Add a validated catalog setup reorder request/service operation that accepts one Offering Option and an ordered active-value identity list.
- [x] 1.2 Normalize active values to contiguous zero-based `SortOrder`, append new values after active values, and reject cross-option, archived, duplicate, missing, or stale IDs atomically.
- [x] 1.3 Add focused application tests for Color and Size reorder, append behavior, normalization after archive/restore, identity/reference preservation, and invalid requests.

## 2. Persistence and ordered consumers

- [x] 2.1 Update SQLite migration/backfill and snapshot load/save paths to preserve deterministic existing order with stable identity tie-breaking.
- [x] 2.2 Ensure catalog choice projections and relevant Color/Size consumers order active values by persisted order with an identity tie-breaker.
- [x] 2.3 Add isolated SQLite round-trip and legacy-schema tests for order persistence, deterministic backfill, and relationship preservation.

## 3. Option value management UI

- [x] 3.1 Add a visible grip/handle for every active value row in the focused management dialog and make Color and Size behavior equivalent.
- [x] 3.2 Implement pointer drag/drop reorder plumbing with clear drop validation, refresh notifications, and no partial update on failure.
- [x] 3.3 Add Move up/Move down command equivalents with target-specific accessible names, predictable focus order, and disabled states at list boundaries.
- [x] 3.4 Add Avalonia headless coverage for handle/control construction, Color and Size reorder behavior, accessibility labels, and keyboard-equivalent actions.

## 4. Verification and delivery artifacts

- [x] 4.1 Run focused domain/application/integration/App tests and resolve any regressions in existing catalog management behavior.
- [x] 4.2 Map every delta-spec scenario to result and evidence in `verification.md`, including the full solution baseline `dotnet test .\FusionCanvas.sln`.
- [x] 4.3 Run strict `openspec validate` and correct any artifact or spec drift before completion.
- [x] 4.4 Complete the retrospective/learning review, promote or explicitly defer reusable lessons, and confirm archive readiness.
