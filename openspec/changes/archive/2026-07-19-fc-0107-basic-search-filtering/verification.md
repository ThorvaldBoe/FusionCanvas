# fc-0107-basic-search-filtering — Verification

## Build / Environment

- Repository: `C:\Code\FusionCanvas_opencode_fc-0107-basic-search-filtering` (worktree, branch `opencode/fc-0107-basic-search-filtering`)
- Base: `main` at `afc5fe4`
- SDK: .NET 10 (net10.0), xUnit v3
- Date: 2026-07-19

## Automated Baseline

Command: `dotnet test .\FusionCanvas.sln`

| Project | Passed | Failed | Total |
| --- | --- | --- | --- |
| FusionCanvas.Domain.Tests | 23 | 0 | 23 |
| FusionCanvas.Application.Tests | 83 | 0 | 83 |
| FusionCanvas.App.Tests | 90 | 0 | 90 |
| FusionCanvas.Integration.Tests | 20 | 0 | 20 |
| **Total** | **216** | **0** | **216** |

New automated coverage added by this change:

- **Domain** (`NavigationTreeTests`): archive-inclusive `BuildTree` reveals archived group subtrees and archived listings marked `IsInactive`; default build leaves all nodes active.
- **Application** (`WorkspaceTreeTests`): projector text matching across listing description, notes, and tag names; tag AND semantics with ancestor-context retention; subtree-scope projection; scope+text combination; include-archived reveals inactive listings; empty-result projections; blank-text pass-through.
- **App** (`WorkspaceTreeViewModelTests`): tag filter narrows and guards sibling positioning; subtree scope pins and restricts; include-archived reveals archived rows without making them canonical context; clear-all restores expansion and keeps selection; empty-results state; `HasNonTextFilters` indication; `AvailableTags` listing; `CanScopeToCurrentTopic` resolvability.

OpenSpec validation: `openspec validate fc-0107-basic-search-filtering --strict` → valid.

## Spec Scenario Coverage (automated)

Every `search-filtering` requirement has at least one automated scenario or is a pure UI-interaction behavior deferred to the desktop pass:

- Text search (title/description/notes/tag name; blank pass-through) — covered.
- Tag filter AND + ancestor context + empty results — covered.
- Subtree scope (niche, group, listing-containing-topic, unresolvable) — covered.
- Archived exclusion by default + intentional inactive inclusion + non-canonical — covered.
- AND combination (text+tag, scope+text, single-dimension clearing) — covered.
- Parent-context preservation — covered (existing + new).
- Clear-all + expansion restore + selection retention — covered.
- Empty-results state + recovery — covered.
- Structural positioning guard while filtering (text and tag) — covered.
- Filter surface progressive disclosure — VM state covered; keyboard/focus/dismissal are UI behaviors for the desktop pass.

## Desktop UI Verification — Not Applicable (OpenCode)

**Status: not applicable.** This implementation was performed under OpenCode, which has no interactive desktop session and cannot launch the built Avalonia app for real keyboard/pointer interaction. Per the agent testing policy (see `AGENTS.md` → Testing, and `docs/qa-review.md` QA-6), the desktop UI verification pass is optional and non-blocking when the contributing agent lacks an interactive desktop; it is recorded as not-applicable here rather than skipped silently. The fast deterministic baseline above plus the UI-owned decision-logic tests are the accepted verification for this OpenCode run.

The desktop pass remains expected when a desktop-enabled contributor (e.g., Codex) picks this change up before release. The plan below is retained for that run.

Disposable workspace: use a throwaway store/niche/group/listing/tag set (never the contributor's normal workspace). Suggested isolation: a fresh in-memory or temporary SQLite workspace via the app's default workspace path override.

Scenarios to exercise (derive from accepted `search-filtering` scenarios):

1. Text search by listing title, by description/idea text, by notes, and by tag name; confirm ancestor topics remain visible as context and non-matches hide.
2. Tag filter: open Filters flyout, check one tag (tree narrows), check a second tag (AND narrows further), uncheck (re-widens). Confirm Filters button shows active indication.
3. Subtree scope: select a niche → enable "Current topic and below" (tree restricts); select a nested group → re-pin; select a listing → scope resolves to its containing topic; clear selection → option disables.
4. Include archived: archive a listing via properties; confirm it is hidden by default; enable "Include archived" → it appears with inactive (dimmed) styling; click it → it does NOT open a working tab or become creation context.
5. Combined filters: text + tag + scope together; clear one dimension and confirm the tree re-widens correctly.
6. Clear-all: with multiple dimensions active, click "Clear all" → every dimension resets, pre-filter expansion restores, selection preserved when still visible.
7. Empty results: enter text matching nothing → empty-state message + "Clear filters" action appears; click it → full tree returns.
8. Positioning guard: with any filter active, attempt sibling before/after placement → blocked with "Clear filtering before positioning…" guidance; clear filters → available again.
9. Keyboard-only flow: Tab to search box, type; Tab into Filters button, Enter to open flyout, Tab through controls, Space to toggle a tag, Escape to close (filters retained); confirm focus returns predictably.
10. Restart: with no filters active, restart the app on the same disposable workspace → unfiltered tree is intact (filter state is session-local, not persisted).

Record: build hash, OS, Avalonia version, screenshots or automation logs per scenario, results, isolation method, and any limitations.

## Limitations

- No schema/persistence change; filter state is session-local (verified by design — no repository writes for filter state).
- Stage/status filter selectors are intentionally absent (owned by FC-0105); the flyout is sectioned to accept them later.
- Asset-name, phrase, and graphic-direction text search deferred until those fields surface in navigation.
