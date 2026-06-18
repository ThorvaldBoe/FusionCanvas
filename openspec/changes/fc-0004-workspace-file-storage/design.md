## Context

FusionCanvas is in Phase 0 and is establishing the local-first foundation that later creative workspace features will depend on. FC-0002 defines the initial domain concepts, and FC-0003 defines SQLite as the structured storage direction. FC-0004 defines the complementary file boundary: large creative files remain normal local files, while FusionCanvas stores durable references and context around managed workspace copies.

The implementation should preserve Clean Architecture boundaries. Domain and application code can describe file references and use cases, but concrete path creation, file copying, and file existence checks belong in integration code behind application-facing contracts.

## Goals / Non-Goals

**Goals:**

- Define a managed workspace file area for FusionCanvas-owned copies of creative files.
- Copy imported files into the managed workspace and treat that copy as authoritative.
- Represent workspace file references without embedding large binary content in structured storage.
- Support practical Print-on-Demand asset categories while keeping the model extensible.
- Associate workspace files with creative context such as stores, niches, groups, listings, designs, assets, prompts, and future related records.
- Detect whether a managed workspace file reference points to an existing file.
- Cover domain, application, and integration behavior with focused tests.

**Non-Goals:**

- No batch import workflow, drag/drop import UI, or import wizard.
- No file deduplication, content hashing requirement, or version history.
- No cloud sync, remote storage, backup/restore, or collaborative file access.
- No image processing, thumbnail generation, mockup generation, or format conversion.
- No automatic missing-file repair or relinking workflow.
- No marketplace export package builder.

## Decisions

### Use workspace-relative references at the application boundary

Managed files should be identified by paths relative to the configured workspace file root. Relative references keep structured records portable if the workspace root moves and prevent domain/application contracts from depending on machine-specific absolute paths.

Alternative considered: store only absolute paths. That is simpler to inspect, but it makes local data brittle when the user moves the workspace folder or restores data onto another machine.

### Keep file-system operations in integration code

The application layer should define contracts for importing and checking managed files. The integration layer should implement those contracts using local file-system APIs.

Alternative considered: let domain entities copy files directly. That would couple business concepts to I/O and make unit testing harder.

### Treat the managed copy as authoritative after import

After import, FusionCanvas should use the managed workspace copy rather than the original source path. Original source path may be preserved as metadata for traceability, but workflows must not depend on it.

Alternative considered: keep references to original files in place. That preserves the user's existing organization but makes FusionCanvas behavior fragile when files are renamed, moved, or deleted outside the application.

### Start with simple asset categories

The first model should use a small set of asset categories that cover Phase 0 and near-term Phase 1 needs: source design, exported image, SVG, mockup, reference, texture, brush, font, prompt output, external link, and unknown/other where needed.

Alternative considered: create a deep file taxonomy now. That would overfit before import, design, listing, and export workflows have clarified their exact needs.

### Record missing-file state without repair behavior

The file store should be able to report that a managed workspace file is missing. Repair, relinking, search, and recovery workflows should be specified later.

Alternative considered: implement repair immediately. Missing-file recovery is useful, but it brings UI and workflow decisions that are outside the foundation scope.

## Risks / Trade-offs

- [Risk] Workspace folder organization may not match future user-facing expectations -> Mitigation: keep physical layout simple and hide layout-specific assumptions behind integration code.
- [Risk] Relative paths can be unsafe if not normalized -> Mitigation: validate imported and referenced paths so they cannot escape the managed workspace root.
- [Risk] File category names may need refinement as Phase 1 import expands -> Mitigation: keep categories coarse and include an unknown/other fallback.
- [Risk] Missing-file detection may be mistaken for full repair support -> Mitigation: explicitly limit this change to status detection and leave repair workflows for later specs.
- [Risk] Existing structured persistence may need to store file references before the SQLite spec is complete -> Mitigation: keep file reference types persistence-neutral and let FC-0003 own database mapping details.
