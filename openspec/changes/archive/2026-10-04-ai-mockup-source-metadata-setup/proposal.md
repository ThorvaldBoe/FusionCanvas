## Why

Mockup source-image setup still requires repetitive per-image configuration even after the source-image table gained multi-selection. Creators need a fast, reviewable way to populate likely Color, Size, and placement metadata for several selected images while retaining control over the final draft.

This module follows issue [#726](https://github.com/ThorvaldBoe/FusionCanvas/issues/726) and builds on the source-image multi-selection delivered by #724. It is intentionally draft-first: AI assistance should reduce repetitive setup without creating hidden persistence or making AI necessary for ordinary manual editing.

## What Changes

- Add a **Set up metadata with AI** action to the focused Mockup Template source-image editor.
- Process all selected new or existing local source-image drafts in one user operation and return an individual result for each image.
- Build requests from filenames, dimensions, current per-image metadata, available Offering Color and Size values, existing source-image metadata and mappings, and the selected Template's target Design Area context.
- Use filename and existing metadata first. Send image content to the configured AI provider only for selected images whose unresolved metadata requires visual inspection and whose configured General AI model supports image input.
- Follow the existing AI Settings privacy configuration, including the user's Zero Data Retention preference, for any AI request.
- Apply usable suggestions to the editable draft immediately; preserve the existing Mockup Template Save workflow as the persistence boundary.
- Surface per-image success, uncertainty, and failure states without discarding existing metadata or blocking successful results for other selected images.
- Reuse an existing compatible source-image mapping for placement. If no suitable mapping exists or references conflict, leave placement unchanged and mark it for review.
- Keep the feature optional and provider-independent; provider-specific request and response handling remains behind application AI contracts.

## Capabilities

### New Capabilities

- `mockup-source-metadata-assistance`: User-invoked, batch AI assistance for local Mockup Template source-image metadata, including conditional image analysis, draft application, uncertainty, and partial-failure behavior.

### Modified Capabilities

- None. The existing `mockup-template-source-images` capability remains the authority for source-image ownership, applicability, mappings, revisions, and manual editing. This module adds an optional assistance action over that existing draft workflow.

## Impact

- Application: new Mockup metadata-assistance request/result contracts, orchestration, structured response validation, deterministic option-value mapping, conditional image-input selection, and placement-reference selection.
- AI boundary: extend the provider-independent AI contract to support image-bearing messages or an equivalent vision request while preserving General profile resolution and ZDR settings.
- Integration: extend the OpenRouter adapter and mock twin for image-input requests without exposing credentials or provider details to the UI.
- App: inject the assistance service into the Mockup Template editor, add action/loading/status presentation, and apply results to selected draft rows without direct persistence or provider logic.
- Tests: application tests for request assembly, parsing, confidence and partial failures; integration adapter tests for multimodal serialization; Avalonia headless tests for action availability, draft application, status presentation, and no-selection/provider-unavailable states.
- No database migration or new persisted AI-result model is planned. AI suggestions remain transient until the user saves ordinary source-image metadata.
