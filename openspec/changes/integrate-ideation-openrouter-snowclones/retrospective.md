# Integrate Ideation, OpenRouter, and Snowclones Retrospective

## Outcome

Production Ideation uses configured OpenRouter generation and the persisted Snowclone Library without delaying main-window creation on native credential, settings, or workspace initialization.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Synchronously waiting for async startup operations was safe because the underlying adapters usually complete quickly or use `ConfigureAwait(false)`. | The FusionCanvas process remained alive but no main window appeared. OpenRouter availability, JSON settings, workspace loading, and Snowclone initialization could wait on Avalonia's non-pumping UI synchronization context. | Keep initial Ideation availability at `Checking`, refresh it asynchronously after view-model composition, and execute unavoidable synchronous factory initialization on a worker context. Add regression tests using a deliberately non-pumping synchronization context. | Implementation defect | Reusable desktop-startup rule | Promote to architecture/coding guidance during the next documentation maintenance change. |

## Deferred or Change-Specific Notes

- Native live-launch checks must be detached from captured terminal output handles; deterministic startup tests are the required verification here.
