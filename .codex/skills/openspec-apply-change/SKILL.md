---
name: openspec-apply-change
description: Implement tasks from an OpenSpec change. Use when the user wants to start implementing, continue implementation, or work through tasks.
license: MIT
metadata:
  author: openspec
  version: "1.0"
  generatedBy: "1.4.1"
---

Implement tasks from an OpenSpec change.

**Input**: Optionally specify a change name. If omitted, check if it can be inferred from conversation context. If vague or ambiguous you MUST prompt for available changes.

**Steps**

1. **Select the change**

   If a name is provided, use it. Otherwise:
   - Infer from conversation context if the user mentioned a change
   - Auto-select if only one active change exists
   - If ambiguous, run `openspec list --json` to get available changes and use the **AskUserQuestion tool** to let the user select

   Always announce: "Using change: <name>" and how to override (e.g., `/opsx:apply <other>`).

2. **Check status to understand the schema**
   ```bash
   openspec status --change "<name>" --json
   ```
   Parse the JSON to understand:
   - `schemaName`: The workflow being used (e.g., "spec-driven")
   - `planningHome`, `changeRoot`, and `actionContext`: planning scope and edit constraints
   - Which artifact contains the tasks (typically "tasks" for spec-driven, check status for others)

3. **Get apply instructions**

   ```bash
   openspec instructions apply --change "<name>" --json
   ```

   This returns:
   - `contextFiles`: artifact ID -> array of concrete file paths (varies by schema - could be proposal/specs/design/tasks or spec/tests/implementation/docs)
   - Progress (total, complete, remaining)
   - Task list with status
   - Dynamic instruction based on current state

   **Handle states:**
   - If `state: "blocked"` (missing artifacts): show message, suggest using openspec-continue-change
   - If `state: "all_done"`: congratulate, suggest archive
   - Otherwise: proceed to implementation

   **Workspace guard:** If status JSON reports `actionContext.mode: "workspace-planning"` and `allowedEditRoots` is empty, explain that full workspace apply is not supported in this slice. Treat linked repos and folders as read-only context, ask the user to select an affected area through an explicit implementation workflow, and STOP before editing files.

4. **Read context files**

   Read every file path listed under `contextFiles` from the apply instructions output.
   The files depend on the schema being used:
   - **spec-driven**: proposal, specs, design, tasks
   - Other schemas: follow the contextFiles from CLI output
   - If `<changeRoot>/retrospective.md` already exists, read it before continuing so later feedback extends the same record.

5. **Run the implementation-readiness gate**

   Before editing implementation files, verify from the context artifacts that:
   - the proposal defines one coherent delivery module, its boundaries, dependencies, non-goals, and scope rationale;
   - every requirement has observable acceptance scenarios;
   - `design.md` contains a dedicated `## Implementation Plan` with affected layers and likely files/types, responsibility placement, data/persistence and UI behavior where relevant, algorithms and edge cases, sequencing, test locations, migration/compatibility work, and decisions not to reopen;
   - each acceptance scenario has a planned verification method or explicit not-applicable rationale;
   - the assigned task range, validation commands, prohibited scope expansion, and ambiguity escalation conditions are clear.

   If a missing decision could materially change product behavior, UX, data, architecture, or acceptance, pause and return the ambiguity. Do not fill it in during implementation. Small mechanical details that remain within the approved plan may be resolved normally.

6. **Show current progress**

   Display:
   - Schema being used
   - Progress: "N/M tasks complete"
   - Remaining tasks overview
   - Dynamic instruction from CLI

7. **Implement tasks (loop until done or blocked)**

   For each pending task:
   - Show which task is being worked on
   - Make the code changes required
   - Keep changes minimal and focused
   - Mark task complete in the tasks file: `- [ ]` → `- [x]`
   - Continue to next task

   Maintain `<changeRoot>/verification.md` as verification runs. It must account for every acceptance scenario with its method, result, evidence, and limitations. An aggregate build or test result is supporting evidence, not a substitute for criterion-level accounting.

   For user-facing modules, implement focused Avalonia headless view tests when construction, bindings, control state, routed input, focus, selection, or visual-tree behavior is material. Keep decision logic at the lowest reliable framework-free layer and avoid superficial static-markup tests. Live desktop checks are optional supplemental evidence and are never required solely because a change is user-facing.

   When a criterion fails, correct the implementation or approved artifact, rerun the affected criterion and relevant regression checks, and update the evidence. Do not mark the module complete while a required criterion is failed or unaccounted for.

   **Pause if:**
   - Task is unclear → ask for clarification
   - Implementation reveals a design issue → suggest updating artifacts
   - Error or blocker encountered → report and wait for guidance
   - User interrupts

   **Capture feedback-driven learning:**
   - When user validation or review invalidates an assumption, first update the relevant specification, design, or tasks so the active artifacts describe the approved behavior.
   - Create or append `<changeRoot>/retrospective.md` using the structure below. Capture the correction when it happens; do not rely on reconstructing it from Git at archive time.
   - Record feedback-driven implementation defects too, but classify defects separately and do not promote them unless they establish a reusable rule.
   - Do not record routine compiler failures, mechanical fixes, or implementation churn that did not change understanding.

   ```markdown
   # <Change> Retrospective

   ## Outcome
   <Current approved outcome, updated as the change evolves.>

   ## Feedback-Driven Adjustments

   | Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
   | --- | --- | --- | --- | --- | --- |
   | ... | ... | ... | Missing requirement / UX / UI / architecture / implementation defect / one-off preference | Change-specific or reusable scope | Target document, deferred with rationale, or none |

   ## Deferred or Change-Specific Notes
   - ...
   ```

8. **On completion or pause, show status**

   Display:
   - Tasks completed this session
   - Overall progress: "N/M tasks complete"
   - If all done: suggest archive
   - If paused: explain why and wait for guidance

**Output During Implementation**

```
## Implementing: <change-name> (schema: <schema-name>)

Working on task 3/7: <task description>
[...implementation happening...]
✓ Task complete

Working on task 4/7: <task description>
[...implementation happening...]
✓ Task complete
```

**Output On Completion**

```
## Implementation Complete

**Change:** <change-name>
**Schema:** <schema-name>
**Progress:** 7/7 tasks complete ✓

### Completed This Session
- [x] Task 1
- [x] Task 2
...

All tasks complete! Ready to archive this change.
```

**Output On Pause (Issue Encountered)**

```
## Implementation Paused

**Change:** <change-name>
**Schema:** <schema-name>
**Progress:** 4/7 tasks complete

### Issue Encountered
<description of the issue>

**Options:**
1. <option 1>
2. <option 2>
3. Other approach

What would you like to do?
```

**Guardrails**
- Keep going through tasks until done or blocked
- Always read context files before starting (from the apply instructions output)
- If task is ambiguous, pause and ask before implementing
- If implementation reveals issues, pause and suggest artifact updates
- Keep code changes minimal and scoped to each task
- Update task checkbox immediately after completing each task
- Stay inside the approved delivery-module boundaries; do not opportunistically implement later opportunities
- Do not invent missing product, UX, data, architecture, or acceptance decisions; escalate them
- Do not report completion until `verification.md` accounts for every acceptance scenario and required validation gate
- Pause on errors, blockers, or unclear requirements - don't guess
- Use contextFiles from CLI output, don't assume specific file names
- Treat user feedback and final approved behavior as stronger evidence than an inferred Git diff
- Keep retrospectives concise and change-local; promote reusable rules to authoritative project guidance instead of turning retrospectives into a global experience log

**Fluid Workflow Integration**

This skill supports the "actions on a change" model:

- **Can be invoked anytime**: Before all artifacts are done (if tasks exist), after partial implementation, interleaved with other actions
- **Allows artifact updates**: If implementation reveals design issues, suggest updating artifacts - not phase-locked, work fluidly
