## 1. Delivery readiness and provider contracts

- [x] 1.1 Review the proposal, delta specs, design, and current issue #320 credential implementation; record approval or delegated approval before application code changes.
- [x] 1.2 Add safe immutable Printify catalog DTOs, typed failure results, and `IPrintifyCatalogClient` under `FusionCanvas.Application`.
- [x] 1.3 Implement the bounded V1 Printify catalog client under `FusionCanvas.Integration` with fixed HTTPS origin, bearer request scope, no redirects, timeout, cancellation, response limits, and safe status/JSON mapping.
- [x] 1.4 Add fake-HTTP tests for catalog endpoint sequencing, headers, success payloads, empty results, malformed JSON, oversized responses, authentication/permission/rate-limit/timeout/network/service failures, and cancellation.

## 2. Catalog import and domain mapping

- [ ] 2.1 Add provider identity fields or metadata mappings for Store-scoped Blueprint, offering, option/value, variant, and placeholder/design-area records with backward-compatible defaults.
- [x] 2.2 Implement a focused Printify catalog import service that validates saved Store identity, Shopify + Printify strategy, credential/shop availability, and captured context generation.
- [x] 2.3 Implement normalized import-plan creation for Blueprint, provider offering, typed options/values, sellable variants, and variant-compatible design areas/placeholders.
- [x] 2.4 Implement identity-based merge behavior that updates matching records, creates missing records, preserves local IDs and local-only records, and rejects duplicate or inconsistent provider identities.
- [ ] 2.5 Add Domain/Application tests for identity matching, option-kind mapping, variant compatibility, malformed payload rejection, Store isolation, and no-network refusal for unsupported strategies.

## 3. SQLite persistence and composition

- [x] 3.1 Add the ordered backward-compatible SQLite migration and mappings for nullable provider identities and any required uniqueness indexes.
- [ ] 3.2 Persist confirmed imports through one transaction, including rollback on validation or write failure; preserve existing catalog relationships and workspace-package compatibility.
- [ ] 3.3 Add isolated Integration tests for migration, atomic import, repeated import, changed provider data, reload round-trip, duplicate identity rejection, and preservation of provider-missing local records.
- [x] 3.4 Wire the catalog client and import service through application composition without exposing tokens to UI, ordinary persistence, transfer, or diagnostics.

## 4. Store Editor import workflow

- [x] 4.1 Add the strategy/credential/shop-gated Store Editor import state and commands beside New Blueprint, with compiled bindings and automation IDs.
- [x] 4.2 Add the focused Blueprint selection surface with loading, empty, selection, confirmation, cancel, retry, and safe error states.
- [ ] 4.3 Implement keyboard selection/confirmation/cancellation, focus restoration, local draft preservation, busy guards, and late-result rejection on Store/context changes.
- [x] 4.4 Refresh the authoritative Catalog & mockups state after successful import while preserving current selection when meaningful and avoiding mockup-template or file creation.
- [ ] 4.5 Add view-model tests for visibility guards, selection counts, confirmation/no-mutation cancellation, failure recovery, context races, and post-import selection.
- [ ] 4.6 Add deterministic Avalonia headless tests for construction, compiled bindings, action visibility/enabled state, list selection, focus, keyboard flow, busy/error states, and absence of provider image/mockup controls.

## 5. Verification and delivery

- [ ] 5.1 Populate `verification.md` with criterion-level evidence for every scenario in the delta specs and correct any failed behavior or approved artifact.
- [x] 5.2 Run focused tests for Domain, Application, Integration, and App projects while implementing and record their results.
- [x] 5.3 Run `dotnet build .\FusionCanvas.sln`, `dotnet test .\FusionCanvas.sln`, and `openspec validate import-printify-catalog --strict`; all must pass.
- [ ] 5.4 Perform scoped completion QA for architecture, security, persistence, UI, external-request safety, and changed-scope drift; record limitations and retrospective.
- [ ] 5.5 After acceptance, synchronize/archive the change and follow the repository issue completion workflow; do not merge while approval or verification is blocked.
