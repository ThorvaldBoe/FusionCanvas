# Establish the QA-225 quality baseline

## Why

The full review reported compiler/analyzer warnings and formatter debt without a committed, reproducible baseline. The repository needs an explicit record so maintenance work can prevent regression while the debt is reduced in focused batches.

## Scope

- Record the current deterministic build command and warning count.
- Record the formatter command and its current runner limitation.
- Define no-regression expectations without suppressing diagnostics.

## Non-goals

- No broad warning cleanup or formatting rewrite in this bounded baseline change.
- No analyzer suppression.

## Modified Capabilities

- `testing-baseline`: require explicit tracking of known compiler, analyzer, and formatting debt.

## Verification

- Build command completes with 0 errors and the recorded warning count.
- Documentation is reviewed for exact commands and limitations.
