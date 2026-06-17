## MODIFIED Requirements

### Requirement: Main window presents an initial workspace shell
The main window SHALL present a clean, mostly empty initial page suitable for an application with no workspace loaded.

#### Scenario: User views the first shell
- **WHEN** the main window is displayed
- **THEN** the user sees a clean empty page with minimal FusionCanvas identity
- **AND** the page does not display dense placeholder navigation, tabs, workflow stages, loading cards, unavailable cards, or fake workspace content

#### Scenario: User sees no workspace content before workspace behavior exists
- **WHEN** no persistent workspace data exists
- **THEN** the application displays an intentionally empty starting surface without requiring storage setup
- **AND** it does not imply that workspace navigation, product pipeline, listing, mockup, plugin, AI, marketplace, or automation behavior is available
