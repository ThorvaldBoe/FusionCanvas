# Add clickable store asset thumbnails

## Why

The store asset surface currently exposes filenames and metadata only, which makes it difficult to identify visual assets quickly. Adding compact thumbnails and an in-app enlarged preview improves browsing without changing asset storage or lifecycle behavior.

## Scope

### Included

- Show thumbnails for supported image assets whose managed file exists.
- Make a thumbnail clickable to open an enlarged in-app preview.
- Preserve the existing filename, purpose, context, missing-file, and removal behavior.

### Non-goals

- Editing, replacing, importing, or deleting image contents.
- Changing supported import types or asset persistence semantics.
- Adding previews for non-image assets.

## Risks and verification

Bitmap resources must be disposed when rows are replaced, and missing or unreadable files must remain safe. Verify with an Avalonia headless preview test, a project build, the full solution test baseline, and strict OpenSpec validation.
