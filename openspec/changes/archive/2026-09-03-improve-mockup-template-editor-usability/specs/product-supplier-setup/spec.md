## MODIFIED Requirements

### Requirement: Mockup Template dialog opens safely from Store Editor
The Store Editor SHALL open the Mockup Template dialog with a valid owner when the parent is attached and visible, and SHALL avoid an owner-dependent modal call when it is not.

#### Scenario: Store editor is not yet attached
- **WHEN** a Mockup Template edit request arrives while Store Editor has no valid native owner
- **THEN** the request is deferred or opened modelessly without throwing an owner exception
