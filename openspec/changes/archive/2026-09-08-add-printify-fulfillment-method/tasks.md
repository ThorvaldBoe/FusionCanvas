## 1. Domain and policy

- [x] 1.1 Add the stable standalone `Printify` fulfillment strategy without changing existing enum values.
- [x] 1.2 Update fulfillment policy membership and Printify-key requirements; add focused policy tests.

## 2. Store editor behavior

- [x] 2.1 Add the strategy label and update editor guidance for standalone Printify.
- [x] 2.2 Reuse the policy for Printify credential visibility, shop persistence, verification context, and strategy-change confirmation.
- [x] 2.3 Add or update application and headless App tests for selection, persistence, visibility, and confirmation behavior.

## 3. Verification and completion

- [x] 3.1 Run focused tests and the full `dotnet test .\FusionCanvas.sln` baseline.
- [x] 3.2 Run `openspec validate --strict` and map every acceptance scenario in `verification.md` with result and evidence.
- [x] 3.3 Complete the retrospective and archive the change.
