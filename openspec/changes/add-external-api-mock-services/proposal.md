## Why

FusionCanvas currently tests its live OpenRouter and Printify adapters with fake HTTP handlers, while application tests rely on one-off local stubs. That proves request translation but does not provide reusable, endpoint-free service doubles for exercising application workflows, failure paths, or future desktop scenarios safely and quickly.

This module establishes a reusable mock twin for every currently implemented external API service. It is intentionally limited to the existing OpenRouter and Printify integrations; Shopify remains out of scope until it exists in the product.

## What Changes

- Record the current external API inventory and its testing boundary.
- Preserve the existing application-facing interfaces and live adapter behavior.
- Add in-memory mock twins for the OpenRouter provider and both Printify outbound services.
- Give mock services deterministic synthetic data, configurable outcomes, cancellation support, and safe request observations without storing API secrets.
- Add focused tests proving mock services implement the same contracts, never require network access, and support application-facing success and failure scenarios.
- Keep transport-level live-adapter tests separate; they continue to use fake HTTP handlers rather than live endpoints.

Non-goals:

- No Shopify integration or Shopify mock before Shopify is implemented.
- No production UI switch or default composition change that could accidentally route normal users to mock services.
- No live API calls in unit tests.
- No change to credential storage, telemetry policy, or external API semantics beyond the refactoring needed to preserve the contracts.

## Capabilities

### New Capabilities

- `external-api-mocking`: Reusable, endpoint-free mock twins and the inventory/testing rules for current external API services.

### Modified Capabilities

- None. This module adds testability infrastructure and does not change an accepted user-facing workflow.

## Impact

- `FusionCanvas.Application` contracts remain the stable boundary for external services.
- `FusionCanvas.Integration` gains mock service implementations alongside the live OpenRouter and Printify adapters.
- Integration tests gain coverage for the mock implementations and their synthetic data/failure controls.
- Application-level consumers can be tested with reusable mocks without referencing `HttpClient` or any live endpoint.
- The change has no UI surface; UX preflight is not applicable.
