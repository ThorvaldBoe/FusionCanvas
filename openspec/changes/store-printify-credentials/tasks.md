## 1. Delivery readiness

- [x] 1.1 Review the proposal, delta specs, design, and verification mapping; record user approval or explicitly delegated approval before application changes.
- [x] 1.2 Create the issue branch `codex/320-store-printify-credentials`; inspect native-library backend and replacement guarantees, resolving any security constraint through artifact correction before implementation.

## 2. Strategy and Application behavior

- [x] 2.1 Enable the three stable strategy values and add Printify applicability rules, with Domain tests and Application valid/invalid strategy tests.
- [x] 2.2 Add Store credential scope, safe result types, native and verification ports, and configuration service with workspace/Store/archive/strategy guards; cover isolation and no-network refusal paths.
- [x] 2.3 Add persistence round trips for all strategy values without schema changes; verify local catalogs remain usable for all selections.

## 3. Secure adapters

- [x] 3.1 Implement per-workspace/Store native credential read/save with isolated backend tests, safe failures, replacement behavior, and no plaintext fallback.
- [x] 3.2 Implement the bounded read-only Printify verifier and fake-HTTP tests for method, origin, headers, redirects, status mapping, JSON limits, timeout, and cancellation.
- [x] 3.3 Parse and return Printify shop ID/title options from successful verification responses, with bounded parsing tests.
- [x] 3.4 Wire and dispose production services and clients without changing OpenRouter behavior; verify no secret reaches ordinary persistence, transfer, or diagnostics.

## 4. Store Editor and dialog

- [x] 4.1 Add a focused credential child view model with explicit presence/error/busy states, store identity capture, invalidation, and deterministic race tests.
- [x] 4.2 Add friendly strategy labels and Store Save transition confirmation; test cancellation, retention, validation, and unchanged Store draft behavior.
- [x] 4.3 Bind credential labels and Add/Manage/Verify/Retry actions with saved-Store and persisted-strategy guards; cover selection, archive, and context transitions.
- [x] 4.4 Add the Store-scoped shop dropdown, persist selected shop ID in StoreContext, restore it on reload, and clear it when a later verification omits it.
- [x] 4.5 Implement the owned masked key dialog, independent Save, validation, discard confirmation, busy guards, draft clearing, and keyboard focus restoration.
- [x] 4.6 Add deterministic Avalonia headless coverage for selector interaction, bindings, masking, dialog focus, command state, shop selection, and context changes.

## 5. Verify and deliver

- [x] 5.1 Fill every verification.md scenario with results and concrete evidence; correct failed behavior or approved artifacts and rerun affected checks.
- [ ] 5.2 Run `dotnet build .\FusionCanvas.sln`, `dotnet test .\FusionCanvas.sln`, and `openspec validate store-printify-credentials --strict`; all must pass.
- [x] 5.3 Perform scoped completion QA for architecture, C# standards, security, persistence, UI, and changed-scope drift; record limitations accurately.
- [ ] 5.4 Record retrospective and complete accepted-spec synchronization/archive after acceptance; commit, push, link issue/change in the PR, and follow repository merge/approval gates.

