# Retrospective

## Validation feedback

- **Original assumption:** A per-user install under `%LOCALAPPDATA%\Programs\FusionCanvas` was the preferred default because it avoids administrator approval.
- **Observed feedback:** While testing the installer, the user asked to use Program Files or Program Files (x86) as the default. After the elevation and machine-wide implications were explained, the user chose to change the specification to Program Files.
- **Approved correction:** New installations default to the 64-bit `%ProgramFiles%\FusionCanvas` path and use elevated machine-wide registration and selected shared shortcuts. The x64 application does not target Program Files (x86). Existing initial per-user installations migrate on an elevated upgrade; per-user application data stays in `%LOCALAPPDATA%\FusionCanvas`.
- **Applicability:** This feedback applies to the Windows installer capability and its upgrade/uninstall contract.
- **Classification:** User-approved product behavior change prompted by installer validation, not an implementation defect.
- **Promotion target:** The `windows-installer` capability specification, with implementation and fresh verification required before archive/sync.
