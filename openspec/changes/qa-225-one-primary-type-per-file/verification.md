# Verification — One Primary Type per File

| Capability / scenario | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| Domain, Application, Integration, and App handwritten production files contain at most one top-level type | Structural source scan and focused App test | Pass | `ProductionFiles_ContainAtMostOneTopLevelType` passed; manual scan reported no remaining multi-type files. |
| Type-named files preserve compilability and existing behavior | Solution build and test baseline | Pass | `dotnet build .\\FusionCanvas.sln --no-restore -m:1 -p:BuildInParallel=false` passed with 143 pre-existing warnings and 0 errors. |
| Structural change does not alter accepted product behavior | Diff inspection and focused test | Pass | Changes are declaration moves, file names, OpenSpec artifacts, and the source-layout test; focused test passed. |
| Strict OpenSpec specs and active changes remain valid | `openspec validate --specs --strict`; `openspec validate --changes --strict` | Pass | Validation completed after the architecture delta and change artifacts were added. |
| Formatting verification | `dotnet format --verify-no-changes` | N/A | Not run as a separate command because the repository already has broad pre-existing formatting debt; build/test and source-layout checks are the completion evidence for this structural-only change. |
