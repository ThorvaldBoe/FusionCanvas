## Summary

<!-- What changed and why? Link the issue or OpenSpec change when applicable. -->

## Verification

- [ ] Focused tests added or updated at the lowest reliable layer.
- [ ] Applicable Avalonia headless component or experience tests added/updated.
- [ ] `dotnet test .\FusionCanvas.sln -m:1` passed locally (or the failure is explained below).
- [ ] OpenSpec acceptance scenarios have criterion-level evidence.

## Defect escape analysis (complete for bug fixes; otherwise write “Not applicable”)

- User expectation that failed:
- Regression test and fail-before/pass-after evidence:
- Why existing tests passed:
- Escape class: missing scenario / bypassed seam / weak oracle / unrealistic fixture / missing re-entry / async-lifecycle / input-focus-geometry / platform-only / ambiguous specification
- Similar surfaces inspected:
- Prevention decision: local regression or reusable prevention (with rationale):

## Notes and limitations

<!-- Include deterministic limitations, optional desktop evidence, flakes, or retained temporary resources. -->
