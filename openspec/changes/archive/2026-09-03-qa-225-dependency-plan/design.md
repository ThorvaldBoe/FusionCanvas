# Design

Keep the update plan in `docs/qa-225-dependency-plan.md`, backed by the dated NuGet commands. Separate patch-level runtime packages from the major Avalonia and xUnit framework batches. Do not modify package files until a batch has an owner, focused verification, and a compatibility review.

## Implementation plan

1. Query outdated and vulnerable packages.
2. Record versions, batch boundaries, and verification gates.
3. Validate the OpenSpec package and preserve the current build/test baseline.
