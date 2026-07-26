## Why

FusionCanvas can capture and refine one Item at the Idea stage, but it cannot yet help a creator generate, compare, accept, and reject multiple contextual directions without leaving the workspace. This module adds a focused, optional Ideation workflow that learns from active and rejected ideas while preserving human approval and local ownership.

**Module outcome:** A creator with placeholder AI access can open Ideation from the Idea stage, generate a bounded batch of Basic or Snowclone candidates from the active store, niche, and optional group context, create useful candidates as normal Idea-stage Items, reject unsuitable candidates with optional reasoning, and safely discard the remaining transient candidates.

**Scope rationale:** The Idea-stage launch action, focused dialog, fake generator, in-memory Snowclone source, context assembly, candidate decisions, rejection persistence, and deterministic verification form one coherent outcome. Splitting them would leave either an unreachable dialog, a generator whose results cannot become work, or rejection feedback that cannot inform later batches. A real AI provider, secure credential management, and a managed Snowclone database are independent later outcomes and remain outside this module.

## What Changes

- Add an `Ideation…` action to the active Idea-stage tool area for niche, group, and Item contexts that resolve to an active niche.
- Gate the action on a non-empty placeholder API-key environment value; keep it visible but disabled with configuration guidance when access is unavailable.
- Open one owned modal Ideation dialog without replacing the current tab, selection, or stage content.
- Show the resolved store, niche, and optional group scope; create at the niche root when no group is selected and in the exact selected or parent group otherwise.
- Provide extensible mode selection with initial `Basic` and `Snowclones` modes, one optional multi-line guidance field, a bounded desired-count field, and one Generate action.
- Generate candidates asynchronously through a fake context-aware generator, using bounded parallelism, cancellation, partial-result retention, normalized duplicate suppression, visible busy/progress state, and a small in-memory Snowclone catalog.
- Supply user-authored store, niche, and group context, optional guidance, mode/template, every active Item Idea in scope, and rejected Idea text/reasoning in scope while excluding credentials and operational database fields.
- Present transient candidates with Create and Reject actions; Create produces a normal Draft Item at the Idea stage, while Reject records a durable ideation rejection after an optional-reason confirmation dialog.
- Remove a candidate only after its Create or Reject operation succeeds; keep recoverable candidates after cancellation or failure.
- Confirm Clear All, dialog Close with candidates, and Close during generation before cancelling work or discarding candidates.
- Persist ideation rejections and their store, niche, optional group, text, reason, mode, and timestamp in versioned local SQLite storage.

## Capabilities

### New Capabilities

- `ideation`: Defines Idea-stage availability, dialog behavior, contextual Basic and Snowclone generation, transient candidate handling, creation, rejection, confirmation, cancellation, and failure behavior.

### Modified Capabilities

- `context-aware-tools`: Adds explicit ideation context containing active Idea content and rejected Idea reasoning at exact-group or whole-niche scope.
- `stage-tool-host`: Adds a context-aware auxiliary Idea-stage action that opens a focused modal tool without replacing the hosted Idea editor.
- `local-sqlite-persistence`: Adds durable ideation-rejection storage and a safe versioned migration while preserving existing workspace data.

## Impact

- **Domain:** Add ideation mode and rejection concepts; extend the workspace model with durable rejections while keeping candidates transient.
- **Application:** Add ideation context assembly, availability, generation, candidate-decision orchestration, Snowclone-source and generator boundaries, and use existing Item management for accepted candidates.
- **Integration:** Add the SQLite rejection table/migration and placeholder environment-based access plus fake Basic/Snowclone generation adapters; no network service or real credential persistence is introduced.
- **App:** Add an Idea-stage launch action, owned modal Ideation window, rejection and discard confirmations, progress/cancellation states, accessible keyboard flow, and authoritative workspace refresh after candidate decisions.
- **Compatibility:** Depends on the Item and built-in stage surfaces from `basic-product-creation-workflow`; implementation should begin after that change is completed or after its relevant contracts are stable. Existing workspace identities, Items, metadata, relationships, and files must survive the new migration unchanged.
- **Privacy/security:** Only user-authored creative context is passed to the fake local generator. API-key values, entity IDs, timestamps, archive flags, file paths, and other operational fields are excluded from generation payloads and logs.
- **Verification:** Add deterministic domain/application tests, SQLite migration and round-trip tests, and Avalonia headless tests for action availability, modal ownership, scope display, busy/disabled states, candidate actions, confirmations, keyboard reachability, focus return, and theme coherence. The normal `dotnet test .\FusionCanvas.sln` and strict OpenSpec validation remain completion gates.
- **Non-goals:** Real AI/provider SDK calls, secure credential entry or storage, provider/model selection, token/cost accounting, a persisted Snowclone library or management UI, prompt-history persistence, automatic scoring, trend or marketplace research, candidate editing, bulk auto-approval, and changes to the existing manual Idea editor.
