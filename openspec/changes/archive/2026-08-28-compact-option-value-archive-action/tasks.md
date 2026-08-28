## 1. Dialog presentation

- [x] 1.1 Change Option Value rows to a star/auto grid that keeps long value names readable and the archive action aligned.
- [x] 1.2 Replace the repeated full archive label and solid danger styling with a compact visible **Archive** action using restrained destructive styling.
- [x] 1.3 Add a target-specific accessible name while preserving the existing command and row parameter.

## 2. Focused verification

- [x] 2.1 Add Avalonia headless coverage for representative Color, Size, custom, and long Option Value rows, including label, style, layout, automation name, order, and command behavior.
- [x] 2.2 Update the relevant UI description if it names the prior row action and correct OpenSpec artifacts if implementation reveals a mismatch.
- [x] 2.3 Create `verification.md` mapping every acceptance scenario to its focused evidence and limitations.

## 3. Completion gates

- [x] 3.1 Run the focused App tests and correct failures.
- [x] 3.2 Run `dotnet test .\FusionCanvas.sln` and record the result.
- [x] 3.3 Run strict OpenSpec validation for specs and changes and record the result.
- [x] 3.4 Complete the learning review and retrospective for archive.
