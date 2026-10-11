## 1. Printify connection and identity verification

- [x] 1.1 Change the lifecycle connection request to read `/v1/shops.json`, match the selected shop ID exactly, and fail closed on missing or invalid shop data.
- [x] 1.2 Retain the actual product ID and shop ID returned by product reads so adoption can reject missing or mismatched identities.
- [x] 1.3 Add mocked Integration tests for shop lookup outcomes and product identity parsing; update the existing connection contract test.

## 2. Verified mapping use case

- [x] 2.1 Add candidate verification that validates Store and Item scope, current mapping state, selected shop, and exact product/shop identity without writing.
- [x] 2.2 Add confirmed linking that re-reads the product, rejects duplicate Store mappings and pending/uncertain replacements, then saves the verified remote snapshot while preserving prior mapping data on failure.
- [x] 2.3 Add Application tests for link, replace, cancellation/no-write, remote verification failures, duplicate mappings, stale candidates, and uncertain state.

## 3. Listing tool interaction

- [x] 3.1 Show the current mapped product ID in a selectable, copyable control and add candidate ID verification in the Printify Listing tool.
- [x] 3.2 Add a clear inline confirmation naming the Item, selected shop, product ID, and title, plus cancel and confirmed-link actions with loading, success, and error states.
- [x] 3.3 Add Avalonia headless coverage for current ID visibility, command enablement, verification and confirmation bindings, cancellation, and read-only behavior.

## 4. Acceptance verification and delivery readiness

- [x] 4.1 Complete `verification.md` with results and evidence for every scenario in the capability delta.
- [x] 4.2 Run focused Integration, Application, and App tests, then run `openspec validate link-existing-printify-products --strict` and resolve any failures.
- [x] 4.3 Run `dotnet test .\FusionCanvas.sln -m:1`; record the result and any repository warnings in `verification.md`.
