# Reorganize Integration Capability Folders Verification

## Status

Complete. The merged implementation at commit `df1a672` matches the approved maintenance change, and fresh deterministic verification passes.

## Acceptance Evidence

| Capability / scenario | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| Architecture guidelines / Contributor locates an item domain type | Structure inspection | Pass | Dependent Domain capability layout remains intact. |
| Architecture guidelines / Contributor reviews the shared domain root | Structure inspection | Pass | The narrow Domain `Workspace/` root remains compliant. |
| Architecture guidelines / Contributor locates an application use case | Structure inspection | Pass | Dependent Application capability layout remains intact. |
| Architecture guidelines / Contributor locates a persistence or file-storage adapter | Structure and namespace inspection | Pass | SQLite repository is under `Integration/Persistence`; file stores are under `Integration/Files`; `Integration/Workspace/` is absent. |
| Architecture guidelines / Contributor opens a domain file | Automated source-layout inspection | Pass | Domain one-type-per-file result remains clean. |
| Architecture guidelines / Contributor opens an application file | Automated source-layout inspection | Pass | Application one-type-per-file result remains clean. |
| Architecture guidelines / Contributor opens an integration file | Automated source-layout inspection | Pass | Every reviewed Integration production `.cs` file has one detected top-level type, with matching filename and folder namespace. |

## Deterministic Gates

- Build: `dotnet build .\FusionCanvas.sln -m:1` passed on 2026-07-27 with 0 warnings and 0 errors.
- Tests: `dotnet test .\FusionCanvas.sln -m:1 --no-build` passed on 2026-07-27: Domain 96, Application 150, Integration 50, App/headless 186; total 482, failed 0, skipped 0.
- Strict OpenSpec validation: `openspec validate reorganize-integration-capability-folders --strict` passed on 2026-07-27.
- Whitespace: `git diff --check` was clean before evidence updates.

## Scoped Completion QA

- Scope: the Integration folder/namespace refactor, composition references, and mirrored tests.
- Architecture: Integration references Application only; adapter responsibilities remain outside the inward layers.
- Persistence/files: implementation bodies were mechanically moved; the 50 isolated Integration tests and full baseline pass.
- Security/UI: no boundary behavior changed; path and persistence regression tests remain green.
- Drift: implementation, proposal, design, tasks, and delta scenarios agree.
- Findings: none.
