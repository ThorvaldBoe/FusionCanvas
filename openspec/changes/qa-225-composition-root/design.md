# Design

## Context

`AppServicesFactory` and `AppWorkspaceFactory` are the composition boundary. `MainWindow` and `MainWindowViewModel` should coordinate presentation and application services, not choose concrete file, package, image, CSV, or document adapters.

## Decisions

- Extend `AppWorkspaceRuntime` with the composed workspace transfer and raster metadata contracts.
- Extend `AppServices` with the composed item CSV codec contracts; keep workspace creation in the dedicated `AppWorkspaceFactory` so settings startup remains independent of workspace database startup.
- Keep direct-construction test compatibility using App-owned null implementations that fail clearly when an operation requires production configuration.
- Keep Avalonia storage-provider picker construction in `MainWindow`, because it is a UI adapter owned by the App layer rather than an Integration adapter.

## Implementation Plan

1. Compose transfer, package, file-store, raster metadata, SLL, and CSV adapters in App factories.
2. Inject composed contracts through `AppServices` and `AppWorkspaceRuntime`.
3. Remove concrete Integration construction from main-window presentation types.
4. Add focused composition/source-boundary verification and run strict OpenSpec validation, build, and App tests.

## Risks and Mitigations

- Runtime startup wiring can be incomplete: factory-level tests and a full solution build cover constructor wiring.
- Direct test construction can lose behavior: explicit injected collaborators remain supported; null defaults produce clear errors for unconfigured operations.
