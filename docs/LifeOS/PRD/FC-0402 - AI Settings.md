# FC-0402 - AI Settings

## Summary

AI Settings let users configure provider access, preferred behavior, and privacy boundaries.

## Requirements

- Users can configure AI provider credentials or connection settings.
- Users can bring their own API key for supported providers where the provider allows it.
- Users can choose preferred providers or models where available.
- Users can understand which workflows may send data to AI services.
- Settings can include default behavior for prompts, outputs, and approvals.
- Sensitive settings should be handled deliberately.
- AI-assisted workflows remain optional; manual workflows should continue to work when no provider is configured.

## Acceptance Criteria

- A user can configure an AI provider.
- A user can choose default AI behavior.
- A user can understand privacy implications before using AI.
- A user can use non-AI workflows when no API key or provider is configured.

## Out of Scope

- Enterprise policy management
- Team-wide settings
- Provider billing

## Related Notes

- [[Roadmap]]
- [[Architecture]]
- [[Principles]]
