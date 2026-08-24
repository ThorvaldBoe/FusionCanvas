## 1. View model accessible naming

- [x] 1.1 Add `AccessibleOverflowName` (and optional stable automation id) to `OfferingChoiceGroupViewModel` so the overflow control exposes an accessible name such as `More actions for Color`.
- [x] 1.2 Add a focused framework-free test asserting `AccessibleOverflowName` reflects the Option name for Color, Size, and custom kinds.

## 2. Headless view tests (write before view change)

- [x] 2.1 Add a headless test that opens Variant management and asserts the current direct **Archive Option** button is NOT present on any Option card.
- [x] 2.2 Add headless tests asserting each Option card exposes **Manage values** as the routine action.
- [x] 2.3 Add a headless test asserting the overflow menu exists per card and contains a destructive **Archive option** entry bound to `ArchiveOptionCommand` for the card's Option.

## 3. View implementation

- [x] 3.1 Rework the choice-card data template in `StoreEditorWindow.axaml`: add an upper-right icon-only overflow `Button` with a `MenuFlyout` containing the destructive **Archive option** `MenuItem`; remove the old `Classes="danger"` **Archive Option** button.
- [x] 3.2 Bind the menu item to `CatalogSetup.ArchiveOptionCommand` with `CommandParameter="{Binding Option}"` and set the accessible name / automation id on the overflow trigger.
- [x] 3.3 Add any required `iconButton`-style and destructive-menu-item styles to `StoreEditorWindow.axaml` consistent with `MainWindow.axaml`.

## 4. Interaction verification

- [x] 4.1 Add headless tests covering pointer open, keyboard open, menu dismissal with no change, and focus-return to the overflow button on dismissal.
- [x] 4.2 Add a headless test asserting archive eligibility/blocked behavior still surfaces the existing recoverable error through `ArchiveOptionCommand` (referenced-Option path).
- [x] 4.3 Verify the card works identically for Color, Size, and a custom Option kind fixture.

## 5. Baseline and validation

- [x] 5.1 Run `dotnet test .\FusionCanvas.sln` and resolve any failures.
- [x] 5.2 Run `openspec validate option-archive-overflow-menu --strict`.
- [x] 5.3 Complete `verification.md` mapping every acceptance scenario to method, result, evidence, and limitations.