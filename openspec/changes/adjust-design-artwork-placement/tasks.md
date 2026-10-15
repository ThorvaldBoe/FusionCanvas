## 1. Image processing boundary

- [x] 1.1 Add Application DTOs and the `IArtworkImageProcessor` contract for PNG canvas inspection, non-zero-alpha bounds, and transformed PNG output.
- [x] 1.2 Implement ImageSharp inspection and proportional transform/composite behavior in Integration, preserving source canvas dimensions and alpha.
- [x] 1.3 Add Integration tests for transparent margins, opaque and fully transparent PNGs, semi-transparent edge pixels, proportional resize, clipping, and transparent uncovered pixels.

## 2. Apply use case and persistence

- [x] 2.1 Extend the Design Stage application contract and service with artwork inspection and Apply operations that validate editability, slot assignment, asset availability, and current workspace state.
- [x] 2.2 Save a before-image ReferenceImage and transformed ExportedImage, then persist the replacement slot assignment, links, content-risk review, and applicable provenance consistently in one workspace snapshot.
- [x] 2.3 Handle staged-file and snapshot failures by cleaning new files best-effort and preserving the previous authoritative slot; after commit, clean the replaced file and invalidate dependent mockups.
- [x] 2.4 Register the processor and output-store dependencies in workspace composition and update null/test implementations.
- [x] 2.5 Add Application tests for successful and repeated Apply, before-image copies, transformed output, content-risk review, repository failure cleanup, and read-only rejection.

## 3. Preview and adjustment UI

- [x] 3.1 Replace the Design slot Enlarge text button with a lower-right magnifier action and update accessible naming and tooltip text.
- [x] 3.2 Implement responsive full-canvas preview, checkerboard, visible-bounds overlay, adjustable draft state, source-pixel coordinate mapping, proportional handle behavior, and keyboard movement/resizing.
- [x] 3.3 Implement Apply, Cancel, dirty-close confirmation, read-only and unavailable states, recoverable persistence errors, preview resource disposal, and focus return.
- [x] 3.4 Add Avalonia headless tests for keyboard movement and proportional resizing, reversible draft/Cancel, Apply, and read-only/transparent preview states; retain the existing preview-window construction and binding tests.

## 4. Acceptance verification and completion review

- [x] 4.1 Verify the scenarios in `specs/design-artwork-adjustment/spec.md` against implementation and Integration, Application, or App headless coverage, record untested interaction journeys, and verify retained/changed `asset-management` scenarios.
- [x] 4.2 Review the changed scope for preview-versus-output pixel consistency, atomic asset persistence and cleanup, content-risk/provenance behavior, mockup invalidation, architecture boundaries, keyboard access, and regression risk.
- [x] 4.3 Run strict `openspec validate` and resolve all findings for this change.
- [x] 4.4 Run `dotnet test .\FusionCanvas.sln` and resolve all failures.
