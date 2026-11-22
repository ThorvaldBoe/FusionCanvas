## Why

Users currently have to discover the public GitHub issue form themselves, and useful reproduction details or diagnostics may be omitted. A focused report flow in FusionCanvas can explain how to prepare a useful public report, help users capture optional evidence, and hand them to the existing GitHub intake without transmitting anything automatically.

## What Changes

- Add a **Report an issue** action to the existing global utility rail and a focused preparation dialog with clear directions for describing where the issue occurred, what they were trying to do, and what happened.
- Let users explicitly enable and disable workspace Debug Mode from the dialog, reproduce the issue, copy a bounded recent diagnostic window to the clipboard, and clear only telemetry captured during that report session.
- Let users paste a screenshot from the Windows clipboard into the dialog; retain it as a temporary attachment with a preview, independently of the diagnostics clipboard.
- Open the public GitHub Bug Report form in the default browser. The user reviews and submits there, pastes copied diagnostics, and optionally uploads the prepared screenshot.
- Update the GitHub bug report form to provide a dedicated optional screenshot upload and reinforce public visibility and sensitive-content guidance.

## Capabilities

### New Capabilities

- `in-app-issue-reporting`: Prepare a privacy-conscious issue report in FusionCanvas, optionally capture a screenshot and recent diagnostics, and hand the user to GitHub for review and submission.

### Modified Capabilities

- `github-issue-workflow`: Extend the existing public bug report form with screenshot upload guidance and preserve the existing issue taxonomy and required report details.

## Impact

- App shell, report dialog/view model, clipboard-image handling, temporary screenshot storage, OS browser launching, and workspace telemetry settings/export/targeted deletion.
- GitHub issue form YAML and its accepted workflow specification.
- No external intake service, GitHub credential in the desktop app, automatic issue submission, or automatic code changes.
- User-facing UI verification will include framework-free report-flow tests and Avalonia headless coverage for dialog construction, bindings, control state, and clipboard/report actions. No Appium journey is planned: browser submission remains an explicit external handoff and no native service is introduced.
