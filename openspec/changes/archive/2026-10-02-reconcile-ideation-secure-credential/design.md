## Design

The current production composition is the authoritative implementation evidence: `AppWorkspaceFactory` constructs `ConfiguredIdeationAccessStatus` over the provider-neutral `IAiTextGenerationService`. The accepted `ideation` specification will be synchronized from the narrow delta below so it describes secure credential/profile readiness and explicitly treats the legacy environment variable as non-authoritative.

No application or integration code changes are needed. The regression test reads the accepted requirement and composition boundary so a later reintroduction of the placeholder contract cannot pass unnoticed.

## Implementation Plan

1. Add the modified Ideation availability requirement and scenarios to the change delta.
2. Add the focused regression test and confirm it fails against the pre-reconciliation accepted spec.
3. Run the sanctioned OpenSpec archive/sync operation for this completed, narrow change.
4. Run focused tests, strict validation, and the affected build/test scope.

## Decisions not to reopen

- Secure native OpenRouter credential plus effective Ideation profile remains the availability source.
- The environment placeholder is not a production access path.
- The broader integration change remains active and is not archived by this issue.
