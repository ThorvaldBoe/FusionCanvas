# Plan dependency update batches

## Why

The QA review identified pending dependency updates but did not provide a checked-in plan or current vulnerability evidence. A staged plan reduces upgrade blast radius while preserving a verified security baseline.

## Scope

- Record the current outdated-package report and vulnerability result.
- Group updates into runtime, Avalonia, and test-tooling batches.
- Define verification expectations for each future batch.

## Non-goals

- No package version changes in this planning finding.
- No claim that unrun upgrades are compatible.

## Modified Capabilities

- `testing-baseline`: require dependency updates to be planned and verified in bounded batches.

## Verification

- NuGet outdated and vulnerability queries complete successfully.
- The checked-in plan reflects the observed package versions and separates major upgrade risk.
