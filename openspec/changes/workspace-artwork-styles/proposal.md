## Why

Creators need a reusable way to carry a deliberate visual style across artwork generations. Today the Design prompt uses Niche visual-style guidance, but it has no selectable style with its own reference images; creators must restate or recreate that guidance for each Item.

## What Changes

- Add workspace-scoped artwork styles with a title, description, and one or more managed reference images, including create, update, and confirmed delete.
- Provide a focused style-management surface reachable from Workspace settings and from an **Add new** shortcut beside the Design-stage style selector.
- Default each Item's style selection to built-in **General**. Selecting a custom style adds its description and reference images to artwork generation; the selected style replaces Niche visual-style guidance while other Niche context remains.
- Support multiple image references for text-to-image generation, gate generation on endpoint reference capacity, and explain when the selected model cannot accept every reference.
- Persist style selection with its Item; when a selected style is deleted, affected Items fall back to General.
- Include styles and their managed reference files in workspace export/import.

## Capabilities

### New Capabilities
- `artwork-style-management`: Workspace-scoped style lifecycle, reference images, and workspace-level management access.
- `design-artwork-style-selection`: Per-Item style selection and style-aware prompt/reference-image generation behavior.

### Modified Capabilities
- `workspace-management`: Provide access to style management for the active workspace.
- `workspace-file-storage`: Associate style references with their owning workspace style and preserve managed-file boundaries.
- `workspace-transfer`: Export and restore styles and their managed reference images.

## Impact

- Application and Domain workspace snapshots, style management use cases, Design prompt compilation and generation orchestration.
- SQLite schema, repository mapping, workspace migrations, and portable workspace package serialization.
- Integration workspace managed-file storage and style reference image import/preview/removal.
- Avalonia Workspace settings/management and Design stage selector/editor surfaces.
- Domain, Application, Integration, and Avalonia headless tests; no new external dependency or live AI service requirement.

This is one module because the management, persistence, transfer, and generation paths all serve one outcome: selecting a reusable workspace style and applying it to artwork generation. The frequent action is choosing a style in Design; infrequent creation and maintenance stay in a focused management surface. Real-desktop Appium coverage is not planned: routed selection, management launch, persistence, and generation gating can be verified deterministically with focused Application/Integration tests and Avalonia headless journeys.
