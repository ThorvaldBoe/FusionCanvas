## ADDED Requirements

### Requirement: Users can prepare and submit a public issue report from FusionCanvas
FusionCanvas SHALL provide a discoverable Report an issue action in the global utility rail. The preparation dialog SHALL explain what information makes a report useful, identify GitHub reports and attachments as public, and provide a clear action that opens the existing public Bug Report issue form in the default browser. FusionCanvas SHALL NOT submit the report or transmit report content automatically.

#### Scenario: User opens report preparation
- **WHEN** the user activates Report an issue from the utility rail
- **THEN** FusionCanvas opens a focused report preparation dialog
- **AND** the dialog explains that the user should describe where the issue occurred, what they were trying to do, and what happened compared with what they expected
- **AND** the dialog explains that GitHub issues and attachments are public and warns against including credentials, personal information, or private workspace data

#### Scenario: User reports an issue without an active workspace
- **WHEN** the user opens report preparation without an active workspace
- **THEN** report guidance, screenshot staging, and the GitHub handoff remain available
- **AND** workspace Debug Mode and telemetry actions are unavailable with a clear explanation

#### Scenario: User continues to GitHub
- **WHEN** the user activates Report Issue
- **THEN** FusionCanvas opens the repository's existing Bug Report issue form in the default browser
- **AND** the report dialog remains available while the user completes the browser handoff
- **AND** FusionCanvas does not create an issue or send the prepared screenshot or diagnostics

#### Scenario: Browser handoff is unavailable
- **WHEN** the default browser cannot be opened
- **THEN** FusionCanvas keeps the dialog and its pending screenshot available
- **AND** presents a recoverable error with the repository issue-form address available to copy

### Requirement: Report diagnostics are explicitly captured, copied, and cleared
FusionCanvas SHALL let the user explicitly enable or disable Debug Mode for the active workspace from the report dialog. A report capture SHALL identify its own start time, copy only sanitized diagnostic entries recorded during that capture and within the most recent ten minutes, and allow the user to clear records from that report capture without deleting older workspace telemetry. Copying SHALL place text on the local clipboard only after an explicit user action. Turning Debug Mode off SHALL stop additional capture and SHALL NOT silently clear records.

#### Scenario: User starts a report capture
- **WHEN** the active workspace has Debug Mode disabled and the user enables it from the report dialog
- **THEN** FusionCanvas enables Debug Mode for that workspace and records the report capture start time
- **AND** the dialog explains that the user should reproduce the issue before copying diagnostics

#### Scenario: Debug Mode was already enabled when report preparation opened
- **WHEN** the user opens report preparation for a workspace whose Debug Mode is already enabled
- **THEN** the control reflects the enabled state
- **AND** FusionCanvas starts the report capture boundary when the dialog opens so earlier telemetry is excluded from this report

#### Scenario: User copies recent diagnostics
- **WHEN** the user requests Copy diagnostics after reproducing the issue
- **THEN** FusionCanvas copies sanitized entries from the active report capture that fall within the latest ten-minute window
- **AND** the action excludes request and response bodies and other non-allowlisted payload fields
- **AND** the button immediately displays “Copied to clipboard” after a successful copy
- **AND** FusionCanvas does not open the browser or submit the diagnostics as part of copying

#### Scenario: No report diagnostics are available
- **WHEN** the report capture has no diagnostic entries in the latest ten-minute window
- **THEN** FusionCanvas explains that no diagnostics are available yet and keeps Copy diagnostics unavailable
- **AND** FusionCanvas does not replace the clipboard contents

#### Scenario: User stops capture and clears report telemetry
- **WHEN** the user disables Debug Mode after copying diagnostics and chooses to clear the report capture
- **THEN** FusionCanvas stops capture for the active workspace
- **AND** deletes only telemetry recorded since this report capture began
- **AND** retains telemetry recorded before this report capture

#### Scenario: User disables capture before copying
- **WHEN** the user disables Debug Mode before copying diagnostics
- **THEN** FusionCanvas stops additional capture
- **AND** retains the report capture records until the user copies them or explicitly clears them

#### Scenario: Active workspace changes during report capture
- **WHEN** the user changes the active workspace while a report capture is in progress
- **THEN** FusionCanvas stops the report capture and clears only records belonging to that report session
- **AND** does not copy or clear records from the newly selected workspace

### Requirement: Users can stage a screenshot from the clipboard as a separate attachment
FusionCanvas SHALL let the user explicitly paste an image from the operating-system clipboard into the report dialog. FusionCanvas SHALL retain the image separately from clipboard text, show a preview, allow replacement or removal, and keep the prepared image available for attachment to the GitHub issue form. FusionCanvas SHALL delete its temporary copy when the report dialog is dismissed or the application exits, and SHALL NOT upload it automatically.

#### Scenario: User stages a screenshot
- **WHEN** the user captures a screenshot with an operating-system tool and activates Paste screenshot in the report dialog
- **THEN** FusionCanvas reads the clipboard image only in response to that action
- **AND** stores a temporary image copy and displays its preview
- **AND** leaves subsequent diagnostics copying independent of the staged screenshot

#### Scenario: User replaces or removes a screenshot
- **WHEN** the user chooses another clipboard image or removes the staged screenshot
- **THEN** FusionCanvas replaces or deletes its temporary copy and updates the preview state
- **AND** does not change clipboard contents as a side effect

#### Scenario: User opens GitHub with a screenshot staged
- **WHEN** the user opens the GitHub issue form while a screenshot is staged
- **THEN** FusionCanvas keeps the report dialog and screenshot available
- **AND** explains how to attach the staged image using the GitHub form's optional upload field
- **AND** does not upload the image automatically

#### Scenario: User dismisses the report dialog
- **WHEN** the user cancels or closes the report dialog
- **THEN** FusionCanvas deletes its temporary screenshot copy
- **AND** leaves the user's original clipboard contents and any earlier telemetry untouched
