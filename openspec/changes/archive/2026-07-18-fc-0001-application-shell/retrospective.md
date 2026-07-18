# fc-0001-application-shell Retrospective

## Outcome

Superseded. FC-0001 proposed refining the initial desktop shell from a mostly empty startup page into a workspace shell with navigation, document window, tab, workflow stage, command, and shell-state regions. The change was never implemented as a standalone unit (0/15 tasks complete). Instead, the application shell was built incrementally through later changes that were each proposed, implemented, and archived separately:

- `fc-0005-navigation-tree` — navigation tree behavior and navigation region
- `fc-0008-workflow-stage-navigator` — workflow stage navigator
- `fc-0009-tabbed-document-window` — tabbed document window and detail view host
- `fc-0010-context-aware-tools` — context-aware tool resolution
- `stage-tool-host` — stage tool host
- `fc-0101-store-management` — store management surface
- `fc-0102-niche-management` — niche management surface
- `fc-0110-workspace-management` — workspace management and compact workspace shell

The codebase (`MainWindow.axaml`, `MainWindowViewModel`, `NavigationTreePresentation`, `DocumentWindowViewModel`, `WorkflowStageNavigatorViewModel`, `StoreManagementViewModel`, `WorkspaceManagementViewModel`) implements the shell FC-0001 described, but the behavior is governed by the separate capability specs those later changes produced (`navigation-tree`, `workflow-stage-navigator`, `tabbed-document-window`, `context-aware-tools`, `stage-tool-host`, `store-management`, `niche-management`, `workspace-management`).

## Feedback-Driven Adjustments

| Initial assumption | Evidence | Correction | Classification | Applicability | Promotion |
|---|---|---|---|---|---|
| FC-0001 would deliver the application shell as one change | 0/15 tasks complete; 8 later changes each delivered a shell region | The shell was decomposed into per-region changes | Implementation defect | Project-specific | None — retrospective only |
| The shell needs reserved placeholder regions | Later changes implemented real behavior in each region | Placeholder reservations were replaced by actual capability specs | One-off preference | Change-specific | None |

## Learning Review

- **Result:** no reusable lessons identified.
- **Evidence reviewed:** FC-0001 proposal, design, delta spec, and tasks; the archive history (17 archived changes); the current `desktop-application-foundation` main spec; the current codebase shell implementation.
- **Promotions completed:** none.
- **Deferred promotions:** none. The decomposition of a large shell proposal into smaller per-capability changes is project-specific history, not a reusable rule. The OpenSpec workflow already permits archiving superseded changes.
- **Delta sync:** skipped. FC-0001's ADDED requirements (navigation region, document window region, tab/workflow stage areas, command locations, unavailable states) describe reservations for behavior that now has its own accepted capability specs. Syncing the reservations would duplicate or conflict with the implemented capability specs. The MODIFIED requirement ("Main window presents an initial workspace shell") refines the old "mostly empty page" wording; the current main spec still carries the old wording, which is a separate drift concern to be reconciled by a future OpenSpec change if needed.
