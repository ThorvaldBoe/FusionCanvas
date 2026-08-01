## Context

The current Bug Report form and accepted `github-issue-workflow` specification require a version or commit, but FusionCanvas has no releases yet.

## Goals / Non-Goals

**Goals:** allow reports to be submitted without version information while continuing to invite it when available.

**Non-Goals:** changing any other Bug Report field, label, workflow rule, or application behavior.

## Decisions

Remove the form field's `required: true` validation and change its description to state that the information is optional. Update the complete corresponding specification requirement block so the durable requirement matches the form.

## Risks / Trade-offs

- [Less diagnostic context in some reports] → Keep the field visible and explain that it is useful when available.

## Implementation Plan

1. Modify the Bug Report Issue Form validation and help text.
2. Modify the `GitHub Issues provide structured work intake` requirement delta and validate the exact form/spec alignment.
3. Strictly validate the OpenSpec change and run the solution test baseline; no UI tests apply.
