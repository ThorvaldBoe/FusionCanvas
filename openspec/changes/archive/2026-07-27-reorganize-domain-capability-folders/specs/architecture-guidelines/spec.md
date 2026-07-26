## ADDED Requirements

### Requirement: Domain layer groups types by cohesive capability
The FusionCanvas Domain layer SHALL group production types into cohesive capability folders and namespaces (for example `Items`, `Stores`, `Niches`, `Groups`, `Tags`, `Assets`, `Prompts`, `Workflow`, `Navigation`), with a narrow shared root for cross-cutting primitives, rather than a single catch-all folder.

#### Scenario: Contributor locates an item domain type
- **WHEN** a contributor looks for the `Item` domain entity or item-related rules and policies
- **THEN** they are found under the `Items` capability folder and `FusionCanvas.Domain.Items` namespace
- **AND** a single `Workspace` folder is not the home for every workspace-related domain type

#### Scenario: Contributor reviews the shared domain root
- **WHEN** a contributor inspects the shared `FusionCanvas.Domain.Workspace` root
- **THEN** it contains only cross-cutting primitives such as `Workspace`, `WorkspaceDefaults`, `WorkspaceEntity`, `WorkspaceEntityKind`, and `WorkspaceSnapshot`
- **AND** capability-specific types live in their own capability folders

### Requirement: Domain layer keeps one primary type per file
The FusionCanvas Domain layer SHALL keep one primary top-level type per file, with the file name matching the type name, per the C# coding standard.

#### Scenario: Contributor opens a domain file
- **WHEN** a contributor opens any Domain layer file
- **THEN** it contains at most one top-level public or internal type
- **AND** the file name matches that type name
- **AND** no file is named with a vague grouping term such as `Entities.cs`, `Models.cs`, or `Relationships.cs`
