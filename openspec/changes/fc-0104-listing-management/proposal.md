## Why

FusionCanvas has the domain concept and navigation shape for listings, but creators still need a durable workflow for capturing and maintaining product ideas before artwork or marketplace data exists. FC-0104 makes listings usable as the primary unit of daily creative work and supplies the Phase 1 foundation required by lifecycle, inspector, search, tagging, and asset features.

## What Changes

- Add fast listing capture in the primary workspace from a selected active store, niche, or group, requiring only one line of idea text and inheriting applicable parent context.
- Add focused listing management for renaming, editing core creative details, moving, duplicating, archiving, restoring, and permanently deleting listings.
- Preserve listing identity and all related context when a listing moves; create a distinct identity when it is duplicated.
- Allow listings to exist without assets, mockups, optimized marketplace metadata, or other later-stage records.
- Keep common capture and selection actions close to the navigation context while progressively disclosing occasional and destructive management actions.
- Define complete empty, validation, blocked, success, cancellation, focus, and destructive-action behavior for listing workflows.
- Resolve Phase 1 safety defaults: permanent deletion requires explicit confirmation, duplicated listings exclude asset relationships by default, and converting a listing into a group is out of scope.

## Capabilities

### New Capabilities

- `listing-management`: Defines creation, core creative-detail editing, movement, duplication, archival, restoration, deletion, persistence, and user-facing interaction behavior for listings.

### Modified Capabilities

- `navigation-tree`: Extend listing placement and movement so listings may appear directly under a store as well as under niche or group topics, while retaining the existing hierarchy and context-preservation rules.

## Impact

- Adds listing-management application contracts and orchestration alongside existing store and niche management services.
- Extends workspace persistence with listing queries and mutations while preserving workspace/store/topic ownership constraints.
- Adds listing capture and management presentation in the Avalonia desktop app, integrated with active store and navigation context.
- Extends navigation destination handling for store-level listings while preserving existing niche and group behavior.
- Adds domain, application, integration, and app tests for listing lifecycle operations, validation, inherited context, persistence, selection, drafts, and confirmations.
- Depends on the accepted workspace, store, niche, core-domain, persistence, and navigation contracts; group destinations become available when FC-0103 group management is present.
