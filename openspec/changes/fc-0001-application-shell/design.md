## Context

FusionCanvas currently launches to a simple Avalonia main window with centered identity text. FC-0001 moves that foundation forward into a usable application shell while staying inside Phase 0 boundaries: it must establish the workspace regions future features will inhabit, but it must not imply that navigation tree behavior, tab management, persistence, marketplace integrations, plugins, AI, or product workflow automation already exist.

The change is UI-owned and belongs in `FusionCanvas.App`. Domain, application, and integration layers remain unchanged unless a tiny presentation model is useful for testable shell state decisions.

## Goals / Non-Goals

**Goals:**

- Present a launchable main window with recognizable FusionCanvas identity.
- Establish a stable shell layout with left navigation and right document window regions.
- Reserve document window areas for tabs, workflow stage navigation, and detail content.
- Communicate empty, loading, and error states without depending on real workspace data.
- Provide shell-level command locations for future actions.
- Keep implementation simple enough for later FC-0005, FC-0008, and FC-0009 work to replace placeholders without redesigning the shell.

**Non-Goals:**

- Implement full navigation tree behavior.
- Implement full tab creation, persistence, or document lifecycle management.
- Implement interactive workflow stage behavior beyond visible stage positions.
- Implement store, niche, group, listing, marketplace, plugin, AI, persistence, or automation features.
- Add external services or new architectural layers.

## Decisions

1. Build the shell directly in `FusionCanvas.App` as an Avalonia main-window composition.

   Rationale: FC-0001 is a presentation foundation, and adding domain or application abstractions for static shell structure would create premature architecture. Later features can introduce view models or application contracts when behavior appears.

   Alternative considered: create cross-layer shell services now. This was rejected because the shell does not yet coordinate domain behavior or external state.

2. Use named visual regions and lightweight presentation state instead of fake product data.

   Rationale: The PRD needs the user to understand where navigation, tabs, workflow stage context, and detail views will live, but the accepted foundation spec still forbids implying unavailable product behavior. Placeholder labels and empty states should describe region purpose without sample stores, groups, listings, or mock workflow objects.

   Alternative considered: include sample workspace content. This was rejected because it would blur Phase 0 scope and could be mistaken for implemented navigation or product pipeline behavior.

3. Represent workflow position as a non-interactive stage strip in the shell.

   Rationale: The FC-0001 shell must reserve space for Idea -> Concept -> Design -> Listing, while FC-0008 owns complete workflow stage navigator behavior. A visible, non-interactive strip keeps the layout stable and leaves behavioral ownership for FC-0008.

   Alternative considered: implement clickable stage navigation now. This was rejected because current data and active-item context do not exist yet.

4. Add tests only for UI-owned decisions that can be verified without brittle rendering assertions.

   Rationale: Static XAML visual markup does not need superficial unit tests, but any introduced shell state model, region naming, or selection decision logic should have lightweight tests in `FusionCanvas.App.Tests`.

   Alternative considered: snapshot-test the whole Avalonia view. This was rejected because it would be brittle and not aligned with the current test baseline.

## Risks / Trade-offs

- [Risk] Placeholder regions could look like functioning product features -> Mitigation: use empty-state language and disabled/non-interactive affordances where behavior is reserved for later work.
- [Risk] A purely static XAML shell could become hard to evolve -> Mitigation: keep named regions and extract only small presentation models when needed by tests or future binding.
- [Risk] Visual polish could consume time before product behavior exists -> Mitigation: aim for clean, restrained shell structure rather than production-level UI styling.

## Migration Plan

1. Replace the existing centered startup content with the new shell layout in `FusionCanvas.App`.
2. Add any minimal UI presentation model and tests needed for shell state decisions.
3. Verify the solution builds and tests pass.

Rollback is straightforward: restore the previous main-window XAML and remove any shell presentation test/model additions.

## Open Questions

- Which exact shell-level commands should be active in Phase 0 versus shown only as reserved command locations?
- Should the first empty state invite opening/creating a workspace, or remain a neutral "no workspace selected" state until workspace behavior is specified?
