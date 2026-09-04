## 1. Eligibility diagnostics

- [x] 1.1 Extend the eligibility result and service to return active candidate templates with their authoritative readiness summaries while preserving ready-only template filtering.
- [x] 1.2 Add application tests covering no active candidates, multiple Draft candidates with accumulated blockers, and a ready candidate remaining eligible.

## 2. Listing presentation

- [x] 2.1 Carry candidate readiness diagnostics through mockup-generation state and expose immutable template-name plus blocker-message presentation data from the Listing view model.
- [x] 2.2 Update Listing AXAML to show distinct no-template, incomplete-template, and eligibility-error guidance with accessible ordinary text, without changing the Apply gate.
- [x] 2.3 Add App/view-model tests for the blocked states, blocker wording, ready-template state, and preserved ready-only application behavior.

## 3. Verification and delivery

- [x] 3.1 Update `verification.md` with criterion-level evidence for every acceptance scenario and record any limitations.
- [x] 3.2 Run `openspec validate listing-mockup-template-diagnostics` and resolve any validation failures.
- [x] 3.3 Run `dotnet test .\FusionCanvas.sln --no-restore -m:1` and confirm the full solution baseline passes.
