# Verification

| Acceptance criterion | Evidence |
| --- | --- |
| Existing image assets show thumbnails | `AssetsPreviewWindowTests.PreviewWindow_DisplaysAssetThumbnail` passed; `AssetRowViewModel` loads the managed image path. |
| User opens an enlarged asset preview | `AssetPreviewWindow` binds the row thumbnail and closes without mutating the asset. |
| Preview is unavailable safely | Thumbnail creation returns no bitmap for missing, unreadable, and non-image files; existing row metadata/actions remain unchanged. |
| Regression baseline | Full `dotnet test .\\FusionCanvas.sln --no-restore -m:1 -nr:false -v q` passed: 1,464 tests, 0 failed. |
| Specification integrity | `openspec validate add-store-asset-thumbnails --strict` passed after sync. |
