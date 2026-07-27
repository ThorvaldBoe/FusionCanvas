## Why

FusionCanvas has committed to optional AI-assisted workflows, but it does not yet provide a secure way to configure an AI provider or a provider-independent application service that future tools can use. This module establishes that foundation with OpenRouter while preserving local-first operation, explicit privacy control, and a path to additional providers.

## What Changes

- Add an application-wide `AI` section to the focused Settings window for occasional provider setup without consuming primary-workspace space.
- Support OpenRouter as the only selectable provider in this module while keeping provider-specific contracts out of callers.
- Store the OpenRouter API key in the current user's native credential store on Windows, macOS, and Linux; never place the key in application settings, workspace data, logs, or source control.
- Allow the user to save, replace, remove, and validate the key with explicit status and actionable secure-storage, authentication, key-type, network, and account errors.
- Require Zero Data Retention by default, explain its effect, and allow the user to opt out deliberately.
- Load and cache OpenRouter's text-capable model catalog, filter it by the active privacy policy, retain unavailable saved selections without silently replacing them, and drive known parameter controls from advertised capabilities.
- Provide one `General` AI configuration and an `Advanced` mode with `Ideation` and `Concept` configurations that either inherit General or use retained custom values.
- Provide recognized controls for model, output limit, sampling, stop, seed, and normalized reasoning settings, with `Provider default` represented by omitting the parameter and strict provider routing used for submitted parameters.
- Add a provider-independent, non-streaming text-request application service that resolves the applicable configuration, calls an OpenRouter integration adapter, reports normalized results and failures, and never persists prompts or responses automatically.
- Keep Ideation and Concept assistance interactions, prompt design, generated-content review and persistence, streaming, structured-output workflows, tools, web search, text-to-image, multiple configured providers, and plugin-provided AI adapters out of scope.
- Verify all routine behavior deterministically with fake provider and credential collaborators; native credential-store checks run on their matching operating systems without requiring a live OpenRouter key, and any optional live provider check remains supplemental.

This is one coherent and reviewable delivery module because the Settings surface, credential boundary, catalog-driven profiles, and text service must agree on one configuration contract before any user workflow can safely invoke AI. Its outcome is independently observable: a user can securely configure and validate OpenRouter, and application callers can submit a profile-resolved text request through a provider-neutral boundary.

## Capabilities

### New Capabilities

- `ai-provider-configuration`: Secure OpenRouter credential management, privacy policy, dynamic text-model selection, capability-aware parameters, profile inheritance, caching, validation, and configuration availability.
- `ai-text-generation`: Provider-independent text requests, profile resolution, OpenRouter request translation, normalized results, cancellation, and actionable failures.

### Modified Capabilities

- `application-settings`: Add the focused AI section and its progressive-disclosure, keyboard, draft, destructive-action, persistence, and error-state behavior.

## Impact

- **Application:** New AI request/result, model-catalog, configuration, credential, and text-service ports and policies. The Domain layer remains independent of AI providers.
- **Integration:** OpenRouter HTTP adapters, model-catalog cache, and native credential-store adapters for Windows Credential Manager, macOS Keychain, and Linux Secret Service/libsecret.
- **App:** A new Settings section with masked credential entry, connection status, privacy controls, searchable model selection, capability-aware parameters, and General/Ideation/Concept profile editing.
- **Persistence and compatibility:** Non-secret AI preferences are application-wide and versioned in local application settings, not workspace SQLite. Existing appearance settings remain readable. Secrets are addressed by stable credential-store identifiers and are not copied into settings migrations or exports.
- **Dependencies:** Requires an audited .NET 10-compatible approach to all three native credential stores and normal `HttpClient`/JSON support for OpenRouter. No live external service is added to the deterministic test baseline.
- **Primary risks:** Security-sensitive native storage behavior across three operating systems; changing remote model metadata; privacy-policy/model incompatibility; unsupported parameters being ignored by downstream endpoints; duplicate cost from unsafe retries; and Settings complexity. The design must fail closed for secret persistence, use strict parameter routing, preserve explicit selections, avoid automatic generation retries, and cover meaningful Settings behavior with framework-free and Avalonia headless tests.
- **Delivery dependency:** The current basic product workflow and active maintenance changes remain separate. This module does not alter stage content or depend on an Ideation or Concept assistance design.
