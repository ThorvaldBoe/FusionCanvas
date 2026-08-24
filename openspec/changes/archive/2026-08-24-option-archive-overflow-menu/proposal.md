## Why

On **Manage Variants → Available choices**, every Option card (Color, Size, and custom kinds) shows a large red **Archive Option** button beside the routine **Manage values** action. Archiving is an infrequent destructive action; its current size and color make it visually dominant and let it compete with the everyday value-management workflow.

## What Changes

- Replace the large red **Archive Option** button on each Available-choice Option card with a compact three-dot (ellipsis) overflow button in the card's upper-right corner.
- The overflow button opens a small context menu that contains **Archive option** as a clearly destructive, non-dominant entry.
- The overflow trigger has an accessible name such as **More actions for Color**, is keyboard focusable, and opens the menu through standard keyboard interaction.
- Dismissing the menu without a selection makes no change.
- Selecting **Archive option** invokes the existing `ArchiveOptionCommand` unchanged, preserving all current dependency checks, confirmations, error messages, and blocked-archive behavior.
- **Manage values** remains directly available on the card as the routine action.
- The change applies consistently to Color, Size, and custom Option kinds.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `variant-management`: Update the Available choices requirement so each compact Option summary exposes a three-dot overflow menu with a non-dominant **Archive option** action while keeping **Manage values** directly available and preserving archive eligibility, dependency checks, confirmations, and error reporting.

## Impact

- **App (Avalonia view):** `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml` — the option choice-card data template (approximately lines 754-772): replace the `Button Classes="danger"` "Archive Option" control with an icon-only overflow `Button` hosting a `MenuFlyout` containing a destructive `MenuItem`.
- **App (code-behind):** `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml.cs` — the choice-card focus-return helper (`OnOptionChoiceFocusRequested`) must continue to target the card's routine action; optionally add an automation identifier for the overflow button.
- **App (view models):** no command or state change expected; `CatalogSetupViewModel.ArchiveOptionCommand`, `RunArchive`, and `CatalogSetupService.ArchiveAsync` are reused verbatim.
- **Domain/Application/Integration:** no changes.
- **Tests:** focused Avalonia headless view tests in `tests/FusionCanvas.App.Tests/` (extending `StoreEditorHeadlessTests.cs` style) covering pointer open, keyboard open, destructive menu-item behavior, focus-return, and accessible-name behavior.
- **Design tooling:** `docs/Visuals/ui-descriptions/manage-variants.ui.yaml` currently has no Archive affordance; no template change is required, but the implementation should match the `choice-card`/`manage` wording conventions.

## Delivery Scope and Verification

This is one cohesive, independently verifiable UI module because it changes a single card template, reuses an existing command path, and relies on one established menu pattern. No high-impact product, UX, data, or architecture decision is left open: the archive action keeps its exact current semantics and surface placement rules, and only its presentation weight changes on the card. Verification will use deterministic Avalonia headless view tests for pointer, keyboard, focus-return, and accessible-name behavior, the full solution test baseline (`dotnet test .\FusionCanvas.sln`), and strict OpenSpec validation.