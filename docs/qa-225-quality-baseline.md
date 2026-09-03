# QA-225 compiler and formatting baseline

Recorded 2026-09-03 after the structural and composition-root maintenance changes.

## Compiler and analyzer diagnostics

The serial solution build is the deterministic baseline command:

```powershell
$env:AVALONIA_TELEMETRY_OPTOUT='1'
dotnet build .\FusionCanvas.sln --no-restore -m:1 -p:BuildInParallel=false -v:minimal
```

The measured result is 0 errors and 143 warnings. The warnings are existing repository debt, primarily xUnit cancellation-analyzer diagnostics (`xUnit1051`), with a smaller set of nullable, unused-event, and assertion-style diagnostics. The count is retained as a tracked baseline; future maintenance work must not increase it, and warning families should be reduced in focused batches.

## Formatting verification

The requested formatter gate is:

```powershell
dotnet format .\FusionCanvas.sln --verify-no-changes --no-restore
```

On this desktop runner the command currently stops before formatting analysis because the Roslyn build host cannot open its named pipe (`UnauthorizedAccessException`). This is an execution-environment limitation, not a formatter-clean result. The repository therefore records the formatter gate as unresolved and requires a CI or permitted runner recheck before this debt can be closed.

## Scope rule

This baseline does not suppress warnings or weaken analyzer configuration. It records the observed debt and establishes a no-regression gate while focused warning and formatting batches are delivered.
