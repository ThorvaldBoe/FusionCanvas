# Add Application Settings Retrospective

## Outcome

A focused Settings window provides a compact entry point for application-wide preferences. The General section offers a persistent Light/Dark appearance toggle applied immediately across all open windows. The Workspace section displays the current workspace and launches the existing workspace-management window. A compact workspace indicator replaced the permanent sidebar cog row in the navigation-pane header.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Workspace administration deserved permanent navigation-pane space. | Workspace management is an occasional task displacing daily creative workflow. | Replace the sidebar cog row with a compact workspace indicator; route management through Settings. | UX / scope | Reusable for occasional-configuration placement | Captured in `desktop-application-foundation` spec. |
| UI guidance described Dark as the default. | The accepted setting is Light as the initial default. | Reconcile UI guidance to allow user-selected Light/Dark with Light as the initial default. | Doc drift | Reusable for default-appearance decisions | Captured in `desktop-application-foundation` spec and UI guidance. |
| Hard-coded colors were sufficient for theming. | Mixed themes appear when hard-coded colors are not replaced by semantic resources. | Introduce shared semantic Light/Dark theme resources for all application windows. | Architecture | Reusable for application-wide theme switching | Captured in `desktop-application-foundation` spec. |

## Deferred or Change-Specific Notes

- Per-workspace themes, system-theme following, custom palettes, additional settings sections, inline workspace editing, plugin-contributed settings, cloud sync, and preference import/migration are non-goals.
- Settings file corruption or write failure is handled gracefully without altering workspace data.
- The settings file is stored outside the workspace database in application-local data.
