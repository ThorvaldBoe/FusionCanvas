## Why

FusionCanvas has offline adapter tests and production integrations for OpenRouter and Printify, but no controlled way to verify those integrations against provider endpoints. Live verification should expose incorrect assumptions in requests and responses, drive fixes in the production services, and leave deterministic regression coverage behind while keeping cost-incurring work out of uncontrolled test runs.

## What Changes

- Add one opt-in API verification project for current providers, with a versioned inventory mapping every API operation used by the app to live scenarios or an explicit safety restriction.
- Require explicit provider and scenario selection, dedicated test credentials, a preflight plan, and confirmation before each remote write.
- Verify OpenRouter text and image operations, then offer an on-demand, resumable per-model qualification matrix for models and parameter profiles supported by FusionCanvas. Every billable session has a configured cost ceiling; broad model qualification is not part of routine runs.
- Exercise all non-cost-incurring Printify operations used by the app against the dedicated **FusionCanvas DevTest** store, including catalog/shop/product reads and applicable upload, product mutation, and publication lifecycle calls. Orders and any operation that could incur a charge are excluded.
- Persist sanitized reports, model-qualification evidence, and a recovery journal for Printify resources. Route each live discrepancy into a production-service fix and deterministic regression test; never call a failure, skip, restriction, or unresolved finding a pass.
- Keep live sessions outside the default solution test baseline and CI.

Non-goals include Shopify, order submission, any cost-incurring Printify operation, production-store writes, schema-generated tests, scheduled or unattended sessions, and changes to normal application workflows. A session may deliberately publish or unpublish test products in FusionCanvas DevTest after showing the target and receiving per-write confirmation.

## Capabilities

### New Capabilities
- `manual-api-verification`: Explicitly launched, provider-scoped live API sessions with full call inventory, qualification evidence, safety gates, feedback into production fixes, and reports.

### Modified Capabilities
- None. The module adds a developer verification lane and does not change normal application behavior.

## Impact

- New standalone `FusionCanvas.ApiVerification` project and provider scenario suites referencing production Application and Integration services. It remains outside the solution baseline and CI.
- OpenRouter coverage includes all application-used API operations plus a deferred per-model qualification lane for supported parameter profiles and text/image generation.
- Printify scenarios use the real application and Integration services against FusionCanvas DevTest, mapping current API operations to live scenario coverage.
- Add run instructions and sanitized report/journal output under an ignored local directory. No UI, database schema, external packages, or CI network access is planned.

This is one module because call inventory, controlled provider scenarios, failure-to-fix feedback, and qualification evidence form one verification outcome. Routine sessions remain selectable and bounded; per-model qualification is an explicit, cost-budgeted operation that can resume from previous evidence.
