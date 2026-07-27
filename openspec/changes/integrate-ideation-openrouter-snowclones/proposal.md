## Why

The independently delivered Ideation, OpenRouter, Snowclone Library, and workspace-transfer modules do not yet form one coherent workflow: a saved OpenRouter key does not enable or power Ideation, persisted snowclones are neither reachable nor used for generation, their placeholder formats disagree, and workspace import can erase durable rejection history. This module makes the merged feature set safe and internally consistent so a creator can configure AI once, manage the vocabulary Ideation actually uses, and retain creative feedback through workspace transfer.

**Module outcome:** A creator can save an OpenRouter inference key in Settings, use the configured Ideation AI profile to generate Basic or persisted-Snowclone candidates, open the Snowclone Library from the Ideation dialog, and export/import a workspace without losing its rejection history or any rejection history already present in the destination installation.

This is one reviewable integration module because every included change closes a boundary in the same Ideation feedback loop: configuration enables generation, the managed library supplies Snowclone templates, candidate rejection creates durable feedback, and workspace transfer preserves that feedback. Provider expansion, prompt history, and broader backup behavior remain separate outcomes.

## What Changes

- Replace the `FUSIONCANVAS_AI_API_KEY` placeholder gate with availability derived from the securely stored OpenRouter credential and the effective Ideation AI profile.
- Replace fake production generation with the provider-neutral `IAiTextGenerationService`, using `AiRequestPurpose.Ideation`; keep deterministic fake collaborators only in tests.
- Keep the Ideation action visible when unavailable and explain whether the missing prerequisite is the OpenRouter key, model catalog/profile, or inaccessible native credential store.
- Make the persisted application-wide Snowclone Library the only production Snowclone source for Ideation.
- Standardize Snowclone variables on brace-delimited placeholders such as `{X}` and `{Audience}`, pass both phrase and guidance into generation, and validate that the AI result no longer contains unresolved placeholders.
- Add a compact `Manage Snowclones…` action to the Snowclones mode in the Ideation dialog. It opens the existing Snowclone Library as an owned modal dialog, returns focus to the invoking action, and refreshes generation availability after library changes.
- Treat an empty Snowclone Library as a recoverable blocked state for Snowclones mode while leaving Basic mode available.
- Include workspace-owned Ideation rejections in workspace export packages, identity preflight, manifest/entity counts, and import merge behavior.
- Preserve all destination rejection history during import and add the imported workspace’s rejection history atomically with its other records.
- Reconcile active OpenSpec language and verification evidence with the shared SQLite v7 schema and the integrated production behavior.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `ideation`: Replace placeholder access and fake production generation with configured OpenRouter-backed generation, and define managed Snowclone behavior and dialog ownership.
- `ai-provider-configuration`: Make saved credential and effective Ideation-profile availability observable to the Ideation workflow without exposing secret material.
- `ai-text-generation`: Define the Ideation caller’s provider-neutral request/result behavior and generation failure handling.
- `snowclone-library`: Make Ideation the owning launcher and production consumer of the application-wide library, with one canonical placeholder contract.
- `workspace-transfer`: Include durable Ideation rejections in the workspace subgraph and preserve destination rejection history during import.
- `local-sqlite-persistence`: Clarify that the combined current schema is v7 and that full-snapshot operations preserve both workspace-owned rejections and global Snowclones.

## Impact

- **Domain:** Extend Snowclone template parsing/substitution support and workspace transfer filtering/preflight for Ideation rejections; no provider types enter Domain.
- **Application:** Add async credential/configuration availability, an OpenRouter-backed Ideation generator adapter over `IAiTextGenerationService`, a persisted Snowclone selection boundary carrying phrase plus guidance, and rejection-aware transfer merge/count behavior.
- **Integration:** Reuse the existing native credential store, OpenRouter adapter, SQLite repositories, and Snowclone repository; no new provider, SDK, secret store, or schema version is required.
- **App:** Compose AI and workspace services through one application lifetime, expose the Snowclone Library only from Snowclones mode, preserve owned-modal and focus behavior, and refresh availability after Settings or library changes without adding persistent main-window controls.
- **Compatibility/data safety:** Existing v7 databases remain v7. Workspace packages newly include rejections; older packages without them continue to import as empty rejection collections. Global Snowclones and OpenRouter credentials remain excluded from workspace packages.
- **Security/privacy:** Credentials remain in native secure storage and are read only by the AI service for dispatch. Prompts contain user-authored creative context and Snowclone guidance but no credential, database identifier, path, timestamp, or operational metadata. Provider failures must not echo secrets.
- **UX:** Ideation remains a frequent Idea-stage action; Snowclone administration remains progressively disclosed in its focused dialog. Blocked, busy, empty-library, provider-error, cancellation, nested-dialog, and focus-return states receive deterministic coverage.
- **Verification:** Add cross-feature application tests, SQLite/package round trips with rejection history, and Avalonia headless tests for key/profile availability and owned Snowclone dialog behavior; run strict OpenSpec validation and the full `dotnet test .\FusionCanvas.sln` baseline.
- **Dependencies:** Builds on the active `add-ideation-tool`, `openrouter-api-configuration`, `snowclone-library`, and `workspace-transfer` changes. Those base changes must be synchronized/archived before this reconciliation change is archived.
- **Non-goals:** Additional AI providers, streaming, automatic retries, web search, image generation, prompt/response persistence, Snowclone tags/categories, whole-application backup, exporting global Snowclones or credentials, and changing candidate approval/rejection UX beyond the integration needed here.
