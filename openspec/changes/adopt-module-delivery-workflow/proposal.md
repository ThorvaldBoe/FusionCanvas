## Why

Feature work has been planned too far ahead and implemented in batches too large for specifications, reviewers, and lower-cost implementation models to hold reliably in context. FusionCanvas needs a rolling, module-sized delivery process with explicit acceptance gates, implementation-ready guidance, and risk-based use of scarce desktop UI verification.

## What Changes

- Replace PRD-led phase implementation with a rolling module workflow: define the next cohesive delivery module, establish shared understanding, approve its OpenSpec change, implement it, verify it, learn from it, and only then commit the next module.
- Require each module to state its goal, feature set, dependencies, boundaries, risks, and why its size is reviewable; module size is judged by cohesion and verification cost rather than a fixed feature count.
- Require requirements and conceptual/functional design to be separated from a detailed implementation plan that is explicit enough for the assigned implementation agent to execute without inventing product or architecture decisions.
- Make acceptance criteria and their verification mapping a mandatory implementation gate. Failed criteria trigger correction and re-verification rather than partial acceptance.
- Allocate desktop UI testing by user risk and information value: deterministic checks remain the default feedback loop, targeted desktop checks cover changed workflows and high-risk integration points, and full regression remains a milestone/release activity.
- Define capability-based agent assignments and handoffs. Higher-reasoning agents are preferred for module discovery, specification, review, and ambiguous corrections; lower-cost agents may implement bounded, approved tasks when the artifacts are sufficiently explicit.
- Demote the LifeOS roadmap and PRDs to optional historical idea sources. Agents do not need to read them and must not treat their feature inventory, ordering, or acceptance text as current direction.
- Replace the phase-oriented roadmap with a rolling module-delivery roadmap that records only the current/next module and a lightweight later-opportunities backlog.
- UX preflight is not applicable because this change governs the development process and does not add an application-facing interaction.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `openspec-project-workflow`: Add rolling module scope, collaborative understanding, implementation-readiness, acceptance gates, agent handoffs, and historical-planning-source rules.
- `testing-baseline`: Make feature verification explicitly traceable to acceptance criteria and desktop UI testing risk-based and proportionate.
- `qa-review-baseline`: Add module-completion QA and verification-evidence checks without turning every module into a full repository QA review.

## Impact

- Accepted process specifications under `openspec/specs/`.
- OpenSpec artifact context and rules in `openspec/config.yaml`.
- Contributor and agent guidance in `AGENTS.md`, `README.md`, and `docs/`.
- OpenSpec workflow skills shared by Codex and OpenCode.
- No production code, runtime API, persistence schema, or application UI changes.
