---
name: openspec-propose
description: Propose a new change with all artifacts generated in one step. Use when the user wants to quickly describe what they want to build and get a complete proposal with design, specs, and tasks ready for implementation.
license: MIT
metadata:
  author: openspec
  version: "1.0"
  generatedBy: "1.4.1"
---

Propose a new change - create the change and generate all artifacts in one step.

I'll create a change with artifacts:
- proposal.md (what & why)
- design.md (how)
- tasks.md (implementation steps)

When ready to implement, run /opsx:apply

---

**Input**: The user's request should include a change name (kebab-case) OR a description of what they want to build.

## FusionCanvas Delivery-Module Contract

For FusionCanvas feature work, one change normally represents one **delivery module**: a cohesive, independently verifiable feature set with one clear outcome. There is no fixed feature count. Do not create detailed artifacts for later modules while proposing the next one.

Before creating artifacts, establish enough shared understanding with the user to resolve:

- the module outcome and included features;
- dependencies, non-goals, and why the scope is coherent and reviewable;
- representative workflows, examples, edge cases, and failure states;
- high-impact product, UX, data, architecture, migration, and acceptance decisions;
- the verification approach and desktop-test risks for user-facing work.

Ask focused questions only where the answer would materially change the module. Capture conclusions in the artifacts rather than pasting the conversation. Historical documents under `docs/LifeOS` are optional idea sources, not required context or current requirements.

Do not create a separate module-specification artifact by default. Treat `proposal.md` as the module-level anchor and place each discovery conclusion directly in the proposal, delta specs, design, tasks, or verification mapping according to its responsibility. Continue refining those artifacts until the package is approved rather than maintaining a parallel source of truth.

The delivery package is implementation-ready only when:

- delta specs contain observable acceptance scenarios for every requirement;
- `design.md` separates conceptual/functional design from a dedicated `## Implementation Plan` detailed enough for the assigned agent, including affected layers and likely files/types, responsibility placement, data/persistence and UI behavior, algorithms and edge cases, sequencing, test locations, migration/compatibility work, and decisions not to reopen;
- every acceptance scenario has a planned verification method or explicit not-applicable rationale;
- `tasks.md` decomposes the implementation plan into ordered, bounded, verifiable steps and includes criterion-level verification, strict OpenSpec validation, and the solution test baseline;
- no unresolved high-impact decision is silently delegated to implementation.

**Steps**

1. **If no clear input provided, ask what they want to build**

   Use the **AskUserQuestion tool** (open-ended, no preset options) to ask:
   > "What change do you want to work on? Describe what you want to build or fix."

   From their description, derive a kebab-case name (e.g., "add user authentication" → `add-user-auth`).

   **IMPORTANT**: Do NOT proceed without understanding what the user wants to build.

2. **Create the change directory**
   ```bash
   openspec new change "<name>"
   ```
   This creates a scaffolded change in the planning home resolved by the CLI with `.openspec.yaml`.

3. **Get the artifact build order**
   ```bash
   openspec status --change "<name>" --json
   ```
   Parse the JSON to get:
   - `applyRequires`: array of artifact IDs needed before implementation (e.g., `["tasks"]`)
   - `artifacts`: list of all artifacts with their status and dependencies
   - `planningHome`, `changeRoot`, `artifactPaths`, and `actionContext`: path and scope context. Use these instead of assuming repo-local paths.

4. **Run the UX preflight when the change is user-facing**

   Before drafting artifacts for a user-facing change:
   - Read `docs/ui-guidelines.md` and `docs/ux-guidelines.md` when they exist in the planning home.
   - Identify the user's primary workflow and the expected frequency of each action.
   - Decide which actions belong in the primary workspace and which belong in a focused surface.
   - Resolve acceptable workspace footprint, progressive disclosure, and relevant empty, loading, success, blocked, and error states.
   - Resolve selection, keyboard focus, drafts, unsaved changes, cancellation, and destructive actions where relevant.

   Capture the decisions in the proposal, design, or specification scenarios rather than copying the checklist verbatim. Ask the user only when a high-impact product preference cannot be derived from project context. For a change without user-facing interaction, record that the UX preflight is not applicable.

   Plan real-desktop verification by risk and information value. Cover the critical workflow and distinct high-risk wiring or interaction behavior; use deterministic tests for equivalent low-risk variants. Record why the selected desktop scenarios are sufficient and how an unavailable interactive environment will be handed off.

5. **Create artifacts in sequence until apply-ready**

   Use the **TodoWrite tool** to track progress through the artifacts.

   Loop through artifacts in dependency order (artifacts with no pending dependencies first):

   a. **For each artifact that is `ready` (dependencies satisfied)**:
      - Get instructions:
        ```bash
        openspec instructions <artifact-id> --change "<name>" --json
        ```
      - The instructions JSON includes:
        - `context`: Project background (constraints for you - do NOT include in output)
        - `rules`: Artifact-specific rules (constraints for you - do NOT include in output)
        - `template`: The structure to use for your output file
        - `instruction`: Schema-specific guidance for this artifact type
        - `resolvedOutputPath`: Resolved path or pattern to write the artifact
        - `dependencies`: Completed artifacts to read for context
      - Read any completed dependency files for context
      - Create the artifact file using `template` as the structure and write it to `resolvedOutputPath`
      - Apply `context` and `rules` as constraints - but do NOT copy them into the file
      - Show brief progress: "Created <artifact-id>"

   b. **Continue until all `applyRequires` artifacts are complete**
      - After creating each artifact, re-run `openspec status --change "<name>" --json`
      - Check if every artifact ID in `applyRequires` has `status: "done"` in the artifacts array
      - Stop when all `applyRequires` artifacts are done

   c. **If an artifact requires user input** (unclear context):
      - Use **AskUserQuestion tool** to clarify
      - Then continue with creation

   d. **Apply the delivery-package readiness gate before stopping**:
      - Confirm the proposal defines one coherent module and justifies its size.
      - Confirm delta specs have observable acceptance scenarios.
      - Confirm `design.md` contains the dedicated implementation plan and planned acceptance-to-verification mapping.
      - Confirm tasks are bounded and include all required verification gates.
      - If any high-impact decision remains unresolved, pause and ask rather than declaring the change apply-ready.

6. **Show final status**
   ```bash
   openspec status --change "<name>"
   ```

**Output**

After completing all artifacts, summarize:
- Change name and location
- List of artifacts created with brief descriptions
- What's ready: "All artifacts created! Ready for implementation."
- Prompt: "Run `/opsx:apply` or ask me to implement to start working on the tasks."

**Artifact Creation Guidelines**

- Follow the `instruction` field from `openspec instructions` for each artifact type
- The schema defines what each artifact should contain - follow it
- Read dependency artifacts for context before creating new ones
- Use `template` as the structure for your output file - fill in its sections
- **IMPORTANT**: `context` and `rules` are constraints for YOU, not content for the file
  - Do NOT copy `<context>`, `<rules>`, `<project_context>` blocks into the artifact
  - These guide what you write, but should never appear in the output

**Guardrails**
- Create ALL artifacts needed for implementation (as defined by schema's `apply.requires`)
- Treat the project-specific delivery-module and implementation-readiness rules above as additional constraints even when generic CLI artifact instructions are less detailed
- Always read dependency artifacts before creating a new one
- If context is critically unclear, ask the user - but prefer making reasonable decisions to keep momentum
- If a change with that name already exists, ask if user wants to continue it or create a new one
- Verify each artifact file exists after writing before proceeding to next
- Do not leave user-facing surface placement or core interaction-state decisions to implementation when they can be resolved during the UX preflight
