## MODIFIED Requirements

### Requirement: Domain layer keeps one primary type per file
The FusionCanvas production layers SHALL keep one primary top-level type per file, with the file name matching the type name, per the C# coding standard. This requirement applies to Domain, Application, Integration, and App production code, except for private nested implementation types, generated code, Avalonia code-behind partial classes paired with their `.axaml` file, and framework-required partial declarations.

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

#### Scenario: Contributor opens an App file
- **WHEN** a contributor opens any App production file
- **THEN** it contains at most one top-level public or internal type unless the file is an allowed framework or generated exception
- **AND** the file name matches the primary type name
