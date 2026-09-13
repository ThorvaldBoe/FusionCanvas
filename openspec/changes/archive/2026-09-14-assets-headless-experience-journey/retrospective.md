# Retrospective — assets-headless-experience-journey

## Outcome

The Assets store surface now has one user-job-oriented Avalonia headless journey. It crosses rendered controls, asynchronous view-model state, managed file import, preview lifecycle, cancellation semantics, and SQLite re-entry while retaining focused lower-layer diagnostics.

## Learning review

- No product defect escaped during implementation; no new production regression was needed.
- A driver locator initially encountered duplicate ComboBox visual instances created by Avalonia templates. Restricting selection to the visible rendered control kept the driver at the user boundary without reflection or direct view-model mutation.
- The reusable lessons are the same strategy promoted by the earlier module: semantic automation IDs only where needed, bounded dispatcher waits, scenario-owned disposable persistence, and visible-plus-durable outcome assertions.
- Native file-picker chrome and platform/pixel rendering remain explicitly supplemental; deterministic fakes and observable preview lifecycle provide the separable baseline evidence.

## Strategy health

- Focused run: 12 relevant tests passed (9 App asset tests, 2 Integration persistence tests, 1 new journey).
- Full serialized baseline: 1,570 passed, 0 failed; no flakes observed.
- Maintenance cost is one thin driver and one scenario fixture; no production behavior or schema change was required.
- Failure localization remains clear: rendered journey failures identify binding/input/state seams, while focused tests retain service/file-store diagnosis.

## Deferred scope

Do not expand this pilot into item-inline asset workflows, workspace transfer, batch import, native picker automation, screenshot comparison, or repository-wide journey conversion without a separate approved change and evidence that the seam risk justifies the maintenance cost.
