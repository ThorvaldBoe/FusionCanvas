## 1. Playbook

- [x] 1.1 Create `docs/qa-review.md` defining the QA review process: execution modes (full review and per-task), review protocol, severity scale, standard report format, and fix-routing rules
- [x] 1.2 Define QA-1 (SOLID principles and code design) with a pragmatic checklist and method
- [x] 1.3 Define QA-2 (Clean Architecture and dependency direction) with project reference and domain purity checks
- [x] 1.4 Define QA-3 (testing and coverage) with baseline run, structural coverage mapping, and per-layer test quality checks
- [x] 1.5 Define QA-4 (security and vulnerabilities) with NuGet hygiene commands, secrets scan, and injection-vector review
- [x] 1.6 Define QA-5 (specification and documentation drift) with spec/code/docs alignment checks

## 2. Agent Integration

- [x] 2.1 Create root `AGENTS.md` as the shared agent guide covering required context, the OpenSpec workflow, architecture rules, coding principles, testing, security, and QA review usage (read automatically by both Codex and OpenCode)
- [x] 2.2 Create root `opencode.json` registering `.codex/skills` as a skill path so OpenCode loads the same OpenSpec workflow skills as Codex without duplication

## 3. Specification

- [x] 3.1 Create the `qa-review-baseline` delta spec with requirements for the documented process, design/architecture review, testing review, security review, drift review, and finding routing

## 4. Validation and Completion

- [ ] 4.1 Run `openspec validate` (or equivalent status checks) for the change and resolve any issues
- [ ] 4.2 Review the change artifacts with the project owner
- [ ] 4.3 Sync the delta spec into `openspec/specs/qa-review-baseline/spec.md`
- [ ] 4.4 Archive the change after acceptance
