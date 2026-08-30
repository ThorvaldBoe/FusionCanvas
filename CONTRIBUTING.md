# Contributing to FusionCanvas

Thank you for helping improve FusionCanvas. GitHub Issues are the durable records for reports, requests, evidence, discussion, and completion. The GitHub Project named **FusionCanvas Issues** is the operational Kanban view and status source for all work. OpenSpec remains the source of truth for accepted product behavior, acceptance criteria, design, implementation tasks, verification, and archived history.

## Report a bug or request a feature

Use the Bug Report or Feature Request form. They provide the information maintainers need to investigate the report and apply an initial label automatically.

Repository issues are public, including any raw notes, logs, or screenshots attached to them. Remove API keys, passwords, tokens, personal information, private workspace contents, and other sensitive material before posting. Do not report a potential security vulnerability publicly; use a private reporting channel instead.

## Triage and labels

Maintainers use labels for issue classification, triage, and supplementary state. The Project's `Status` field, rather than issue labels, is the authoritative operational workflow stage.

| Group | Labels |
| --- | --- |
| Type | `type: bug`, `type: feature` |
| Workflow state | `status: needs-triage`, `status: needs-information`, `status: accepted`, `status: in-progress`, `status: blocked`, `status: declined` |
| Optional priority | `priority: next`, `priority: soon`, `priority: backlog` |

Completed work is represented by a closed issue, not a `done` label. Maintainers close duplicates with a link to the canonical issue and close declined or unreproducible reports with a concise reason.

Repository maintainers must enable Issues, create the labels above with these exact names, and keep blank issues disabled. The Issue Forms assign their type and `status: needs-triage` labels automatically, so both labels must exist before accepting reports.

## Project board workflow

Use the **FusionCanvas Issues** GitHub Project as the single Kanban overview of every issue's current state. Repository issues and OpenSpec artifacts remain the delivery records behind that overview:

| Project status | Meaning and action |
| --- | --- |
| **Backlog** | The unstarted queue. Keep work here while it still needs triage, clarification, or prioritization. |
| **Ready** | The issue is understood, valid, and sufficiently prepared for someone to pick up. |
| **In progress** | Actual work has begun. For behavior changes, this triggers the existing OpenSpec workflow: explore when appropriate, then propose, review, apply, and verify according to the process below. |
| **In review** | Implementation is ready for validation, testing, and acceptance review. |
| **Done** | The work has been accepted. Close the linked repository issue on completion; configured Project workflows may synchronize the Project status from the closed issue. |

The Project automatically adds new or updated open issues from the FusionCanvas repository. Issues created through Codex or directly in the repository therefore appear on the board. Conversely, the Project's **Create new issue** flow immediately creates and links a repository issue.

Do not create a second issue or card for the same work. A Project card represents its linked repository issue; dragging it between columns changes only the Project status and does not create another issue. Keep the description, evidence, discussion, links to any OpenSpec change or pull request, and open/closed state on the repository issue.

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

## Versioning and releases

FusionCanvas uses [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) to generate the application version from the repository.

- The manually maintained `Major.Minor` version lives in [`version.json`](version.json) at the repository root. The `Build` component is generated automatically from Git version height and is never stored.
- A single central build dependency in [`Directory.Build.props`](Directory.Build.props) applies Nerdbank.GitVersioning to every Clean Architecture project. Layer `.csproj` files must not set a competing `Version`, `VersionPrefix`, `PackageVersion`, or `AssemblyVersion` property.
- The same Git commit produces the same version whether it is built locally or in GitHub Actions. The canonical build number is derived from the repository, **not** from `github.run_number`.
- The user-facing version is exposed in Settings → About, and the About section provides a copyable diagnostic block (version, short commit id, platform) for bug reports.

### GitHub Actions checkout

GitHub Actions builds must preserve full Git history so Nerdbank.GitVersioning can compute the build number:

```yaml
- name: Checkout
  uses: actions/checkout@v4
  with:
    fetch-depth: 0
```

A shallow checkout builds successfully but produces a less useful version.

### Release tag convention

Releases use the Git tag form:

```text
vMajor.Minor.Build
```

Example: `v0.4.127`.

The Git tag, application version, release title, and artifact filename use the same `Major.Minor.Build` value:

```text
Application version: 0.4.127
Git tag:              v0.4.127
Release title:       FusionCanvas 0.4.127
Artifact:            FusionCanvas-0.4.127-win-x64.zip
```

Creating the complete GitHub release workflow is outside the scope of the versioning module; this convention documents the intended alignment.
