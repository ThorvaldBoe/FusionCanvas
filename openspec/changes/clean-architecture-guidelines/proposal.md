## Why

FusionCanvas is moving from a single buildable shell toward real domain and workflow behavior. Clear architecture guidelines are needed now so future features grow into stable boundaries instead of mixing UI, application orchestration, domain rules, and external integrations in the same project.

## What Changes

- Emphasize Clean Architecture as the preferred long-term structure for FusionCanvas.
- Emphasize SOLID principles as the default code design guidance for maintainable, efficient implementation without unnecessary abstraction.
- Establish unit testing as a vital architectural practice for every feature.
- Define separate project boundaries for domain, application, integration, and UI layers.
- Clarify dependency direction so outer layers depend inward and domain behavior remains independent of UI frameworks, persistence, marketplace APIs, AI providers, and plugin hosts.
- Update architecture guidance to keep early implementation simple while ensuring new feature work moves toward these boundaries with appropriate tests.

## Capabilities

### New Capabilities
- `architecture-guidelines`: Defines the required architectural layering, SOLID design, and testing expectations for future FusionCanvas implementation work.

### Modified Capabilities

## Impact

- Affects architecture documentation and OpenSpec project guidance.
- May affect future solution structure by introducing or requiring separate projects for domain, application, integration, and UI layers.
- May affect future implementation tasks by requiring appropriate unit tests alongside feature work.
- Does not introduce new runtime behavior, external dependencies, APIs, storage, plugin loading, or marketplace integration behavior.
