## Why

FusionCanvas currently opens directly into the workspace without ensuring that the user understands FusionCanvas's own usage expectations, the obligations attached to Printify and Shopify use, or their responsibility for copyright, trademark, and other intellectual-property rights. Issue #319 calls for a clear acknowledgement before the user begins working, and the first-run boundary is the most visible and reliable place to establish that responsibility.

The module should establish informed user acknowledgement without pretending that FusionCanvas accepts third-party provider agreements on the user's behalf, verifies ownership, or provides legal advice.

## What Changes

- Add a first-run, app-wide consent surface before the main workspace and navigation become available.
- Require separate acknowledgement of:
  - the versioned FusionCanvas Terms of Use and Responsible Use Policy;
  - current Printify terms and applicable policies;
  - current Shopify terms and applicable policies;
  - the user's responsibility to have rights and permissions for content, brands, trademarks, and other intellectual property.
- Keep the consent surface usable offline by bundling the FusionCanvas policy text and providing clearly labeled links to current provider policies.
- Persist a minimal, versioned acknowledgement record in application settings rather than workspace data.
- Reopen the gate when the FusionCanvas terms or acknowledgement requirements change, while making clear that provider policies may change independently and must be reviewed by the user.
- Provide a Settings route for reviewing the accepted policy versions and reopening the policy documents.
- Add deterministic application, persistence, and Avalonia headless view coverage for the consent states and startup boundary.

The module does not add automated infringement detection, legal advice, provider publishing, provider-specific compliance validation, or a claim that a checkbox completes a user's contractual acceptance with Printify or Shopify.

## Capabilities

### New Capabilities

- `terms-consent`: App-wide first-run acknowledgement, versioning, policy presentation, persistence, and review behavior.

### Modified Capabilities

- `desktop-application-foundation`: Startup must enforce the consent gate before exposing the main workspace and navigation.
- `application-settings`: Settings must expose the current acknowledgement state and policy-review access without duplicating the first-run acceptance workflow.

## Impact

- **App startup and UI:** startup composition, splash transition, a focused consent window or equivalent surface, keyboard focus, cancellation/quit behavior, and the Settings window.
- **Application layer:** consent value objects, acknowledgement state, version invalidation rules, and an application-facing persistence contract.
- **Integration layer:** backward-compatible JSON application-settings persistence for the acknowledgement record and bundled policy content/resource access.
- **Testing:** application policy tests, isolated settings-store tests, Avalonia headless view tests, startup composition tests, and the solution baseline.
- **Documentation/legal content:** a short, versioned FusionCanvas Terms of Use and Responsible Use Policy must be reviewed before implementation is treated as legally authoritative. Third-party policy text remains owned by and linked to Printify and Shopify.
- **Compatibility:** existing installations with no acknowledgement record will require the gate once; existing workspaces and workspace packages remain unchanged.
