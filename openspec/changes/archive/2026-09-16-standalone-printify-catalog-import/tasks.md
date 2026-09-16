## 1. Application behavior

- [x] 1.1 Update the Printify catalog import service to accept all strategies recognized by `FulfillmentStrategyPolicy.RequiresPrintifyKey` and use strategy-neutral setup guidance.
- [x] 1.2 Update the import-session precondition message to describe a saved active Printify Store without implying Shopify is required.

## 2. Regression coverage

- [x] 2.1 Add Application tests proving standalone Printify can retrieve catalog data and preserves the existing credential, shop, and active-store guards.
- [x] 2.2 Add or extend import coverage proving standalone Printify can confirm and persist selected catalog data without changing Shopify behavior.

## 3. Verification and completion

- [x] 3.1 Run focused Application/App tests and record criterion-level results for every acceptance scenario in `verification.md`.
- [x] 3.2 Run `dotnet test .\\FusionCanvas.sln` and `openspec validate`, then record final evidence and limitations in `verification.md`.
