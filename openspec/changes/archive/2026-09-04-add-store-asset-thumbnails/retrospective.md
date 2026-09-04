# Retrospective

## What worked

- Keeping preview state on the existing asset row made the feature small and preserved the current list actions.
- A dedicated preview window allowed direct Avalonia headless coverage without coupling store assets to the design-stage preview.

## Lessons

- Image resources owned by refreshed rows need explicit disposal to avoid retaining files and bitmap memory.
- Preview behavior should always use the managed workspace path, matching the asset-management source of truth.

## Follow-up

- Future asset formats can add explicit decoders when platform support and product requirements justify them.
