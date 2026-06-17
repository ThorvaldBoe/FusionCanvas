# FC-0109 - Import Existing Assets

## Summary

Import Existing Assets lets creators copy files into the FusionCanvas workspace and attach them to the work they support.

Phase 1 is focused on preserving context, not processing files. The user should be able to import source files, exports, mockups, references, textures, and fonts into the managed workspace, then connect those assets to listings or broader workspace contexts so they are not lost in disconnected folders.

## User Need

As a Print on Demand creator, I need to import existing files into FusionCanvas and attach them to listings or related contexts so I can find the assets that support a product concept later.

## Goals

- Let users attach existing assets to listings.
- Allow assets to support broader contexts where useful.
- Make related assets visible from the relevant work.
- Preserve asset relationships when work moves.
- Avoid advanced asset automation in Phase 1.

## Requirements

- The user can import an existing file as an asset.
- Importing a file copies it into the managed FusionCanvas workspace.
- The user can associate an asset with a listing.
- The user can associate an asset with a broader context where useful.
- The user can see which assets belong to a listing.
- The user can identify basic asset purpose.
- Asset relationships should remain intact when listings or groups move.
- Existing assets should be useful even when the original file was created outside FusionCanvas.
- FusionCanvas should use the managed workspace copy after import, not the original source path.

## Asset Purposes

Phase 1 should support basic asset purpose labels such as:

- source file
- export
- mockup
- reference
- texture
- brush
- font

## User Workflows

### Attach a Design File

The user imports an existing design source file or exported image to a listing.

FusionCanvas copies the file into the managed workspace. The listing should then show that supporting asset as part of its context.

### Attach Reference Material

The user imports a reference image, texture, font, or brush to a listing, niche, group, or store where it provides useful context.

### Review Listing Assets

The user opens a listing and sees the managed workspace files associated with it.

This should help the user understand what already exists and what still needs to be created.

## Acceptance Criteria

- A user can import an existing design file to a listing.
- A user can import reference material or exported artwork without losing where it belongs.
- A user can open a listing later and see the files that support it.
- Imported files are copied into the managed FusionCanvas workspace.
- Moving a listing does not break its asset relationships.
- Assets can be labeled by basic purpose.

## Out of Scope

- Batch asset import
- Automatic file matching
- Image processing
- Mockup generation
- Cloud storage sync
- Asset deduplication
- Versioned asset history
- Asset validation

## Open Questions

- Should assets attach only to listings in Phase 1, or also to stores, niches, and groups?
- Should missing files be detected in Phase 1?

## Related Notes

- [[Phase 1 - MVP Creative Workspace]]
- [[Roadmap]]
- [[Product]]
- [[Data Model]]
