## 1. Domain Model Structure

- [ ] 1.1 Review existing `FusionCanvas.Domain` namespaces and choose a focused location for core entity types.
- [ ] 1.2 Add shared identifier/value validation helpers only where they directly simplify the core model.
- [ ] 1.3 Implement Store as the top-level business or brand context without persistence or marketplace dependencies.

## 2. Topic and Item Concepts

- [ ] 2.1 Implement Niche as a top-level topic concept scoped to a Store.
- [ ] 2.2 Implement Group as a nested topic concept that can belong under a Niche or another Group.
- [ ] 2.3 Implement Listing as the initial item-like product concept scoped to a Store and optional topic context.
- [ ] 2.4 Add domain APIs or type markers that make topic and item classification explicit to contributors.

## 3. Context Entities

- [ ] 3.1 Implement Asset as a persistence-neutral connected resource that can relate to store-level or listing-level creative work.
- [ ] 3.2 Implement Prompt as preserved prompt-related context without AI provider execution behavior.
- [ ] 3.3 Implement Tag as a reusable classification label that can classify multiple core entity types.

## 4. Relationship and Boundary Tests

- [ ] 4.1 Add `FusionCanvas.Domain.Tests` coverage proving the seven core entities are represented and named.
- [ ] 4.2 Add tests for Store ownership and absence of required parent store or marketplace account behavior.
- [ ] 4.3 Add tests for Niche and Group as topics, including nested group support without fixed depth.
- [ ] 4.4 Add tests for Listing as the initial item concept and not a marketplace product.
- [ ] 4.5 Add tests for Asset, Prompt, and Tag relationships while excluding workspace file storage, AI execution, and marketplace keyword behavior.
- [ ] 4.6 Add tests or assertions that advanced entities from later phases are not introduced by this implementation.

## 5. Validation

- [ ] 5.1 Run `dotnet test` for the solution or the affected domain test project.
- [ ] 5.2 Run `openspec status --change "fc-0002-core-domain-model"` and verify the change is apply-ready.
- [ ] 5.3 Update task checkboxes to reflect completed implementation work before archive.
