## Why

FusionCanvas needs a clear local file storage boundary before Phase 1 import, design, mockup, and listing workflows begin attaching large creative assets to structured records. Defining this now preserves local ownership while preventing large binary files from becoming embedded database content.

## What Changes

- Introduce a managed local workspace file area for creative assets used by FusionCanvas.
- Treat imported creative files as copied workspace files, with the managed copy becoming the authoritative file used by the application.
- Store structured references to workspace file paths and file context rather than embedding large binary content in SQLite.
- Define baseline asset file categories for source files, exported images, SVGs, mockups, references, textures, brushes, fonts, prompt outputs, and external links.
- Allow workspace file references to be associated with creative entities such as stores, niches, groups, listings, designs, assets, prompts, or future related records.
- Establish minimum behavior for missing or moved workspace files without implementing full repair workflows.
- Keep batch import, deduplication, sync, cloud storage, image processing, mockup generation, automatic repair, and file version history out of this change.

## Capabilities

### New Capabilities
- `workspace-file-storage`: Defines local workspace file storage expectations for managed creative files, authoritative workspace copies, structured path references, entity associations, asset categories, and missing-file boundaries.

### Modified Capabilities

## Impact

- Affected code: `FusionCanvas.Domain` may need file reference value objects or metadata types, `FusionCanvas.Application` will need file storage contracts/use cases, and `FusionCanvas.Integration` will need a local file-system implementation when this change is implemented.
- Affected tests: `FusionCanvas.Domain.Tests`, `FusionCanvas.Application.Tests`, and `FusionCanvas.Integration.Tests` should cover file reference behavior, copy/import boundaries, path validation, association metadata, and missing-file handling.
- Affected specs: a new `workspace-file-storage` capability will become accepted behavior after implementation and archive.
- Dependencies: uses local file-system APIs available to .NET; no cloud service, file sync provider, image processing library, batch importer, marketplace SDK, AI provider, or plugin host contract is required.
