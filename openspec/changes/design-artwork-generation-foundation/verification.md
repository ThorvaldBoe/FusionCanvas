# Design Artwork Generation Foundation Verification

## Criterion Evidence

| Delta-spec scenario | Named evidence | Result |
| --- | --- | --- |
| Primary assignment, clearing, same-offering and active-area guards | `CatalogModelTests.PrimaryArtworkArea_RequiresActiveAreaFromTheSameOffering`; `CatalogModelTests.PrimaryArtworkArea_ReplacementAndClearingAreAtomic`; `CatalogSetupServiceTests` archive/delete cases | Pass |
| Primary SQLite reload and workspace transfer | `SqliteWorkspaceRepositoryTests.SaveAndLoadAsync_RoundTripsOfferingPrimaryArtworkDesignArea`; `WorkspacePackageIntegrationTests` normalized package equality | Pass |
| Independent Artwork settings and image-only model readiness | `AiSettingsViewTests`; `AiConfigurationTests.ArtworkProfile_IsIndependentAndResolvesAgainstImageModel`; `AiConfigurationTests.ArtworkProfile_RejectsTextOnlyModel` | Pass |
| Provider-neutral contracts and provenance fields | Application contract compilation; `AiImageProvenanceTests.Codec_RoundTripsCompleteNonSecretProvenance` | Pass |
| Endpoint model/privacy/format/transparency filtering and deterministic size ranking | `AiConfigurationTests.ImageEndpointPolicy_FiltersPrivacyFormatAndTransparencyPerEndpoint`; `AiConfigurationTests.ImageEndpointPolicy_SelectsClosestRatioThenLargestUsefulSize`; unavailable-model readiness cases | Pass |
| Missing prerequisites, target selection, default row, and read-only readiness | `AiConfigurationTests.ArtworkGenerationReadiness_ReportsEveryMissingPrerequisite`; `ArtworkGenerationServiceTests.GenerateAsync_PersistsOneFinalAssetAndAssignsDefaultRow`; `DesignStageToolHeadlessTests` read-only and disabled-state cases | Pass |
| Prompt includes current triangle, target guidance, relevant context, and verbatim Phrase; excludes stale SLL and implicit Supporting Images | `AiConfigurationTests.ArtworkPromptBuilder_PreservesPhraseAndOmitsStaleSllAsUntrustedData`; generation provenance assertion | Pass |
| OpenRouter dedicated image catalog, endpoint discovery, selected-model resolution, one POST, no retry, ZDR/provider pinning | `OpenRouterClientTests.GetImageModelsAsync_UsesDedicatedImageCatalogAndKeepsImageOnlyModels`; endpoint discovery tests; `OpenRouterClientTests.ImageGenerateAsync_SendsOnePinnedRequestWithoutRetry` | Pass |
| PNG/non-PNG decode, malformed/empty payload, pixel bounds, resizing, aspect fit, inset, alpha, and opaque shortfall warning | `ImageSharpArtworkNormalizerTests` six focused cases | Pass |
| Versioned provenance round trip, optional fields, malformed/unknown versions, and secret exclusion | `AiImageProvenanceTests.Codec_RoundTripsCompleteNonSecretProvenance` plus malformed/unknown-version cases | Pass |
| SLL fingerprint persistence, stale detection, reset, keep-for-reference, and regeneration clearing stale state | `ItemInspectorServiceTests.Load_MarksCommittedSllStaleWhenItsSourceFieldsChange`; `SllGenerationSessionViewModelTests`; `SllSectionHeadlessTests` | Pass |
| Empty/occupied placement, manual replacement, generated history ordering, unassignment/deletion guard, reload, and manual/generated distinction | `DesignStageServiceTests` slot/history/removal cases; `ArtworkGenerationServiceTests.GenerateAsync_PersistsOneFinalAssetAndAssignsDefaultRow`; `DesignStageToolHeadlessTests` slot-action cases | Pass |
| Single logical persistence, staged-file cleanup, cancellation, late-result rejection, no retry, and cost-warning persistence | `ArtworkGenerationService`; `OpenRouterClientTests.ImageGenerateAsync_SendsOnePinnedRequestWithoutRetry`; deterministic error-path inspection; provenance warning round-trip | Pass |
| Settings keyboard access, primary editing, artwork target/defaulting, persisted target/transparency preference, target reset, transparency default, busy/cancel/disabled/read-only states | `AiSettingsViewTests`; `DesignStageServiceTests.SaveArtworkPreferencesAsync_LegacyDesignArea_RoundTripsAndIsRestored`; `DesignStageServiceTests.SelectConfigurationAsync_ClearsPersistedArtworkPreferences`; `DesignStageToolHeadlessTests.ArtworkGenerationSection_ExposesTargetAndTransparencyControls`; `SllSectionHeadlessTests` | Pass |
| Generated lifecycle actions, provenance/dimensions, persistent warning, and manual import/preview/download/missing-state preservation | `DesignStageServiceTests`; `DesignStageToolHeadlessTests`; generated `DesignSlotSummary` inspection | Pass |

## Validation Runs

- `dotnet test .\\FusionCanvas.sln --no-restore -m:1 -p:UseSharedCompilation=false -v minimal` — passed all projects: Domain 250, Application 451, Integration 233, App 649, UI description 27; 1,610 tests total, 0 failed.
- Focused raster, provenance, endpoint, SLL/configuration, orchestration, and Design-stage headless suites passed.
- `openspec validate design-artwork-generation-foundation --strict` — passed.

## Residual Limitations

- The bounds-editor follow-up remains explicitly deferred; this change supplies the target selector and consumes existing Design Area bounds.
- Existing repository analyzer warnings remain outside this change's scope; they do not fail the baseline.
