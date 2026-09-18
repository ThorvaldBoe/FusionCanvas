# Archive-First Blueprint Lifecycle Retrospective

## Outcome

Implemented an archive-first Blueprint lifecycle: active Blueprints expose archive only; archived Blueprints are opt-in visible and can be permanently deleted only after explicit confirmation. Permanent deletion validates protected listing/design references and removes the owned normalized and compatibility catalog graph atomically.

## Feedback-Driven Adjustments

| Initial assumption | Evidence / correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- |
| Existing product deletion could continue to represent Blueprint archive behavior | The requested lifecycle makes archive and permanent deletion distinct user actions | Missing requirement | Blueprint/catalog lifecycle | Promote through the delta specs; sync to main specs if approved |
| Archived records could remain hidden without an explicit review path | The requested workflow requires intentional archived review before destructive deletion | UX/UI principle | Archive-first catalog surfaces | Promote through the delta specs; sync to main specs if approved |
| Permanent deletion can be treated as a row-level operation | Normalized and compatibility records can recreate or orphan catalog state | Architecture/data-safety lesson | Catalog-owned graph deletion | Promote through the delta specs; sync to main specs if approved |

## Learning Review

- Result: reusable lessons identified.
- Evidence reviewed: final proposal, design, both delta specs, completed tasks, verification evidence, application tests, Avalonia headless tests, and full solution test results.
- Promotions completed: captured in the change delta specs and implementation design.
- Deferred promotions: syncing the delta requirements into `openspec/specs/` awaits the user's archive-time sync choice.
