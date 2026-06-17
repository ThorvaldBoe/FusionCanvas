# FC-0401 - AI Provider Abstraction

## Summary

AI Provider Abstraction lets FusionCanvas support multiple AI providers without tying workflows to one vendor.

## Requirements

- AI workflows can call a generic provider interface.
- Providers can represent cloud, local, or future AI services.
- Provider capabilities can be described consistently.
- Provider-specific details should not leak into core workflows.
- Errors, limits, and unavailable providers should be understandable to users.

## Acceptance Criteria

- A workflow can request AI assistance without knowing the provider implementation.
- A provider can describe supported capabilities.
- A missing or failed provider does not corrupt user work.

## Out of Scope

- Specific provider implementation
- Billing management
- Plugin marketplace

## Related Notes

- [[Roadmap]]
- [[Architecture]]
- [[Principles]]
