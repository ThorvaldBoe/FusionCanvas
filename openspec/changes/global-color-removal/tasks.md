## 1. Application contracts and source eligibility

- [x] 1.1 Add global color-removal request, parameter, preview, apply-result, and failure models with normalized RGB/tolerance validation.
- [x] 1.2 Add the narrow application-facing raster processor contract for preview-mask and transparent-PNG generation without exposing ImageSharp types inward.
- [x] 1.3 Add application tests for invalid item/source/read-only eligibility, exact tolerance bounds, and cancellation before any durable mutation.

## 2. Deterministic raster processing

- [x] 2.1 Implement the ImageSharp processor with bounded decode checks, unpremultiplied sRGB Euclidean distance, global matching, zero-alpha preservation, and cancellation checks.
- [x] 2.2 Implement preview overlay/mask output and final binary alpha-removal PNG output, including match counts and no-match/all-visible outcomes.
- [x] 2.3 Add focused Integration tests for exact matches, tolerance expansion, disconnected and in-artwork matches, transparency, alpha preservation, malformed input, and PNG output.

## 3. Derived asset persistence

- [x] 3.1 Implement the application service that resolves an eligible Item-linked Design/supporting image and produces an in-memory preview without persistence.
- [x] 3.2 Implement Apply as a derived same-kind managed PNG asset/link operation that preserves the source, refreshes authoritative state, and identifies the new result.
- [x] 3.3 Add best-effort cleanup and recoverable error handling for output-file creation, snapshot persistence, advisory review, and cancellation boundaries.
- [x] 3.4 Add isolated Application/Integration persistence tests covering source preservation, derived relationship reload, output cleanup after save failure, and output-size/format limits.

## 4. Design-stage editor

- [x] 4.1 Add a focused global color-removal view model and connect it to eligible Design image actions while preserving read-only and missing-source behavior.
- [x] 4.2 Add the Avalonia color-removal surface with source preview, picker, tolerance control, removal overlay, match/status guidance, warning, Apply, Cancel, busy, and recoverable error states.
- [x] 4.3 Wire generation guards/debounced preview refresh, cancellation, disposal, keyboard focus, and post-Apply/post-Cancel focus or selection behavior.
- [x] 4.4 Register the new application service and processor in the existing composition root without introducing a parallel workspace or asset boundary.

## 5. User-facing verification

- [x] 5.1 Add view-model tests for initial no-color state, picker/tolerance updates, global-removal warning, no-match/all-visible blocking, busy-state duplicate prevention, Apply success, Cancel, and error recovery.
- [x] 5.2 Add deterministic Avalonia headless tests for rendered control discovery, routed picker/tolerance interaction, overlay state, Apply/Cancel transitions, keyboard reachability, and focus/selection outcomes.
- [x] 5.3 Record the explicit decision that no Appium scenario is warranted because the module introduces no native-window or OS-specific seam, while retaining lower-layer and headless coverage.

## 6. Acceptance and delivery gates

- [x] 6.1 Reconcile implementation and tests against every scenario in `specs/global-color-removal/spec.md`, correcting the change artifacts if an approved requirement is underspecified.
- [x] 6.2 Run strict OpenSpec validation and resolve all change-artifact diagnostics.
- [x] 6.3 Run the deterministic solution baseline `dotnet test .\FusionCanvas.sln -m:1` and the focused changed-scope tests.
- [x] 6.4 Capture criterion-level results and evidence in the verification artifact before archiving the completed change.
