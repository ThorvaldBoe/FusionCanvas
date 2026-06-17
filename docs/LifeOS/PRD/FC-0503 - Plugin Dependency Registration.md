# FC-0503 - Plugin Dependency Registration

## Summary

Plugin Dependency Registration lets plugins provide services to FusionCanvas through stable extension points.

## Requirements

- Plugins can register services for declared capabilities.
- Registered services should integrate through known contracts.
- Core behavior should not depend on a specific plugin.
- Plugin failures should be isolated where practical.
- Service registration should be understandable to contributors.

## Acceptance Criteria

- A plugin can contribute a service to the application.
- The application can use the service through a stable contract.
- A missing plugin does not break unrelated core features.

## Out of Scope

- Arbitrary runtime scripting
- Security sandbox
- Marketplace review process

## Related Notes

- [[Roadmap]]
- [[Architecture]]
- [[FC-0501 - Plugin Manifest]]
