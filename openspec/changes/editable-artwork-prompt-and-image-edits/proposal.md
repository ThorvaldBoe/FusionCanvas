## Why

Artwork generation currently hides the assembled prompt and always starts from text, so creators cannot reliably control foreground contrast or refine a promising image without hoping a fresh generation preserves it. Exposing a resettable, editable prompt and a reference-image edit path makes the current design context inspectable while keeping the creator in control.

## What Changes

- Build a deterministic default artwork prompt from the current Concept Triangle, current niche/store creative context, composition guidance, contrast selection, and print constraints.
- Add a Design-stage prompt editor that shows the default, preserves the creator's edited prompt per Item, and resets to a newly compiled default from current source data.
- Add a per-Item Light on dark / Dark on light contrast choice that contributes explicit foreground-ink direction to the default prompt.
- Allow a creator to edit the currently assigned artwork by sending that image as a reference with a separate, saved adjustment-instructions field; successful edits replace the slot artwork and preserve the prior image as a Related Image.
- Enable image edits only when the selected image model endpoint advertises support for input references, with actionable unavailable/error states.
- Add a GitHub issue for a future curated-style feature; curated-style controls and presets are not part of this module.

## Capabilities

### New Capabilities
- `design-artwork-prompting`: Compiled and editable prompts, contrast direction, and prompt-based image editing in the Design stage.

### Modified Capabilities
- None.

## Impact

- App Design stage view model and Avalonia UI for prompt/contrast/edit controls.
- Application prompt compilation, generation/edit orchestration, Item metadata, and workspace asset preservation.
- AI image request contract, endpoint capability parsing and selection, and OpenRouter image request serialization for `input_references`.
- Application, integration, and App headless tests; no database schema migration because existing Item metadata is extensible JSON.
- GitHub issue for curated styles only; no style catalog implementation.
