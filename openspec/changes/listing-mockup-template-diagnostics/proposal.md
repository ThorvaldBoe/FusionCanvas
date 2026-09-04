## Why

When Listing has no ready mockup templates, the selector is empty and the user sees only a generic instruction. This makes a valid Draft template look missing and forces the creator to hunt through Store settings to discover the actual unmet requirements.

## What Changes

- Keep the Listing selector restricted to active, ready-to-use templates.
- Expose actionable readiness diagnostics in Listing when no ready template is available.
- Distinguish between no configured templates and configured templates that are incomplete.
- Show each affected template name with its exact missing or invalid requirements and identify Store settings as the place to correct them.
- Preserve the existing Store mockup-template editor and authoritative readiness policy; do not weaken eligibility rules.

## Capabilities

### New Capabilities

- `listing-mockup-template-diagnostics`: Actionable empty and blocked states for Listing mockup template selection.

### Modified Capabilities

- None.

## Impact

- Application mockup-generation state and eligibility result contracts will carry candidate readiness summaries.
- Listing stage view model and AXAML will present the diagnostics without adding a new persistent data model or navigation system.
- Application and App tests will cover no-template, draft-template, and ready-template presentation states.
- No persistence schema, image-composition, or template-readiness rule changes are expected.
