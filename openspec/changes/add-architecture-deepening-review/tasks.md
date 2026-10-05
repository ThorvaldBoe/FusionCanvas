## 1. Factory vocabulary and scope rules

- [x] 1.1 Add the deep-module vocabulary to the active Software Factory Architecture & Code Structure guidance, defining module, interface, seam, adapter, depth, leverage, locality, deletion test, and interface-as-test-surface in plain language.
- [x] 1.2 Record that the review is a supporting architecture procedure, not a seventh audit axis, compliance verdict, certification result, or automatic remediation instruction.
- [x] 1.3 Add earned-seam guidance that preserves the existing rules for consumer-owned interfaces, real variation, I/O, lifecycle, testability, and anti-overengineering.

## 2. Deepening review procedure and record

- [x] 2.1 Add the procedure for recording scope, source revision, working-tree state, owner, evidence baseline, and reason for selecting the review scope.
- [x] 2.2 Add the candidate-discovery procedure using source-extract types, callers, implementations, registrations, dependencies, tests, and relevant audit evidence.
- [x] 2.3 Add the candidate record template covering current interface, leaked complexity, deletion-test result, proposed seam, leverage, locality, dependency category, adapters, test surface, risks, recommendation strength, and decision.
- [x] 2.4 Add explicit candidate outcomes for selected, deferred, rejected, blocked, and not-worth-deepening, including rationale and follow-up ownership where needed.
- [x] 2.5 Add the rule that high-impact interfaces compare at least two credible designs before selection, while small local refactors may use one justified design.

## 3. Workflow handoff and pilot

- [x] 3.1 Add OpenSpec and maintenance handoff guidance for accepted behavior changes, public contracts, persistence semantics, and internal refactors.
- [x] 3.2 Add the separation and linking rules for deepening candidates, audit findings, audit challenges, certifications, and post-change revalidation.
- [x] 3.3 Run the procedure retrospectively against the completed terms-consent architecture correction and record the pilot without creating a duplicate finding.
- [x] 3.4 Review the pilot for false positives, speculative interface recommendations, evidence gaps, and scope leakage; correct the procedure or artifacts if needed.

## 4. Verification and delivery gates

- [x] 4.1 Verify every architecture-deepening-review acceptance scenario against the procedure, template, and pilot record, recording criterion-level evidence.
- [x] 4.2 Confirm the documentation does not expose private Software Factory paths or content in tracked repository artifacts.
- [x] 4.3 Run `openspec validate add-architecture-deepening-review --strict` and correct any artifact or delta-spec errors.
- [x] 4.4 Run `dotnet test .\FusionCanvas.sln` as the repository baseline and record whether the documentation-only change affects or does not affect product tests.
- [x] 4.5 Complete the Factory documentation review and record the final decision, limitations, and any deferred follow-up before archiving the change.
