# Design

## Implementation plan

1. Extend the application asset summary with the resolved managed file path so the presentation layer can load the authoritative workspace copy.
2. Let each asset row create a disposable bitmap thumbnail for supported image extensions, exposing whether preview is available while retaining existing state and commands.
3. Add a compact thumbnail button to the asset list and a dedicated preview window bound to the row bitmap.
4. Dispose row bitmaps when the asset list is refreshed and verify window construction and thumbnail binding through an Avalonia headless test.

## Decisions

- The managed workspace copy is the preview source; original source paths are not used.
- Preview is intentionally read-only and in-app.
- Non-image, missing, and unreadable assets have no thumbnail but continue to render their existing metadata and actions.
