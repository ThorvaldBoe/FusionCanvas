## 1. Architecture Documentation

- [ ] 1.1 Update `docs/architecture.md` to state that FusionCanvas follows Clean Architecture for meaningful implementation work.
- [ ] 1.2 Add layer responsibility guidance for domain, application, integration, and UI layers.
- [ ] 1.3 Document inward dependency direction and examples of dependencies that must stay outside the domain layer.
- [ ] 1.4 Clarify how Clean Architecture fits with incremental complexity and the existing single Avalonia shell.

## 2. Project Guidance

- [ ] 2.1 Update `openspec/project.md` architectural direction with the clean layer structure.
- [ ] 2.2 Update repository guidance, such as `README.md`, where it describes future project structure or architecture documents.
- [ ] 2.3 Define preferred project names or naming guidance for domain, application, integration, and UI projects.

## 3. Validation

- [ ] 3.1 Run OpenSpec validation/status checks for `clean-architecture-guidelines`.
- [ ] 3.2 Confirm the updated guidance satisfies the architecture-guidelines requirements.
- [ ] 3.3 If project files are changed during implementation, build the solution to confirm existing app behavior still compiles.
