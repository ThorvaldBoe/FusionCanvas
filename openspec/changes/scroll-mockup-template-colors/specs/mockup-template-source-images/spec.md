## MODIFIED Requirements

### Requirement: Local source-image setup has a focused, accessible master-detail editor workflow

FusionCanvas SHALL provide local source-image setup inside the focused Mockup Template dialog. An upper image table SHALL expose upload, selection, archive, applicability and mapping summaries, and complete/incomplete indicators. A lower selected-image editor SHALL expose applicability controls, preview, and that entry's mapping independently of upload. The placement editor SHALL also expose the accessible **Keep aspect ratio** option defined by the `mockup-placement-aspect-ratio` capability. The dialog SHALL preserve meaningful unsaved work on cancellation or close requests and SHALL keep Archived Stores read-only. The Color applicability choices SHALL be hosted in a vertically scrollable region when their count exceeds the available editor space, so the remaining metadata controls and the dialog's Save and Cancel actions remain reachable.

#### Scenario: Creator configures an image with many available Colors

- **WHEN** the selected offering exposes more Color values than fit in the available selected-image editor height
- **THEN** FusionCanvas keeps the Color choices in a vertically scrollable region
- **AND** the creator can reach every Color choice by pointer or keyboard scrolling
- **AND** the remaining selected-image metadata controls and Mockup Template Save and Cancel actions remain reachable without increasing the editor beyond the dialog's available height

#### Scenario: Color scrolling preserves existing applicability behavior

- **WHEN** the creator scrolls the Color choices and selects or clears a Color
- **THEN** the selected Color value is retained in the existing draft applicability state
- **AND** read-only or disabled editor state remains enforced while the Color list is scrolled
