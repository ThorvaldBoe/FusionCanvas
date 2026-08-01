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
| `fc-coordinator` | primary | `openrouter/moonshotai/kimi-k3` | Owns workflow state and routing; writes OpenSpec artifacts only, never production code |
| `fc-spec-reviewer` | subagent | `openrouter/z-ai/glm-5.2` | Reviews the delivery package for omissions, contradictions, architecture risks, and testability; never edits |
| `fc-implementer` | subagent | `openrouter/deepseek/deepseek-v4-flash` | Implements approved task slices and maintains task/verification evidence |
| `fc-verifier` | subagent | `openrouter/moonshotai/kimi-k3` | Final verification that intent, artifacts, code, tests, and behavior agree; never edits |

## Running the loop

1. Restart OpenCode after adding or changing files in `.opencode/` — agents are loaded at startup.
2. Switch to the `fc-coordinator` primary agent (Tab cycles agents).
3. Say: `Start an iterative OpenSpec change for: <feature description>`.

The coordinator explores and proposes with the standard skills, then loops:

```text
Specify -> fc-spec-reviewer -> Implement (fc-implementer) -> Verify (fc-verifier) -> Archive
```

Each quality gate is capped at 3 automatic revisions. Stylistic preferences, unrelated refactors, optional enhancements, and future scope never keep the loop going. Before spec sync or archive, the coordinator stops and asks for your explicit approval.

For the first few changes, supervise each handoff.

## Notes and deliberate limitations

- Models are OpenRouter IDs set in each agent's frontmatter; adjust them there if you switch providers or model versions.
- There is no separate architect or spec-writer agent: exploration and proposal writing use the standard `openspec-explore` and `openspec-propose` skills. Add specialists only after observing a concrete failure mode.
- The verifier needs no dedicated verify skill: verification follows the change's `verification.md` plus the scoped completion QA in `docs/qa-review.md` and the `qa-review-baseline` spec.
- The agents are project-scoped (they live in this repo and travel with it), not global agents.
