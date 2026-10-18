# Windows Installer

The Windows setup program installs the 64-bit FusionCanvas application machine-wide under `%ProgramFiles%\FusionCanvas` by default. Windows asks for administrator approval because the installer writes application files and uninstall registration to protected, shared locations. The installer is built for `win-x64`; it does not default to `Program Files (x86)`.

The installer offers optional Start Menu and desktop shortcuts. Selected shortcuts are created for all users of the computer. The install page lets you choose another supported application directory.

## Upgrading

A machine-wide installation is upgraded in its registered directory. The first elevated setup run can migrate the original per-user default installation from `%LOCALAPPDATA%\Programs\FusionCanvas` to Program Files. If the old installation was placed in a custom per-user directory, uninstall that old version first, then run the new setup. This avoids deleting an elevated, registry-supplied path.

Workspace databases, settings, managed files, AI cache, and telemetry remain under each user's `%LOCALAPPDATA%\FusionCanvas`. Install, upgrade, and uninstall do not move or delete that user data.

## Uninstalling

Uninstalling requires administrator approval. It removes the machine-wide application files, registration, and installer-owned shortcuts. FusionCanvas data under each user's LocalAppData directory is preserved; remove it separately only if you intend to delete that user's workspace and settings.

## Candidate and stable builds

Merges to `main` and manual installer workflow runs produce retained installer candidate artifacts for testing. Stable releases are published separately through the repository's version-tag release workflow and include the signed installer, checksum, update manifest, and provenance evidence.
