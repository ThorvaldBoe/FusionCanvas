## 1. Discovery and contract closure

- [x] 1.1 Inventory every Design mutation that can change an output's source asset or assignment and map its reverse dependency to Listing mockups.
- [x] 1.2 Resolve the three proposal decisions and update proposal/design/spec scenarios before implementation.
- [x] 1.3 Define the output lifecycle contract and failure diagnostics for preview, save-copy, removal, and invalidation.

## 2. Application and persistence behavior

- [x] 2.1 Add focused application contracts for listing mockup consumption and derived-output invalidation.
- [x] 2.2 Implement safe preview/read and save-copy behavior through managed workspace file ports.
- [x] 2.3 Implement confirmed output removal with atomic Asset/AssetLink persistence and best-effort file cleanup.
- [x] 2.4 Implement precise metadata-based invalidation with item-wide fallback when mapping is not trustworthy.
- [x] 2.5 Integrate invalidation into all affected Design mutation paths and preserve source assets.

## 3. Listing experience

- [x] 3.1 Replace filename-only output presentation with a thumbnail gallery and attribution.
- [x] 3.2 Add enlarged preview, save-copy, removal confirmation, empty, missing, busy, and stale-result states.
- [x] 3.3 Preserve read-only policy, keyboard reachability, active-item isolation, and focus restoration.

## 4. Verification and delivery gates

- [x] 4.1 Add application and Integration tests for lifecycle, cleanup, missing files, protection, and reverse dependencies.
- [x] 4.2 Add Avalonia headless tests for gallery/control state and interaction wiring; add a real-desktop journey only if justified by residual risk.
- [x] 4.3 Map every acceptance scenario to evidence in `verification.md` and correct failed criteria rather than hiding them behind aggregate passes.
- [x] 4.4 Run strict OpenSpec validation and `dotnet test .\FusionCanvas.sln`.
