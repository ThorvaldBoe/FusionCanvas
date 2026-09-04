# Keep Integration adapters in the App composition root

## Why

The main-window presentation layer currently constructs workspace file/transfer adapters and CSV codecs directly. This couples view construction to concrete Integration implementations and makes composition boundaries harder to test.

## Scope

- Move workspace runtime services, transfer dependencies, raster metadata, SLL codec, and item CSV codecs into App composition factories.
- Inject application-facing contracts into `MainWindow` and `MainWindowViewModel`.
- Preserve existing runtime behavior and test seams with explicit non-Integration defaults for manually constructed view models.

## Non-goals

- No change to transfer, import/export, persistence, or UI behavior.
- No new dependency-injection framework.

## Verification

- Focused composition tests verify the factory supplies the runtime and codec contracts.
- Source scan confirms presentation files do not instantiate Integration adapters.
- Full build and relevant App tests pass.

## Modified Capabilities

- `architecture-guidelines`: require the App composition root to own concrete Integration adapter construction.
