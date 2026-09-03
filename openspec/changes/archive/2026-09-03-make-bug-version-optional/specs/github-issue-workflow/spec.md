## MODIFIED Requirements

### Requirement: GitHub Issues provide structured work intake
FusionCanvas SHALL use GitHub Issues for externally reported bugs and feature requests, separately tracked internal bugs, and high-level planned features. The repository SHALL provide required Bug Report and Feature Request Issue Forms, SHALL disable blank issues, and SHALL warn reporters not to submit credentials, personal information, private workspace data, or other sensitive content. The Bug Report form SHALL request version or commit information when available but SHALL NOT require it.

#### Scenario: External contributor reports a bug
- **WHEN** an external contributor selects the Bug Report form
- **THEN** the form requires operating system, expected behavior, actual behavior, reproduction steps, and frequency information
- **AND** the form accepts a report when version or commit information is omitted
- **AND** the created issue receives `type: bug` and `status: needs-triage` labels

#### Scenario: External contributor requests a feature
- **WHEN** an external contributor selects the Feature Request form
- **THEN** the form requires the problem, current workaround, desired outcome, affected product area, and alternatives
- **AND** the created issue receives `type: feature` and `status: needs-triage` labels

#### Scenario: Reporter needs to disclose sensitive information
- **WHEN** a reporter opens either Issue Form
- **THEN** the form warns that public issues must not contain credentials, personal information, private workspace data, or other sensitive material
- **AND** contributor guidance directs security-sensitive reports away from public issues
