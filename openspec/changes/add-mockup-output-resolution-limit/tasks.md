## 1. Resolution policy and Store persistence

- [x] 1.1 Add the Domain mockup-output resolution policy with the 2000-pixel default, positive whole-pixel validation, proportional output-dimension calculation, and scaled mapping calculation.
- [x] 1.2 Add Application Store mockup-output settings records, contract, and service; read missing or malformed metadata as the default, preserve unknown metadata keys, and persist valid values.
- [x] 1.3 Add focused Domain and Application tests for defaulting, valid round-trip persistence, invalid values, and metadata preservation.

## 2. Raster generation and provenance

- [x] 2.1 Extend the mockup compositor contract and ImageSharp implementation to downscale before compositing, preserve aspect ratio, avoid upscaling, and keep PNG output.
- [x] 2.2 Update mockup generation to resolve the Store policy, pass it to the compositor, and record effective width, height, and policy value in generated asset metadata.
- [x] 2.3 Add or update Integration and Application tests for portrait, landscape, square, below-limit, placement, provenance, and failure-compensation behavior.

## 3. Store mockup dialog

- [x] 3.1 Load the Store-wide maximum-long-edge value into `CatalogSetupViewModel`, include it in draft/dirty-state handling, validate it, save it through the Application service, and keep it read-only for archived Stores.
- [x] 3.2 Add the simple labelled TextBox and helper text to `MockupTemplateEditorWindow`, including accessible naming and inline validation presentation.
- [x] 3.3 Wire the settings service through the application composition root and existing Store/catalog view-model construction without breaking existing test constructors or optional integrations.
- [x] 3.4 Add focused view-model and Avalonia headless tests for binding, default display, validation, save/reload, helper text, and archived read-only behavior.

## 4. Verification and delivery evidence

- [x] 4.1 Run focused Domain, Application, Integration, and App tests; correct any failed acceptance criterion and rerun affected regressions.
- [x] 4.2 Run `openspec validate` and record criterion-level results and limitations in `verification.md`.
- [x] 4.3 Run the required `dotnet test .\FusionCanvas.sln` baseline and record the result in `verification.md`.
