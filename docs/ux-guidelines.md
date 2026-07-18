# FusionCanvas UX Guidelines

## Purpose

These guidelines describe how FusionCanvas should behave from the user's point of view: where work happens, how actions are discovered, how state changes are communicated, and how users remain in control.

Use these guidelines together with [FusionCanvas UI Guidelines](ui-guidelines.md). The UI guidelines define visual composition and control presentation; this document defines interaction and workflow behavior.

## Protect the Primary Workspace

The main window should prioritize frequent creative work: navigation, context switching, workflow progression, and the active stage tool.

- Keep common, lightweight actions close to the object or context they affect.
- Move occasional setup, administration, and destructive management into a focused dialog or editor window when inline controls would compete with daily work.
- Do not permanently consume workspace area for controls that are used only occasionally.
- Preserve the user's main-window context while a focused management surface is open.

Store management is the reference pattern: the main window owns store selection and access to management, while a dedicated editor owns creation, context editing, archive, restore, and deletion.

## Use Progressive Disclosure

Show the minimum controls and information needed for the current task, while keeping additional options discoverable.

- Prefer compact selectors, menus, expandable sections, or focused editors over always-visible administration forms.
- Keep the current selection visible when a larger list or editor is collapsed.
- Make expand and collapse state clear through the control's state, label, tooltip, or icon.
- Ensure large collections remain reachable without permanently displacing higher-value content.

## Give Each Action One Clear Owner

Avoid multiple visible controls that perform the same action in the same context.

- Place commands near the object or state they affect.
- Prefer the clearest compact control when two controls duplicate an action.
- Keep selection actions separate from editing or destructive management actions.
- Make the primary next action evident without presenting every possible action as equally prominent.

## Preserve User Intent During Editing

Editing flows should distinguish temporary input from persisted changes.

- New records may begin as drafts and should not persist until the user explicitly saves when accidental creation would be surprising.
- Detect unsaved changes when the user switches selection or closes an editor.
- Ask before discarding meaningful unsaved work.
- Keep the user on the current record or draft when discard is declined.
- Preselect the active or most relevant record when opening a management surface.

## Keep State and Available Actions Coherent

The interface should reflect what can happen in the current state.

- Highlight the current selection consistently across compact and expanded views.
- Enable actions only when they apply to the selected state.
- After deleting or removing a selection, choose a sensible remaining item when one exists; otherwise show a useful empty state.
- Keep archived, inactive, unavailable, and destructive states visibly distinct from normal active work.
- Explain why a requested action is blocked and offer the safe alternative when one exists.

## Manage Focus and Keyboard Flow

After a transition, place keyboard focus where the user's next input is expected.

- Focus the primary field when starting a new draft.
- Preserve predictable tab order through forms and action rows.
- Return focus to a meaningful invoking or replacement control when a dialog closes or a selected item disappears.
- Do not require pointer interaction for essential confirmation, cancellation, or save flows.

## Design Complete Interaction States

User-facing feature design should consider the full interaction lifecycle, not only the populated success state.

For each relevant workflow, decide:

- initial and empty states
- loading or in-progress state
- successful completion and resulting selection
- validation and recoverable errors
- unavailable or blocked actions
- cancellation and dismissal
- unsaved-change behavior
- destructive-action confirmation and aftermath

Not every feature needs every state, but omitted states should be intentional rather than left to implementation guesswork.

## UX Preflight for User-Facing Changes

Before implementation, answer these questions in the OpenSpec proposal, design, or scenarios:

1. Who performs the workflow, and what are they trying to finish?
2. How frequently is each action used?
3. Which actions belong in the primary workspace, and which belong in a focused surface?
4. How much persistent workspace area may the feature consume?
5. What should be progressively disclosed?
6. What are the initial, empty, success, blocked, and error states?
7. How do selection, focus, and keyboard flow behave after transitions?
8. How are drafts, unsaved changes, cancellation, and destructive actions handled?

For changes without a user-facing interaction, mark the UX preflight as not applicable rather than inventing UI behavior.
