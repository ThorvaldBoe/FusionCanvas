## ADDED Requirements

### Requirement: Per-window geometry persists locally with backward compatibility

FusionCanvas SHALL persist per-window geometry as an optional section of the versioned local application-settings document, keyed by a stable window identity, while preserving readable appearance, AI, and main-window layout settings when the section is absent, malformed, or partially invalid.

#### Scenario: Settings document without a per-window geometry section loads cleanly
- **WHEN** FusionCanvas loads a settings document written by a version that persisted only the main window layout section
- **THEN** the main window layout is restored from that section
- **AND** every secondary window uses its default placement
- **AND** appearance and AI settings remain readable and usable

#### Scenario: A single per-window geometry entry is invalid
- **WHEN** the per-window geometry section contains one malformed or out-of-range entry alongside valid entries
- **THEN** FusionCanvas discards only the invalid entry
- **AND** preserves the remaining geometry entries, the main window layout, and the rest of the readable settings
