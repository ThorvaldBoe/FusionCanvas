## Why

The AI profile editor's additional parameters are presented as unlabeled text boxes in a dense grid, making the controls difficult to scan and understand. Clear labels and concise explanations will improve confidence without changing values, capability gating, or persistence.

## What Changes

- Rework the additional-parameters area into consistently labeled parameter cards/rows.
- Add concise helper text describing what each supported parameter influences.
- Preserve the existing expander, capability-driven visibility, bindings, ranges, and save behavior.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `application-settings`: improve the presentation and explanatory text for AI additional parameters without changing their behavior.

## Impact

Changes are limited to the Avalonia AI profile editor view and its focused headless view tests, plus the OpenSpec delta and verification record. No application contracts, persistence, or provider APIs change.
