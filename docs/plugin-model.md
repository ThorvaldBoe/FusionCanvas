# FusionCanvas Plugin Model

## Purpose

This document defines the intended plugin model for FusionCanvas.

The goal of the plugin system is to make FusionCanvas extensible without making the core application overly complex.

FusionCanvas should provide a stable foundation for common Print-on-Demand workflows while allowing specialized functionality to be added through plugins.

---

# Plugin Philosophy

FusionCanvas should have a focused core and an extensible edge.

The core application should provide:

* Workspace management
* Product pipeline management
* Design Triangle workflow
* Local storage
* Basic file handling
* Plugin loading and management

Specialized or platform-specific functionality should usually live in plugins.

This allows FusionCanvas to support many workflows without turning the core application into a large, fragile system.

---

# Why Plugins Matter

Print-on-Demand creators use different tools, platforms, and workflows.

Examples:

* Some creators sell on Etsy
* Some creators sell on Shopify
* Some use Printify
* Some use Printful
* Some generate mockups locally
* Some use cloud services
* Some manage listings manually
* Some rely heavily on AI tools

A plugin model allows FusionCanvas to support these differences without forcing every user into the same workflow.

---

# Core vs Plugin Responsibility

## Core Responsibilities

The FusionCanvas core should own the concepts that are universal to the product.

Core responsibilities include:

* Workspace structure
* Project organization
* Product workflow stages
* Design Triangle data
* Local storage
* Plugin discovery
* Plugin lifecycle management
* Shared domain models
* Shared user interface extension points
* Stage tool hosting

## Plugin Responsibilities

Plugins should own optional, specialized, or external behavior.

Plugin responsibilities may include:

* Marketplace integrations
* Print provider integrations
* Mockup generation methods
* Export formats
* AI providers
* Stage tools
* Reporting tools
* Automation workflows
* External file processing
* Metadata enrichment

---

# Initial Plugin Categories

## Marketplace Plugins

Marketplace plugins connect FusionCanvas to selling platforms.

Examples:

* Etsy
* Shopify
* Amazon Merch
* Redbubble

Possible capabilities:

* Prepare listing payloads
* Publish listings
* Update existing listings
* Import product data
* Retrieve marketplace status

---

## Print Provider Plugins

Print provider plugins connect FusionCanvas to fulfillment platforms.

Examples:

* Printify
* Printful
* Gelato

Possible capabilities:

* Retrieve products and variants
* Create draft products
* Upload artwork
* Sync product metadata
* Publish to connected stores

---

## Mockup Plugins

Mockup plugins generate product presentation images.

Possible capabilities:

* Apply designs to mockup templates
* Batch-generate mockups
* Export platform-specific image sets
* Support local rendering engines
* Support external mockup services

---

## Export Plugins

Export plugins produce files or packages for external use.

Possible capabilities:

* CSV export
* ZIP package export
* Marketplace import formats
* Backup formats
* Design handoff packages

---

## AI Provider Plugins

AI provider plugins connect FusionCanvas to AI services.

Possible capabilities:

* Idea generation
* Phrase suggestions
* Concept refinement
* Listing title suggestions
* Description suggestions
* Tag suggestions
* Product analysis

AI plugins should assist the creator, not replace creator judgment.

---

## Stage Tool Plugins

Stage tool plugins contribute interactive work surfaces for specific workflow stages.

Core workflow stages:

```text
Idea
Concept
Design
Listing
```

Archive is a related retained state, not a primary creative tool stage.

Possible capabilities:

* Alternative ideation tools
* Specialized concept refinement surfaces
* Design import or generation workspaces
* Listing preparation tools
* Validation and readiness panels

Stage tools should receive explicit context from the application, including active store, niche, topic path, selected item, active workflow stage, inherited tags and metadata, relevant nearby work, and available AI or plugin capabilities.

Stage tools should not infer context by scraping visible UI state.

Default built-in tools should use the same host and context model that plugin-provided tools use where practical.

---

## Workflow Automation Plugins

Workflow plugins automate repeatable processes.

Possible capabilities:

* Batch processing
* Scheduled checks
* File monitoring
* Product state transitions
* Notification workflows

---

## Reporting Plugins

Reporting plugins provide insight into product performance and workflow status.

Possible capabilities:

* Sales summaries
* Conversion analysis
* Pipeline bottleneck analysis
* Product lifecycle reports
* Marketplace performance comparison

---

# Plugin Lifecycle

A plugin may move through the following lifecycle:

```text id="6s9ndw"
Discovered
↓
Loaded
↓
Configured
↓
Enabled
↓
Running
↓
Disabled
↓
Unloaded
```

The core application should manage this lifecycle consistently.

---

# Plugin Discovery

FusionCanvas should support a simple local plugin discovery mechanism.

Initial discovery may be based on a plugins folder:

```text id="e9sf3g"
FusionCanvas/
└─ plugins/
   ├─ FusionCanvas.Plugin.Shopify/
   ├─ FusionCanvas.Plugin.Printify/
   └─ FusionCanvas.Plugin.Mockups.Local/
```

Future versions may support:

* Plugin manifests
* Plugin package installation
* Plugin repositories
* Plugin marketplace
* Signed plugins

---

# Plugin Manifest

Each plugin should provide metadata describing itself.

Example manifest fields:

```text id="dyay4h"
Name
Id
Version
Author
Description
Plugin Type
Required FusionCanvas Version
Permissions
Configuration Schema
```

The manifest should allow FusionCanvas to understand what the plugin does before loading executable code when possible.

---

# Plugin Capabilities

Plugins should declare the capabilities they provide.

Example capabilities:

```text id="7fm74o"
MarketplacePublishing
PrintProviderIntegration
MockupGeneration
AiProvider
StageTool
Export
WorkflowAutomation
Reporting
```

Capability declaration allows FusionCanvas to present plugin functionality in the correct parts of the user interface.

---

# Extension Points

FusionCanvas should expose defined extension points.

Possible extension points:

* Navigation items
* Detail panels
* Stage tools
* Pipeline actions
* Context menu actions
* Export commands
* Mockup generators
* Listing publishers
* AI providers
* Background tasks
* Reports

Extension points should be explicit and stable.

Plugins should not depend on internal implementation details.

---

# Permissions

Plugins may need access to sensitive data or external services.

Examples:

* Read workspace data
* Modify workspace data
* Access local files
* Connect to external APIs
* Store credentials
* Run background tasks

FusionCanvas should make plugin permissions visible to users.

Early versions may keep permissions simple, but the architecture should anticipate more explicit permission management later.

---

# Configuration

Plugins should be configurable through FusionCanvas.

Configuration may include:

* API keys
* Account settings
* Export paths
* Template folders
* Default behavior
* Feature toggles

Configuration should be stored securely where appropriate, especially for credentials.

---

# Plugin Isolation

The initial plugin model may use simple in-process plugins.

This is acceptable for early development.

However, the architecture should leave room for stronger isolation later.

Possible future models:

* Separate plugin processes
* Sandboxed execution
* Restricted permissions
* Signed plugins

The project should not over-engineer isolation before the plugin system is proven useful.

---

# Versioning

Plugins should declare compatibility with FusionCanvas versions.

The core application should avoid breaking plugin contracts without a clear versioning strategy.

Possible future versioning mechanisms:

* Semantic versioning
* API contract versions
* Capability versioning
* Migration helpers

---

# Error Handling

Plugins should not be able to crash the entire application during normal use.

FusionCanvas should handle plugin errors gracefully.

Examples:

* Failed plugin load
* Missing configuration
* Authentication failure
* External API failure
* Unsupported file format
* Background task failure

Errors should be visible and actionable for the user.

---

# Security Considerations

Plugins introduce risk.

FusionCanvas should eventually support:

* Clear permission visibility
* Secure credential storage
* Plugin source transparency
* Optional signature verification
* Plugin disablement
* Safe error handling

Users should understand that installing plugins from unknown sources can be dangerous.

---

# Open Source and Commercial Plugins

FusionCanvas core is open source.

The plugin model should allow:

* Open-source community plugins
* Private personal plugins
* Commercial third-party plugins
* Official FusionCanvas plugins

The plugin system should not require all plugins to use the same license as the core application.

---

# Initial Implementation Direction

The first version of the plugin system should be minimal.

Initial goals:

* Define plugin contracts
* Load local plugins
* Read plugin metadata
* Enable or disable plugins
* Expose simple extension points

The first implementation should avoid:

* Plugin marketplace
* Sandboxing
* Automatic updates
* Complex permission systems
* Remote plugin installation

These may be added later if needed.

---

# Plugin Model Success Criteria

The plugin model is successful if FusionCanvas can:

* Keep the core application focused
* Add platform-specific features without core changes
* Support multiple workflows
* Allow experimentation
* Enable community extensions
* Support future commercial extensions
* Avoid major rewrites as integrations grow

The plugin system should make FusionCanvas more flexible without making it harder to understand.
