# UI Test Suite Support Retrospective

## Outcome

FusionCanvas now has a separately selectable Windows Appium test project with disposable runtime state, actionable prerequisite diagnostics, stable Store Editor automation identifiers, and one passing compiled-app store-creation smoke journey. The deterministic solution baseline remains independent of desktop automation prerequisites.

## Feedback-Driven Adjustments

| Initial assumption | Evidence | Correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Launching `appium.ps1` would behave like a normal executable | It opened in Notepad in the contributor environment | Recommend the Windows command shim (`appium.cmd`) or `appium`, not direct script-file invocation | Setup defect | Windows npm tooling | Kept in UI-test README and retrospective |
| The main Appium session could traverse Avalonia flyouts and secondary windows | Flyout and Store Editor surfaces were absent from the attached window tree; desktop-root sessions hung during teardown | UI-test mode hosts the real Store Editor as one stable top-level target with normal services and isolated persistence | Architecture/testing lesson | Future multi-window desktop journeys | Promoted to change design; future journeys should document one stable top-level target |
| An empty isolated database was sufficient for store creation | The application correctly presented the no-workspace state | Seed the minimum workspace inside the disposable database before launch | Missing test precondition | Store-related desktop journeys | Promoted to fixture behavior and change design |
| The UI-test project was outside the deterministic solution baseline | The baseline attempted to run it and failed without Appium | Remove `FusionCanvas.UITests` from `FusionCanvas.sln` and rerun | Implementation defect | Optional environment-dependent suites | Requirement already captured in the testing-baseline delta |
| Container `.Text` exposed the created store name | WinAppDriver did not aggregate child text for Avalonia `ItemsControl` | Assert the visible child button by accessibility name | Framework-specific locator lesson | Avalonia collection assertions | Kept in page object and retrospective |

## Learning Review

- Result: reusable lessons identified
- Evidence reviewed: user feedback, Appium/WinAppDriver results, final smoke output, deterministic baseline output, proposal, design, delta specs, tasks, and implementation behavior
- Promotions completed: stable single-window target documented in the design; optional environment-dependent projects kept outside the solution baseline through the testing-baseline requirement
- Deferred promotions: no repository-wide UI guideline change; these lessons remain Windows-Appium-specific until another desktop journey confirms broader applicability
