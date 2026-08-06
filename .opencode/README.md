# FusionCanvas OpenCode Agent Loop

This directory holds project-scoped OpenCode agents that drive the FusionCanvas OpenSpec workflow as a bounded loop with automatic review and verification gates.

**OpenSpec remains the backbone.** These agents are one of three equivalent ways to run feature work; they all produce and consume the same OpenSpec artifacts and honor the same completion gates.

## Three equivalent ways to run feature work

1. **Codex + OpenSpec skills** — use the shared skills in `.codex/skills/`: `openspec-explore`, `openspec-propose`, `openspec-apply-change`, `openspec-sync-specs`, `openspec-archive-change`.
2. **OpenCode + the same skills** — the root `opencode.json` registers `.codex/skills`, so the identical skill set is available in OpenCode sessions.
3. **OpenCode + the fc- agent loop** — the agents in `.opencode/agents/` orchestrate the same lifecycle, with the coordinator delegating review, implementation, and verification to specialized subagents.

All three operate on the same artifacts (`proposal.md`, delta specs, `design.md`, `tasks.md`, `verification.md`, `retrospective.md`) and the same gates: criterion-level verification, strict OpenSpec validation, the `dotnet test .\FusionCanvas.sln` baseline, and explicit user approval before spec sync and archive. Because the artifacts — not the tool — carry the state, a change can move freely between Codex and OpenCode at any point.

## The agents

| Agent | Mode | Model | Role |
| --- | --- | --- | --- |
| `fc-coordinator` | primary | `openrouter/z-ai/glm-5.2` | Owns workflow state and routing; sole communicator with subagents; writes OpenSpec artifacts only, never production code |
| `fc-spec-writer` | subagent | `openrouter/deepseek/deepseek-v4-flash` | Runs `openspec explore` and `openspec propose`; calls `fc-reviewer` after each stage |
| `fc-reviewer` | subagent | `openrouter/z-ai/glm-5.2` | Single reviewer of explore, proposal, specs, and code; never edits |
| `fc-architect` | subagent | `openrouter/z-ai/glm-5.2` | Read-only architecture consultant; invoked via coordinator |
| `fc-ui-specialist` | subagent | `openrouter/z-ai/glm-5.2` | Read-only UI/UX consultant; invoked via coordinator |
| `fc-business-analyst` | subagent | `openrouter/z-ai/glm-5.2` | Read-only business/product-strategy consultant; invoked via coordinator |
| `fc-image-viewer` | subagent | `openrouter/google/gemini-3.5-flash-lite` | Read-only vision specialist: inspects/describes images and answers questions about them |
| `fc-researcher` | subagent | `openrouter/google/gemini-3.5-flash-lite` | Read-only internet-research specialist for current information |
| `fc-implementer` | subagent | `openrouter/deepseek/deepseek-v4-flash` | Implements approved task slices and maintains task/verification evidence |
| `fc-verifier` | subagent | `openrouter/z-ai/glm-5.2` | Final verification only; never edits, never performs git/GitHub mutations |

## Running the loop

1. Restart OpenCode after adding or changing files in `.opencode/` — agents are loaded at startup.
2. Switch to the `fc-coordinator` primary agent (Tab cycles agents).
3. Say: `Start an iterative OpenSpec change for: <feature description>`.

The coordinator drives exploration and proposal through `fc-spec-writer`, then the loop:

```text
Explore (fc-spec-writer) -> fc-reviewer -> Propose (fc-spec-writer) -> fc-reviewer -> Implement (fc-implementer) -> fc-reviewer -> Verify (fc-verifier) -> Archive
```

**Routing principle:** whenever a subagent needs support, lacks the capability to answer a query, or hits an ambiguous decision, it asks the coordinator. The coordinator routes to the appropriate specialist (architect / ui-specialist / business-analyst / image-viewer / researcher / reviewer / verifier) rather than resolving it speculatively. The only direct subagent-to-subagent call is `fc-spec-writer` -> `fc-reviewer`; everything else flows through the coordinator so routing stays visible and iteration caps hold.

Each quality gate is capped at 3 automatic revisions. Stylistic preferences, unrelated refactors, optional enhancements, and future scope never keep the loop going. Before spec sync or archive, the coordinator stops and asks for your explicit approval.

For the first few changes, supervise each handoff.

## Notes and deliberate limitations

- Models are OpenRouter IDs set in each agent's frontmatter; adjust them there if you switch providers or model versions.
- Sub-agents are intentionally read-only where applicable and do not reach specialists directly; all consultation routes through the coordinator (see Routing principle above).
- The verifier needs no dedicated verify skill: verification follows the change's `verification.md` plus the scoped completion QA in `docs/qa-review.md` and the `qa-review-baseline` spec.
- The agents are project-scoped (they live in this repo and travel with it), not global agents.
- All agents allow `external_directory` access to sibling worktrees (`*/FusionCanvas-*/**`) plus a shared set of read-only git and PowerShell pipeline commands, because the coordinator's workflow runs each change in `..\FusionCanvas-<slug>`; without the worktree rule, every file and shell action there prompts once per directory. The pattern intentionally trusts any directory named `FusionCanvas-*` so it stays machine-agnostic in a public repo.
