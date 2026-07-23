# FusionCanvas C# Coding Standard

## Purpose

This document defines the default standard for production and test code in FusionCanvas. It turns the architectural direction in `docs/architecture.md` and the accepted requirements in `openspec/specs/architecture-guidelines/spec.md` into concrete coding rules.

The goals are to make code:

- easy to locate and understand
- correctly separated by Clean Architecture layer
- cohesive and independently testable
- consistent across contributors and coding agents
- simple to change without creating broad side effects

This standard applies to new code and to code modified as part of normal feature work. Existing violations are migration debt; they do not justify repeating a pattern, and they do not require an unrelated repository-wide rewrite.

Normative terms have their usual meaning:

- **MUST** and **MUST NOT** are required.
- **SHOULD** and **SHOULD NOT** are the default; deviations need a concrete reason.
- **MAY** is optional.

When this document conflicts with an accepted OpenSpec requirement, the OpenSpec requirement wins. Architecture responsibilities remain governed by `docs/architecture.md` and `openspec/specs/architecture-guidelines/spec.md`.

## 1. Organize by Layer, Then Capability

Production code MUST remain in the project that owns its responsibility:

| Project | Owns | Must not own |
| --- | --- | --- |
| `FusionCanvas.Domain` | Entities, value objects, invariants, calculations, policies, and domain events | Avalonia, SQLite, file-system, network, serialization, or orchestration concerns |
| `FusionCanvas.Application` | Use cases, workflow orchestration, ports, commands, queries, and application-facing models | Views or concrete persistence and external-service implementations |
| `FusionCanvas.Integration` | SQLite, managed files, external APIs, provider adapters, serialization at external boundaries | Domain rules, presentation state, or use-case policy |
| `FusionCanvas.App` | Avalonia views, view models, navigation, commands, converters, and presentation state | Domain rules, persistence logic, or direct SQL |

Project references MUST point inward:

```text
FusionCanvas.App ──────────┐
                          ├─> FusionCanvas.Application ─> FusionCanvas.Domain
FusionCanvas.Integration ─┘
```

Because `FusionCanvas.App` is the executable composition root, it MAY reference `FusionCanvas.Integration` to register and construct concrete adapters. That reference MUST remain in startup/composition code; views and view models MUST depend on application-facing contracts rather than concrete integration types.

Within each project, code MUST be grouped by cohesive product capability, not by a catch-all technical folder. A broad concept such as `Workspace` MAY be a namespace root, but it MUST NOT become the permanent home for every workspace-related type. Add a narrower capability namespace when a concept has its own behavior or changes for its own reasons.

Preferred examples:

```text
src/
├─ FusionCanvas.Domain/
│  ├─ Items/
│  │  ├─ Item.cs
│  │  ├─ ItemStatus.cs
│  │  └─ ItemWorkflowPolicy.cs
│  ├─ Assets/
│  │  ├─ Asset.cs
│  │  └─ AssetKind.cs
│  └─ Navigation/
│     └─ NavigationNode.cs
├─ FusionCanvas.Application/
│  ├─ Items/
│  │  ├─ IItemManagementService.cs
│  │  ├─ ItemManagementService.cs
│  │  ├─ ItemManagementCreateRequest.cs
│  │  └─ ItemManagementResult.cs
│  └─ Assets/
│     ├─ IAssetManagementService.cs
│     └─ AssetManagementService.cs
├─ FusionCanvas.Integration/
│  ├─ Persistence/
│  │  └─ SqliteWorkspaceRepository.cs
│  └─ Files/
│     └─ LocalWorkspaceFileStore.cs
└─ FusionCanvas.App/
   ├─ Items/
   │  ├─ ItemInspectorView.axaml
   │  └─ ItemInspectorViewModel.cs
   └─ Assets/
      ├─ AssetsWindow.axaml
      └─ AssetsViewModel.cs
```

Folder names SHOULD describe a domain or user capability such as `Items`, `Assets`, `Navigation`, or `StageTools`. Technical subfolders such as `Persistence`, `Files`, `Views`, and `Converters` are appropriate where the layer itself makes the capability clear.

Namespaces MUST normally match the project and folder path. Moving a type to a more cohesive capability includes moving its namespace unless compatibility requires a staged migration.

## 2. One Primary Type per File

Every top-level production type MUST normally have its own file. This includes:

- classes
- records and record structs
- interfaces
- enums
- structs
- delegates

The file name MUST match the type name, including its generic arity without the backtick notation. For example, `IWorkspaceRepository` belongs in `IWorkspaceRepository.cs`.

An interface and its implementation MUST be separate files. Requests, results, summaries, options, and state records MUST also have separate files when they are top-level types. A short declaration is not a reason to combine unrelated public types.

Allowed exceptions are:

- private nested types that are implementation details of the containing type
- generated code
- the code-behind partial class paired with an Avalonia `.axaml` file
- a small set of tightly coupled private test fixtures used by only one test class
- partial declarations required by a framework or source generator

An exception MUST improve locality without creating a second independently reusable responsibility. Multiple public or internal top-level types in one handwritten production file require an explicit code-review justification.

Types MUST NOT be grouped into files named `Models.cs`, `Entities.cs`, `Contracts.cs`, `Helpers.cs`, or `Common.cs`. These names conceal ownership and tend to accumulate unrelated responsibilities.

## 3. Type and Responsibility Design

Apply SOLID pragmatically:

- A type MUST have one cohesive reason to change.
- Dependencies MUST be explicit constructor parameters unless a framework owns construction.
- An abstraction MUST protect a real architectural boundary, external dependency, variation point, or useful testing seam.
- Interfaces SHOULD be small and consumer-focused. Do not create an interface merely to mirror every class.
- Prefer composition over inheritance. Use inheritance only for a genuine substitutable relationship or a framework requirement.
- Keep domain decisions in domain types or policies, and keep application services focused on orchestration.
- View models MAY coordinate presentation state but MUST NOT become alternate application services.
- Static mutable state and service locators MUST NOT be used.

Split a type when it combines independent responsibilities such as validation, persistence, mapping, orchestration, and presentation, or when changes repeatedly touch unrelated regions of the file. Line count alone is not a design rule, but a production type above roughly 300 lines or with more than seven direct dependencies SHOULD trigger a responsibility review.

Do not introduce speculative layers, generic repositories, base services, mediator pipelines, factories, or extension points without a current requirement. The simplest design that preserves the required boundary is preferred.

## 4. Type Selection and Data Modeling

- Use a `class` for objects with identity, mutable lifecycle, services, view models, and framework-owned components.
- Use a `record` for value-oriented data whose equality should be based on its contents.
- Use a `readonly record struct` or `readonly struct` only for small values where value semantics and copying costs are understood.
- Use an `enum` for a closed, stable set of named values. If values need behavior, evolving metadata, or external extensibility, use another model.
- Seal concrete classes and records unless inheritance is an intentional supported extension point.
- Prefer immutable state. Expose mutation through behavior that preserves invariants rather than public setters.
- Use domain-specific types when primitive values have important validation or are easily confused. Avoid wrapping every primitive without a meaningful invariant.
- Collection properties SHOULD expose `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, or another read-only contract unless callers are intended to mutate them.
- Do not expose a mutable collection owned by another type.

Domain entities MUST protect their invariants at construction and at every state transition. Persistence and UI code MUST NOT be the only place where a domain rule is enforced.

## 5. Naming

Names MUST communicate intent and use the project's product language.

| Element | Convention | Example |
| --- | --- | --- |
| Namespace, type, method, property, event | `PascalCase` | `ItemManagementService` |
| Interface | `I` + `PascalCase` | `IWorkspaceRepository` |
| Parameter and local variable | `camelCase` | `workspaceId` |
| Private instance field | `_camelCase` | `_repository` |
| Private or internal constant | `PascalCase` | `DefaultPageSize` |
| Boolean | Positive question or state | `IsArchived`, `CanMove`, `HasSelection` |
| Asynchronous method | `Async` suffix | `LoadAsync` |
| Test method | Behavior and outcome | `MoveAsync_WhenDestinationIsArchived_ReturnsFailure` |

Additional rules:

- Prefer complete words over abbreviations. Use common domain or technical abbreviations such as `Id`, `UI`, `SQL`, or `JSON` consistently.
- Use nouns for types and properties, verbs for actions, and verb phrases for methods.
- Avoid vague names such as `Manager`, `Processor`, `Handler`, `Helper`, `Utility`, `Data`, and `Info` unless the name also states the specific responsibility.
- Commands describe intent; queries describe information requested. Do not hide a state-changing operation behind a query name.
- Use C# keywords (`string`, `int`, `bool`) rather than framework aliases (`String`, `Int32`, `Boolean`) in C# code.

## 6. Source Layout and Formatting

- Use file-scoped namespaces.
- Place `using` directives outside the namespace and remove unused directives.
- Use four spaces for indentation and no tabs.
- Use Allman braces and always use braces for `if`, `else`, loops, and similar control flow, including single-line bodies.
- Put one statement and one declaration on each line.
- Prefer expression-bodied members only when the result remains immediately readable.
- Prefer `var` when the assigned type is obvious from the right-hand side or when spelling the type adds noise. Use the explicit type when it improves understanding.
- Prefer collection expressions and other current C# features when they improve clarity and are supported by the repository language version.
- Avoid deeply nested control flow. Prefer guard clauses and extracting cohesive operations.
- Keep lines readable; wrap complex expressions and argument lists rather than optimizing for a rigid character limit.
- Order members consistently: constants, static fields, instance fields, constructors, properties, public methods, internal methods, private methods, then nested types. Deviate when another order makes a small type materially easier to read.

Formatting SHOULD be automated with `dotnet format` and repository `.editorconfig` rules once those controls are adopted. Formatting-only changes SHOULD NOT be mixed with behavior changes across unrelated files.

## 7. Nullability and Validation

Nullable reference types MUST remain enabled. New warnings MUST NOT be suppressed merely to make a build pass.

- Use non-nullable types for required values and nullable types only when absence is valid.
- Validate arguments at the boundary where invalid data enters. Use `ArgumentNullException.ThrowIfNull` and specific argument exceptions for programmer errors.
- Use domain validation results or domain-specific exceptions for business rejection according to the owning use case; do not report expected user decisions as unexpected system failures.
- Do not use the null-forgiving operator (`!`) unless the invariant is evident and cannot be expressed to the compiler. Add a short explanation when it is not locally obvious.
- Avoid returning `null` collections. Return an empty collection.
- Avoid `default!`, broad warning suppression, and nullable annotations that misrepresent runtime behavior.

## 8. Asynchronous and Concurrent Code

- Use asynchronous APIs for I/O. Do not block tasks with `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` in application code.
- Return `Task` or `Task<T>` from asynchronous methods. Use `async void` only for framework event handlers.
- Public asynchronous operations that may wait or perform I/O MUST accept a `CancellationToken`, normally as the final parameter with a default only at an outer application boundary.
- Pass cancellation tokens through to downstream operations. Do not silently replace a caller's token.
- Re-throw `OperationCanceledException`; do not convert cancellation into a generic failure.
- Use `ValueTask<T>` only after measurement demonstrates that it benefits a hot path.
- Avoid fire-and-forget work. If it is unavoidable at a UI boundary, make ownership, exception observation, cancellation, and lifetime explicit.
- Shared mutable state requires an explicit synchronization and ownership strategy. Prefer immutable messages and serialized orchestration.

Library-style Domain, Application, and Integration code SHOULD use `ConfigureAwait(false)` where the surrounding codebase consistently does so. Avalonia UI code that must resume on the UI thread SHOULD rely on the captured UI context or dispatch explicitly.

## 9. Exceptions, Results, and Logging

- Throw exceptions for unexpected failures and invalid programmer input, not for ordinary branching.
- Catch only exceptions that can be handled, translated at a boundary, enriched with useful context, or cleaned up safely.
- Never use an empty `catch`.
- Avoid catching `Exception`. A boundary-level catch is allowed when it records or translates an otherwise unhandled failure; cancellation and fatal process conditions must retain their intended behavior.
- Preserve the original exception as `InnerException` when translating failures.
- Error messages MUST be actionable and MUST NOT expose secrets, tokens, connection strings, or sensitive local paths unnecessarily.
- Expected user-correctable outcomes MAY use explicit result types. Result types SHOULD be specific to a use case rather than one universal result abstraction.
- Logging belongs at application and integration boundaries. Domain logic SHOULD remain independent of logging frameworks.
- Use structured log properties rather than string concatenation when logging infrastructure is present.

## 10. Dependency Injection and Boundaries

- Register dependencies at the application composition root.
- Use constructor injection for required dependencies.
- Do not call the dependency-injection container from domain, application, integration adapter, view, or view-model code.
- Do not inject a broad service provider when the type needs a specific dependency.
- Interfaces owned by an inward layer define what that layer needs; adapters in an outer layer implement those interfaces.
- Mapping between persistence, provider, application, and presentation models SHOULD happen at the boundary. External DTOs MUST NOT leak into Domain.
- Avoid generic abstractions that expose persistence mechanics, SQL concepts, Avalonia types, or provider SDK types across an inward boundary.

## 11. Avalonia and MVVM

- Views own visual structure and framework-specific interaction.
- View models own presentation state, commands, selection, and UI coordination.
- Application services own use-case orchestration.
- Domain types and policies own business rules.
- Code-behind MUST be limited to view concerns that are impractical or less clear in markup, such as framework event adaptation or focus behavior.
- Use compiled bindings and explicit `DataType` declarations.
- Commands MUST not contain persistence or domain policy directly; they delegate to a view model operation or application service.
- UI-thread dispatch MUST be explicit when work can complete on a background thread.
- Converters MUST be pure presentation transformations and MUST NOT access repositories or services.

## 12. Persistence, Files, and External Input

- Use parameterized SQL. Never construct SQL by concatenating user-controlled values.
- Keep transactions explicit for operations that must succeed or fail together.
- Keep persistence schemas and serialization formats at the Integration boundary.
- Dispose database commands, readers, streams, and other owned resources promptly with `using` or `await using`.
- Validate and normalize external paths at the file-system boundary, and verify that managed paths remain under the intended workspace root.
- Treat database content, files, plugin data, network responses, and AI output as untrusted input.
- Do not log or commit secrets. Configuration holding secrets MUST remain outside source control.
- Use `DateTimeOffset` for persisted instants and UTC for generated timestamps unless an accepted requirement defines different semantics.

## 13. Comments and Documentation

Code SHOULD explain what happens; comments should explain why.

- Do not comment obvious syntax or restate the method name.
- Document non-obvious invariants, compatibility constraints, security decisions, and deliberate tradeoffs.
- Remove stale or commented-out code instead of preserving it in source.
- `TODO` comments MUST state the remaining action and, when available, reference an issue or OpenSpec change.
- Add XML documentation to public extension contracts and APIs whose correct usage is not self-evident. Internal implementation types do not need ceremonial XML comments.

## 14. Tests

Every behavior change MUST include focused tests at the lowest reliable layer, as required by the testing baseline and relevant OpenSpec scenarios.

- Mirror production capabilities in the corresponding test project.
- A production type named `ItemWorkflowPolicy` SHOULD normally have tests in `ItemWorkflowPolicyTests.cs`.
- Test one behavior per test. Multiple assertions are appropriate when they verify one outcome.
- Use Arrange–Act–Assert structure when it improves readability; comments are optional when the phases are obvious.
- Tests MUST be deterministic and isolated. Do not depend on execution order, wall-clock time, the contributor's files, network services, or shared mutable state.
- Inject clocks, identifier generators, and external collaborators when behavior depends on them.
- Use real domain objects and simple fakes where practical. Mock only at architectural boundaries and verify outcomes rather than incidental call sequences.
- Persistence and file tests MUST use isolated disposable resources.
- Avalonia behavior involving construction, bindings, focus, input, selection, or the visual tree MUST use deterministic headless tests when that framework behavior carries meaningful risk.
- A bug fix SHOULD first add a test that fails for the defect and passes after the correction.

The repository baseline is:

```powershell
dotnet test .\FusionCanvas.sln
```

## 15. Enforcement and Review

Standards that can be automated SHOULD be encoded in repository tooling rather than left to reviewer memory. The preferred enforcement path is:

1. a root `.editorconfig` for formatting, naming, and code-style rules
2. built-in .NET analyzers enabled at an agreed analysis level
3. warnings kept visible and warning-clean for changed code
4. `dotnet format --verify-no-changes` and `dotnet test .\FusionCanvas.sln` in CI
5. narrowly selected additional analyzers only when they provide durable value without excessive noise

Introducing or tightening analyzers SHOULD be a separate maintenance change. Existing diagnostics may be baselined temporarily, but new or modified code MUST follow this standard and MUST NOT expand the baseline.

Code review MUST check:

- correct layer and inward dependency direction
- cohesive capability folder and namespace
- one primary top-level type per file
- focused responsibilities and justified abstractions
- explicit nullability, cancellation, and error behavior
- tests at the appropriate layer
- security of external input, SQL, paths, files, and secrets

## 16. Incremental Adoption

Do not perform a repository-wide file and namespace reorganization as incidental work. Apply this standard using the following rule:

1. New types comply immediately.
2. A type being substantially changed SHOULD move to its own correctly named file.
3. A feature being substantially changed SHOULD adopt a cohesive capability folder and namespace when the move is bounded and reviewable.
4. Mechanical moves SHOULD be separated from behavior changes when combining them would obscure the review.
5. Large cleanup work SHOULD be planned as a dedicated maintenance change with compilation and test verification after each bounded step.

Refactoring existing structure must preserve accepted behavior. If a proposed reorganization changes public behavior, persistence compatibility, or another accepted contract, it follows the OpenSpec behavior-change workflow.

## References

This project standard adapts established .NET guidance to FusionCanvas:

- [Microsoft C# coding conventions](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [.NET identifier naming rules and conventions](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/identifier-names)
- [.NET code analysis configuration](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/configuration-files)
- [.NET code-style rule options](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/code-style-rule-options)
- [C# asynchronous return types](https://learn.microsoft.com/dotnet/csharp/asynchronous-programming/async-return-types)
