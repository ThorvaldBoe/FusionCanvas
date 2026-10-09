## 1. Grid integration

- [x] 1.1 Replace repeated Variant rows with the fixed-height aligned virtual grid and preserve the header, count, empty guidance, default order, and creation actions.
- [x] 1.2 Add the view-owned row provider, collection refresh, DataContext rebind/close cleanup, and pointer routing for per-row Archive while retaining keyboard command activation.

## 2. Focused view verification

- [x] 2.1 Add headless coverage for column data and alignment, fixed row height and full-text help, active order, archived exclusion, scrolling/provider refresh, rebind, and close cleanup.
- [x] 2.2 Add headless pointer and keyboard coverage that proves Archive targets the correct row; verify blocked dependency feedback and successful archive/count refresh through existing behavior.

## 3. Acceptance and regression verification

- [x] 3.1 Record each delta scenario's focused test, result, and evidence in `verification.md`; correct any specification or implementation gaps and rerun affected checks.
- [x] 3.2 Run focused App tests, `openspec validate --specs --strict --no-interactive`, strict change validation, and `dotnet test .\\FusionCanvas.sln -m:1`; record results and warnings in `verification.md`.
