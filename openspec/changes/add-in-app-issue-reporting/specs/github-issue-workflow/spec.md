## ADDED Requirements

### Requirement: Public bug reports accept optional screenshot uploads
The repository SHALL provide an optional screenshot upload field in the public Bug Report issue form. The form SHALL explain that screenshots attached to public issues are publicly accessible and SHALL remind reporters to review screenshots for credentials, personal information, private workspace data, and other sensitive content before uploading.

#### Scenario: Reporter attaches a screenshot
- **WHEN** a reporter completes the public Bug Report issue form
- **THEN** the reporter can optionally upload a screenshot
- **AND** the created issue includes the uploaded image as evidence
- **AND** the screenshot remains optional

#### Scenario: Reporter reviews public screenshot guidance
- **WHEN** a reporter opens the screenshot upload field
- **THEN** the form explains that uploaded evidence is public
- **AND** reminds the reporter to remove sensitive material before uploading
