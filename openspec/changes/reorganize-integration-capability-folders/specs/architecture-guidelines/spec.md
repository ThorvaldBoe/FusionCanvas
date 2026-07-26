## MODIFIED Requirements

### Requirement: Domain layer groups types by cohesive capability
The FusionCanvas production layers SHALL group production types into cohesive capability folders and namespaces (for example `Items`, `Stores`, `Niches`, `Groups`, `Tags`, `Assets`, `Prompts`, `Workflow`, `Navigation` in Domain; the matching capability folders in Application; and technical subfolders such as `Persistence` and `Files` at the Integration layer where the layer itself makes the capability clear), with a narrow shared root for cross-cutting primitives where justified, rather than a single catch-all folder.

#### Scenario: Contributor locates an item domain type
- **WHEN** a contributor looks for the `Item` domain entity or item-related rules and policies
- **THEN** they are found under the `Items` capability folder and `FusionCanvas.Domain.Items` namespace
- **AND** a single `Workspace` folder is not the home for every workspace-related domain type

#### Scenario: Contributor reviews the shared domain root
- **WHEN** a contributor inspects the shared `FusionCanvas.Domain.Workspace` root
- **THEN** it contains only cross-cutting primitives such as `Workspace`, `WorkspaceDefaults`, `WorkspaceEntity`, `WorkspaceEntityKind`, and `WorkspaceSnapshot`
- **AND** capability-specific types live in their own capability folders

#### Scenario: Contributor locates an application use case
- **WHEN** a contributor looks for an application-layer use case such as item management or group management
- **THEN** the service, its interface, and its request/result/state records are found under the matching capability folder (for example `Items/` or `Groups/`)
- **AND** the Application project does not keep every workspace-related type in a single `Workspace/` folder

#### Scenario: Contributor locates a persistence or file-storage adapter
- **WHEN** a contributor looks for the SQLite workspace repository or the local workspace file store
- **THEN** the repository is found under a `Persistence` folder and the file store under a `Files` folder at the Integration layer
- **AND** the Integration project does not keep every adapter in a single `Workspace` folder

### Requirement: Domain layer keeps one primary type per file
The FusionCanvas production layers SHALL keep one primary top-level type per file, with the file name matching the type name, per the C# coding standard.

#### Scenario: Contributor opens a domain file
- **WHEN** a contributor opens any Domain layer file
- **THEN** it contains at most one top-level public or internal type
- **AND** the file name matches that type name
- **AND** no file is named with a vague grouping term such as `Entities.cs`, `Models.cs`, or `Relationships.cs`

#### Scenario: Contributor opens an application file
- **WHEN** a contributor opens any Application layer file
- **THEN** it contains at most one top-level public or internal type
- **AND** a service's interface, its request/result/state records, and its implementation each live in separate files
- **AND** no file is named with a vague grouping term such as `Contracts.cs`, `Models.cs`, or `Management.cs` when it bundles multiple top-level types

#### Scenario: Contributor opens an integration file
- **WHEN** a contributor opens any Integration layer file
- **THEN** it contains at most one top-level public or internal type
- **AND** the file name matches that type name
