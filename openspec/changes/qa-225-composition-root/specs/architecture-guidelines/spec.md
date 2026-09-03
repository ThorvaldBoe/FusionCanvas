## MODIFIED Requirements

### Requirement: Composition root owns concrete integration construction
The App composition root SHALL construct concrete Integration adapters and inject them through Application-facing contracts into presentation types.

#### Scenario: Main-window presentation is composed for production
- **WHEN** the desktop App creates the main window and its view model
- **THEN** concrete workspace file stores, workspace transfer adapters, CSV codecs, and other Integration adapters are created by an App composition factory
- **AND** `MainWindow` and `MainWindowViewModel` receive application-facing contracts or already-composed runtime services
- **AND** those presentation types do not instantiate concrete types from `FusionCanvas.Integration`

#### Scenario: A view model is constructed directly by a test
- **WHEN** a test constructs a presentation type without the production composition root
- **THEN** its fallback behavior uses an explicit non-Integration test-safe default or a supplied application contract
- **AND** the production Integration assembly is not required by that fallback path
