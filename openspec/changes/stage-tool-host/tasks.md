## 1. Application Contracts

- [ ] 1.1 Add workflow stage, entity context kind, and stage tool availability types in `FusionCanvas.Application`.
- [ ] 1.2 Add stage tool descriptor and registry contracts that support built-in and future contributed tools.
- [ ] 1.3 Add a stage tool context snapshot model that carries active stage, store, topic path, selected topic, selected item, inherited metadata hooks, nearby work hooks, user settings hooks, and provider capability hooks as available.
- [ ] 1.4 Add an application-layer host service that evaluates registered tools for the active context and returns available, unavailable, disabled, and failed states.
- [ ] 1.5 Add active tool selection logic scoped by workflow stage and context kind, including fallback to the default available tool.

## 2. Built-In Tool Registration

- [ ] 2.1 Register built-in default stage tool descriptors for the initial Idea, Concept, Design, and Listing stages.
- [ ] 2.2 Mark item-bound built-in tools as requiring selected item context where appropriate.
- [ ] 2.3 Ensure built-in descriptors are discoverable when no plugin or contributed tools exist.
- [ ] 2.4 Keep built-in tools routed through the same registry/query service used for contributed tools.

## 3. UI Host Integration

- [ ] 3.1 Add a Stage Tool Host view model or UI state model in `FusionCanvas.App` that consumes the application-layer host service.
- [ ] 3.2 Add the lower document-window detail host area to the main document surface without implying unavailable workflow features.
- [ ] 3.3 Render the selected tool's display surface or honest default placeholder for the current stage.
- [ ] 3.4 Render a compact tool selector in a consistent top-right location when multiple tools are available.
- [ ] 3.5 Hide or minimize the selector when exactly one tool is available.
- [ ] 3.6 Render an item-required or unavailable state when the active context does not satisfy a selected tool's requirements.
- [ ] 3.7 Update host state when the active tab, navigation selection, selected item, or workflow stage changes.

## 4. Tool Failure and Durable Change Boundaries

- [ ] 4.1 Represent contributed tool load failures as availability states that do not break the document window.
- [ ] 4.2 Verify the default built-in tool remains selectable when a contributed tool is missing, disabled, unavailable, or failed.
- [ ] 4.3 Expose application-layer command or service boundaries that hosted tools must use for durable workspace changes.
- [ ] 4.4 Ensure hosted tool contracts do not require direct persistence, workspace storage, or UI control access.

## 5. Tests and Verification

- [ ] 5.1 Add application tests for tool availability filtering by workflow stage.
- [ ] 5.2 Add application tests for topic-context tools versus item-required tools.
- [ ] 5.3 Add application tests for selected-tool fallback when the prior selection is invalid.
- [ ] 5.4 Add application tests proving built-in default tools remain discoverable without contributed tools.
- [ ] 5.5 Add app-layer tests for selector visibility and unavailable-state decisions where practical.
- [ ] 5.6 Run `dotnet test` for `FusionCanvas.sln` and resolve failures.
- [ ] 5.7 Run `openspec status --change "stage-tool-host"` and confirm the change is apply-ready.
