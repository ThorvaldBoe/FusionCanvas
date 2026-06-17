# FC-0004 - Workspace File Storage

## Summary

Workspace File Storage defines how FusionCanvas thinks about large files such as source files, exports, mockups, references, textures, brushes, fonts, and generated assets.

Phase 0 should establish that files remain files, while FusionCanvas stores enough workspace paths and context to keep them connected to creative work.

## User Need

As a creator, I need FusionCanvas to copy my design files into a managed local workspace and connect them to the right work without hiding them inside an opaque system.

## Goals

- Preserve local ownership of creative files.
- Keep large assets outside the structured database.
- Let future features attach files to stores, niches, groups, listings, or designs.
- Support predictable workspace organization.
- Avoid early overengineering of asset management.

## Requirements

- FusionCanvas treats large creative assets as files.
- Imported creative files are copied into the FusionCanvas-managed workspace.
- The managed workspace copy is the authoritative asset used by FusionCanvas.
- Structured data stores workspace paths to files rather than embedding large files directly.
- The product has a clear concept of a workspace location or file storage boundary.
- File paths can be associated with creative entities.
- The storage approach should support common Print on Demand asset types.
- Missing or moved file scenarios should be considered even if full repair workflows come later.
- File storage should support future import, export, and asset relationship features.

## Asset Types to Consider

- source design files
- exported PNGs
- SVG files
- mockup images
- reference images
- textures
- brushes
- fonts
- prompt outputs
- external links

## User Workflows Supported

### Keep Files Local

The user can keep source and export files accessible on disk.

FusionCanvas should not require the user to surrender control of their files.

When a file is imported, FusionCanvas copies it into its managed workspace. The original file can remain wherever the user keeps it, but FusionCanvas does not depend on that original path after import.

### Connect Files to Context

The user should eventually be able to connect a copied workspace file to the store, niche, group, listing, or other context it supports.

### Prepare for Asset Import

The foundational file storage model should make Phase 1 asset import possible without redesign.

## Acceptance Criteria

- The project has a clear file storage expectation.
- Large binary assets are not treated as structured database content.
- Managed workspace file paths can be connected to future asset records.
- The storage model supports Phase 1 import of existing assets.
- The approach remains local-first.

## Out of Scope

- Batch asset import
- Asset deduplication
- File sync
- Cloud storage
- Image processing
- Mockup generation
- Automatic file repair
- File version history

## Open Questions

- Should file organization be visible and user-friendly on disk?
- What minimum missing-file behavior is needed for Phase 1?

## Related Notes

- [[Phase 0 - Foundation]]
- [[Roadmap]]
- [[Architecture]]
- [[Data Model]]
- [[FC-0109 - Import Existing Assets]]
