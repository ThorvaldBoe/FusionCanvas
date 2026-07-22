# FusionCanvas Rolling Delivery Roadmap

FusionCanvas plans and delivers one cohesive module at a time. This page is intentionally lightweight: detailed requirements, design, implementation plans, tasks, and acceptance evidence belong to the current module's OpenSpec change.

The roadmap is not a fixed feature inventory or phase commitment. It is revised after each module is verified so implementation evidence and user feedback can influence what comes next.

## Current Delivery Module

**Module:** Development quality and rolling module workflow

**OpenSpec change:** `adopt-module-delivery-workflow`
**Outcome:** Establish the process used to discover, specify, implement, and verify later product modules with explicit acceptance gates and safe lower-cost-agent handoffs.

Completion means the workflow is reflected in accepted process specs, canonical contributor guidance, shared OpenSpec skills, and QA/testing guidance, with strict OpenSpec validation and the repository test baseline passing.

## Candidate Next Module

Not selected.

The next module will be chosen collaboratively after the current module is verified and its learning review is complete. Selection starts from:

- current user goals and pain points;
- accepted OpenSpec behavior and the current application state;
- defects, friction, and opportunities found during verification;
- dependencies or gaps that block a coherent user outcome;
- recent change retrospectives;
- optional historical idea sources when useful.

Before proposal work, record the module's outcome, included feature set, dependencies, non-goals, key risks, verification approach, and why it is a coherent size. A fixed feature count is not used.

## Later Opportunities

Later opportunities stay deliberately brief until promoted to **Candidate Next Module**. They are not approved scope, requirements, ordering, or acceptance criteria.

- Complete and strengthen the local creative workspace.
- Expand the idea-to-concept-to-design-to-listing workflow.
- Improve asset, mockup, metadata, and batch-production support.
- Add optional contextual AI assistance after provider, privacy, and manual-fallback boundaries are clear.
- Expose proven extension points through a plugin platform.
- Add publishing integrations while preserving local authority.
- Add analytics and automation only after the underlying workflows are stable and observable.

Historical LifeOS planning files remain under `docs/LifeOS` as optional, potentially stale idea sources. Any idea reused from them must be revalidated through current module discovery and OpenSpec; the historical ordering and descriptions are not current commitments.

## Recently Completed Modules

Use archived OpenSpec changes under `openspec/changes/archive/` as the authoritative history. Add only short pointers here when they materially help choose the next module; do not rebuild a second feature catalog.

## Module Promotion Checklist

A candidate is ready to become the current module when:

- it has one clear user or platform outcome;
- included features belong together and share enough implementation or verification context to justify delivery together;
- dependencies and exclusions are explicit;
- high-impact product and architecture questions are resolved or intentionally delegated;
- acceptance scenarios are observable and can be mapped to verification;
- the implementation plan can be made explicit enough for the assigned agent;
- the expected code, review, and desktop-verification surface remains understandable and diagnosable.
