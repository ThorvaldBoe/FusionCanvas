# Contributing to FusionCanvas

Thank you for helping improve FusionCanvas. GitHub Issues are the front door for reports, requests, discussion, prioritization, ownership, and delivery tracking. OpenSpec remains the source of truth for accepted product behavior, acceptance criteria, design, implementation tasks, verification, and archived history.

## Report a bug or request a feature

Use the Bug Report or Feature Request form. They provide the information maintainers need to investigate the report and apply an initial label automatically.

Public issues must not contain API keys, passwords, tokens, personal information, private workspace contents, or other sensitive material. Do not report a potential security vulnerability publicly; use a private reporting channel instead.

## Triage and labels

Maintainers give each active tracked issue one label from each applicable group:

| Group | Labels |
| --- | --- |
| Type | `type: bug`, `type: feature` |
| Workflow state | `status: needs-triage`, `status: needs-information`, `status: accepted`, `status: in-progress`, `status: blocked`, `status: declined` |
| Optional priority | `priority: next`, `priority: soon`, `priority: backlog` |

Completed work is represented by a closed issue, not a `done` label. Maintainers close duplicates with a link to the canonical issue and close declined or unreproducible reports with a concise reason.

Repository maintainers must enable Issues, create the labels above with these exact names, and keep blank issues disabled. The Issue Forms assign their type and `status: needs-triage` labels automatically, so both labels must exist before accepting reports.

## From issue to delivery

An issue is a request or work record; it is not a specification.

- A confirmed bug that only restores existing accepted behavior can be fixed directly from its issue. Add a focused regression test where practical and link the issue from the pull request.
- An accepted high-level feature stays in the GitHub backlog until it is chosen as the next delivery module. It then follows OpenSpec: explore, propose, implement, verify, learn, and archive before implementation is complete.
- A promoted change adds `## Origin` to `proposal.md`, containing the primary issue number and URL. Update the issue with the OpenSpec change name and link.
- One independently deliverable OpenSpec module has one primary issue. If a broad issue becomes several independently deliverable modules, retain it as a tracking issue and create linked child issues; each child has its own primary OpenSpec change and pull request.

For work with a primary issue, use the branch name `codex/<issue-number>-<slug>`. The pull request identifies the OpenSpec change when one exists and contains `Closes #<issue-number>`. GitHub closes that issue only when the pull request merges; this never replaces OpenSpec verification or archive.

## Examples

### Regression fix

Issue `#42` reports that an existing specified Save action remains disabled after a valid edit. The maintainer confirms the accepted specification already requires saving, adds a focused regression test, fixes the code, and opens a pull request with `Closes #42`. No new OpenSpec change is needed.

### Planned feature

Issue `#57` requests a way to manage multiple mockup configurations. After triage it is `status: accepted` and remains a high-level backlog item. When selected as the next module, maintainers create `openspec/changes/57-multiple-mockup-configurations/`, add `## Origin` with the issue link, follow the OpenSpec lifecycle, and merge a PR containing `Closes #57`.

### Broad request split into modules

Issue `#71` asks for marketplace publishing. Discovery shows independent Etsy export and Shopify export outcomes. Issue `#71` remains the tracking issue, while linked child issues become the primary work records for their separate OpenSpec changes and pull requests.
