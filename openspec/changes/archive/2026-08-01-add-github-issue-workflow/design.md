## Context

FusionCanvas currently has no version-controlled Issue Forms or documented relationship between GitHub Issues and OpenSpec. The accepted OpenSpec workflow already permits small maintenance work that does not alter accepted behavior, but it does not define an intake or backlog system. This delivery module adds a lightweight external and internal work-management layer; it changes no application behavior.

## Goals / Non-Goals

**Goals:**

- Make structured bug and feature reporting available to external contributors.
- Make GitHub Issues the single place for candidate-work discussion, triage, priority, ownership, and delivery status.
- Preserve OpenSpec as the only authority for significant accepted product behavior and delivery-module verification.
- Make the transition from issue to OpenSpec change to pull request traceable and unambiguous.

**Non-Goals:**

- GitHub Discussions, Projects, milestones, sprints, automated triage, task-level issue mirroring, or mandatory issues for incidental maintenance.
- Application code, UI, data, runtime configuration, or CI behavior.
- A new SLA, fixed triage cadence, or contributor assignment process.

## Decisions

### Authority is split by responsibility

GitHub Issues are the source of truth for reports, requests, discussion, triage state, priority, ownership, and delivery tracking. OpenSpec is the source of truth for significant behavior, acceptance scenarios, design, implementation tasks, verification, and archived product history. Pull requests record implementation and close the issue on merge. This prevents an issue description or label from becoming an alternative specification.

### Intake is limited to two required forms

`.github/ISSUE_TEMPLATE/bug_report.yml` and `feature_request.yml` use GitHub Issue Forms with explicit required fields and default `type` plus `needs-triage` labels. A markdown warning prohibits credentials, personal information, private workspaces, and other sensitive content. `config.yml` disables blank issues. This creates a consistent public front door without adding a general-purpose discussion channel.

### Labels are small and mutually understandable

Issues have one type label and one workflow-state label. Priority labels are added only when needed. Completion is represented by closing the issue, not a status label. Duplicate, declined, and unreproducible reports are closed manually with an explanatory comment and, where relevant, a canonical link.

### Promotion occurs when an accepted feature becomes the next delivery module

An accepted high-level feature stays in GitHub until it is selected for detailed work. Discovery then follows the existing OpenSpec lifecycle before implementation. The resulting `proposal.md` contains an `## Origin` section with the issue number and URL, while the issue links to the change. The proposal resolves ambiguity; it does not copy the issue.

One independently deliverable module has one primary issue. If a broad issue splits into independently deliverable modules, it remains a tracking issue and linked child issues become the primary issue for each OpenSpec change. A maintenance bug that only restores accepted behavior may skip a dedicated OpenSpec change but retains its issue and focused regression coverage when practical.

### Delivery uses GitHub-native linkage

Work branches use `codex/<issue-number>-<slug>`. Pull requests name the linked OpenSpec change when present and use `Closes #<issue-number>`, allowing the merge to close the primary issue. OpenSpec verification and archive remain independent completion gates; an issue closure never substitutes for them.

### Repository configuration has a tracked specification and an external setup step

Issue Form files and policy are version-controlled. Enabling Issues and creating labels are GitHub repository settings, so the contributor documentation includes an exact setup checklist and verification path. No GitHub Action is introduced merely to enforce labels or sync states.

## Risks / Trade-offs

- [GitHub settings drift from tracked form defaults] → Document the required labels and verify repository settings after deployment; forms apply their default labels only when those labels exist.
- [Issue text and OpenSpec behavior diverge] → Require promotion backlinks and state explicitly that OpenSpec supersedes the issue for behavior and acceptance criteria.
- [Label taxonomy grows into process overhead] → Restrict this module to the documented type, status, and optional priority labels; add any new taxonomy only through an intentional workflow change.
- [Reporters disclose sensitive material] → Present the warning before all form fields and direct security-sensitive reports away from public issues in contributor documentation.

## Migration Plan

1. Merge the tracked forms and workflow documentation.
2. Repository maintainers ensure Issues are enabled, create the listed labels, and confirm blank issues are disabled.
3. Submit one disposable report through each form to confirm rendering and automatic labels.
4. Begin applying the policy only to newly created issues; existing work is not retroactively converted unless a maintainer chooses to track it.

Rollback consists of disabling Issues or removing the forms and reverting the documentation change. No application data, schema, or compatibility migration is involved.

## Acceptance-to-Verification Mapping

| Acceptance scenario | Verification method |
| --- | --- |
| Structured public bug and feature intake | Inspect YAML schema; preview and submit a disposable issue through each rendered form; confirm required fields and default labels. |
| Sensitive-content guidance and blank-issue restriction | Inspect rendered form warning and `config.yml`; verify the new-issue chooser has no blank issue option. |
| Authority split and direct-bug path | Review OpenSpec delta, `CONTRIBUTING.md`, and `AGENTS.md` against the documented bug and feature examples. |
| Promotion, split-module, and PR closure traceability | Review the three end-to-end examples in contributor guidance for issue → OpenSpec → PR behavior. |
| Repository label and Issue availability | Confirm settings and labels through GitHub after merge; record manual evidence in `verification.md`. |

## Implementation Plan

1. Add delta specifications for the new `github-issue-workflow` capability and the existing `openspec-project-workflow` capability. They define responsibilities, forms, triage labels, direct-maintenance exceptions, promotion, split tracking, and closure rules without encoding file implementation details.
2. Add the Issue Form YAML files and `config.yml`. Keep field identifiers stable and use only GitHub-supported Issue Form types; assign `type` and `status: needs-triage` labels automatically.
3. Create `CONTRIBUTING.md` as the human-facing operational policy, including the exact label inventory, repository-admin setup checklist, sensitive-report route, triage decision table, link conventions, and three worked examples.
4. Add a concise `AGENTS.md` workflow note requiring agents to consult the primary issue, preserve OpenSpec authority, use the specified branch/PR links, and stop when an issue reveals a behavior decision not yet specified.
5. Add a short README link to the contributor guide. Do not add issue-template policy to application documentation or create an automation workflow.
6. Validate the OpenSpec package strictly, parse the YAML files, inspect the rendered GitHub forms and settings, and run the existing solution test baseline as the repository completion gate. No Avalonia test is applicable because no desktop surface changes.

## Open Questions

None. Maintainer triage cadence and assignment remain intentionally unspecified.
