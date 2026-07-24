# FC-0103 Verification

## Desktop interaction pass

Result: **Pass for the desktop scenarios listed below; no blocked desktop scenario.**

- Date/build: 2026-07-18, current working-tree Debug `net10.0` Avalonia desktop build.
- Environment/input: interactive Windows desktop using Windows UI Automation plus native keyboard and pointer input.
- Isolation: every run used a disposable copy of `workspace.db` selected through `FUSIONCANVAS_WORKSPACE_DB`; the normal workspace database was inspected only to create the copy and was never mutated by verification.
- Evidence: observed accessibility state and persisted hierarchy results are recorded per scenario below. Pixel-perfect visual regression was not applicable to this interaction-focused change.

- Keyboard: expanded the tree, created consecutive siblings with `Ctrl+Shift+N` and `Shift+Enter`, renamed with F2, and retained a focused inline editor after duplicate-name validation.
- Clipboard and selection: copied and moved subtrees with Ctrl+C/X/V, confirmed normal selection updates the inspector without opening a tab, and confirmed Ctrl-click opens/activates one tab without duplication.
- Lifecycle and filtering: confirmed archive removes the active row while preserving an intentional archived-review row with Restore enabled; confirmed filtering retains match ancestry, hides nonmatches, and restores the hierarchy when cleared.
- Pointer structure editing: confirmed before and after placement, before-order persistence after restart, drop-onto reparenting, and automatic expansion of a collapsed destination.
- Safety: confirmed descendant drops are rejected with visible feedback and no hierarchy change, and filtered positional dragging does not change sibling order.
- Recovery: duplicate-name validation was exercised in the desktop inline editor. Persistence-failure injection was protected at code level by a failing-repository app test because the production desktop composition root exposes no test-only fault injector; that test confirms the confirmed hierarchy and canonical selection remain visible with a recoverable error.

## Automated regression pass

- `dotnet test FusionCanvas.sln --no-restore --nologo -v quiet`: 180 passed, 0 failed.
- `dotnet format FusionCanvas.sln whitespace --verify-no-changes --no-restore`: passed.
- `openspec validate fc-0103-group-management --strict`: passed.
- `git diff --check`: passed (line-ending conversion warnings only).
