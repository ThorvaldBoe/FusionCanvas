## 1. Domain File Reference Model

- [ ] 1.1 Review existing `FusionCanvas.Domain.Workspace` asset concepts and choose the smallest extension needed for managed workspace file references.
- [ ] 1.2 Add or update asset category modeling for source design files, exported images, SVG files, mockup images, reference images, textures, brushes, fonts, prompt outputs, external links, and unknown or other resources.
- [ ] 1.3 Add workspace-relative file reference behavior that rejects empty paths and paths that escape the managed workspace boundary.
- [ ] 1.4 Add metadata needed to connect a managed file reference to creative context without introducing advanced asset workflow entities.

## 2. Application Contracts and Use Cases

- [ ] 2.1 Define an application-facing contract for importing a local file into managed workspace storage.
- [ ] 2.2 Define an application-facing contract for checking whether a managed workspace file reference exists.
- [ ] 2.3 Ensure imported file results expose workspace-relative reference data and do not require downstream code to depend on the original source path.
- [ ] 2.4 Keep file storage contracts independent of Avalonia, SQLite provider details, marketplace APIs, AI providers, and plugin storage.

## 3. Local File-System Implementation

- [ ] 3.1 Implement the managed workspace root creation or validation needed by the local file store.
- [ ] 3.2 Copy imported local files into managed workspace storage and return the managed copy as authoritative.
- [ ] 3.3 Normalize and validate destination references so managed files cannot be written outside the workspace file boundary.
- [ ] 3.4 Implement missing-file detection for managed workspace file references without automatic repair or relinking behavior.
- [ ] 3.5 Preserve original source path only as optional traceability metadata, not as the path FusionCanvas depends on after import.

## 4. Tests

- [ ] 4.1 Add `FusionCanvas.Domain.Tests` coverage for asset categories, workspace-relative references, invalid paths, and creative context metadata.
- [ ] 4.2 Add `FusionCanvas.Application.Tests` coverage for file storage contracts or use case behavior at the application boundary.
- [ ] 4.3 Add `FusionCanvas.Integration.Tests` coverage for local file import copying, authoritative managed paths, workspace boundary enforcement, and missing-file detection.
- [ ] 4.4 Add tests or assertions proving large file bytes are not embedded in structured data by the file storage model.
- [ ] 4.5 Add tests or assertions that batch import, deduplication, cloud sync, image processing, mockup generation, automatic repair, file version history, and marketplace export packaging are not introduced.

## 5. Validation

- [ ] 5.1 Run `dotnet test` for the solution or affected test projects.
- [ ] 5.2 Run `openspec status --change "fc-0004-workspace-file-storage"` and verify the change is apply-ready.
- [ ] 5.3 Update task checkboxes to reflect completed implementation work before archive.
