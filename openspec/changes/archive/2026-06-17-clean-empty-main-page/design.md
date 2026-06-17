## Context

The current Avalonia `MainWindow` starts with a full placeholder workspace: menu commands, header actions, side navigation, tabs, workflow stage chips, loading state, empty state, unavailable state, and retry action. This was useful for proving shell regions, but it now makes the app feel busier and more complete than it is.

The requested change is to start FusionCanvas on a nice, clean, empty page. The app should still feel intentionally launched, but it should not show fake workspace data, future feature regions, or inactive workflow controls.

## Goals / Non-Goals

**Goals:**
- Make the startup main window visually calm and mostly empty.
- Keep a minimal FusionCanvas identity signal.
- Remove placeholder navigation, fake workspace content, workflow tabs/stages, loading cards, unavailable cards, and inactive command-heavy UI.
- Keep the existing Avalonia startup path and main window type.
- Preserve build and test health.

**Non-Goals:**
- Implement workspace creation, opening, persistence, navigation, product pipeline, listing, mockup, plugin, AI, or marketplace behavior.
- Introduce a new design system or app-wide theme.
- Add interactive onboarding.
- Move UI logic into application/domain layers.

## Decisions

### Use a minimal empty main window

Replace the dense `DockPanel` shell with a simple centered or gently positioned empty surface that contains only the app name and, optionally, a short neutral subtitle.

Rationale: the app should feel clean and intentional without suggesting that unfinished controls are usable.

Alternative considered: keep the menu/header but remove content. This still leaves many inactive actions and makes the empty app feel like a disabled product rather than a deliberate starting state.

### Avoid fake workspace regions

Remove the placeholder tree, tabs, workflow stage indicators, loading progress bar, unavailable state, retry action, and explanatory cards.

Rationale: those controls imply future behavior that is not implemented yet and visually compete with the desired empty launch experience.

Alternative considered: keep one empty-state card with action buttons. That would be useful once workspace create/open behavior exists, but it would be premature before those commands do real work.

### Keep the change UI-only

Implement the change in `src/FusionCanvas.App/Views/MainWindow.axaml`, with no domain, application, or integration behavior changes.

Rationale: the request is purely about initial presentation, and the architecture guidelines keep UI concerns in the UI layer.

Alternative considered: create a view model now. There is no behavior to model yet, so that would add abstraction without value.

## Risks / Trade-offs

- The app may look too empty -> Mitigation: keep a small brand/title signal so the window does not feel broken.
- Removing placeholder regions could make future layout work less visible -> Mitigation: future features should add real regions when their behavior exists.
- UI smoke tests may need updates -> Mitigation: adjust tests only if they inspect startup UI content.

## Migration Plan

1. Replace the current main window markup with a simpler empty launch surface.
2. Keep the existing `MainWindow` code-behind unless compile errors require cleanup.
3. Run `dotnet build FusionCanvas.sln`.
4. Run `dotnet test FusionCanvas.sln`.

## Open Questions

- Should the empty page include a very small subtitle, or only the FusionCanvas name?
