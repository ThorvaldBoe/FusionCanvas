## ADDED Requirements

### Requirement: Tools Can Determine The Active Store
FusionCanvas SHALL ensure that tools can determine the active store.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Tools can determine the active store

### Requirement: Tools Can Determine The Active Niche
FusionCanvas SHALL ensure that tools can determine the active niche.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Tools can determine the active niche

### Requirement: Tools Can Determine The Current Topic And Full Parent Topic Path
FusionCanvas SHALL ensure that tools can determine the current topic and full parent topic path.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Tools can determine the current topic and full parent topic path

### Requirement: Tools Can Determine The Active Item And Workflow Stage Where Applicable
FusionCanvas SHALL ensure that tools can determine the active item and workflow stage where applicable.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Tools can determine the active item and workflow stage where applicable

### Requirement: Tools Can Access Relevant Metadata From The Store, Niche, Topic, And Item
FusionCanvas SHALL ensure that tools can access relevant metadata from the store, niche, topic, and item.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Tools can access relevant metadata from the store, niche, topic, and item

### Requirement: Tools Can Access Inherited Tags And Metadata That Apply To Newly Created Work
FusionCanvas SHALL ensure that tools can access inherited tags and metadata that apply to newly created work.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Tools can access inherited tags and metadata that apply to newly created work

### Requirement: Tools Can Inspect Existing Items In The Current Topic To Avoid Obvious Duplicates
FusionCanvas SHALL ensure that tools can inspect existing items in the current topic to avoid obvious duplicates.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Tools can inspect existing items in the current topic to avoid obvious duplicates

### Requirement: Tools Can Inspect Accepted, Created, Rejected, Or Archived Nearby Work When That Context Helps The Current Workflow
FusionCanvas SHALL ensure that tools can inspect accepted, created, rejected, or archived nearby work when that context helps the current workflow.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Tools can inspect accepted, created, rejected, or archived nearby work when that context helps the current workflow

### Requirement: Creating A New Item From A Topic Places The Item In That Topic By Default
FusionCanvas SHALL ensure that creating a new item from a topic places the item in that topic by default.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Creating a new item from a topic places the item in that topic by default

### Requirement: Creating A New Item From A Topic Can Automatically Apply Inherited Tags And Metadata From The Selected Topic And Parent Context
FusionCanvas SHALL ensure that creating a new item from a topic can automatically apply inherited tags and metadata from the selected topic and parent context.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Creating a new item from a topic can automatically apply inherited tags and metadata from the selected topic and parent context

### Requirement: Item-bound Tools Can Require A Selected Item And Should Receive That Item's Full Parent Context
FusionCanvas SHALL ensure that item-bound tools can require a selected item and should receive that item's full parent context.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Item-bound tools can require a selected item and should receive that item's full parent context

### Requirement: Item-bound Tools Can Still Create Related New Items When The User Explicitly Branches Or Saves An Alternate Idea
FusionCanvas SHALL ensure that item-bound tools can still create related new items when the user explicitly branches or saves an alternate idea.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Item-bound tools can still create related new items when the user explicitly branches or saves an alternate idea

### Requirement: Generative Tools Use The Current Context Automatically Unless The User Changes Scope
FusionCanvas SHALL ensure that generative tools use the current context automatically unless the user changes scope.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Generative tools use the current context automatically unless the user changes scope

### Requirement: Users Should Be Able To See Or Understand The Scope A Tool Is Using
FusionCanvas SHALL allow users to be able to see or understand the scope a tool is using.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** the user should be able to see or understand the scope a tool is using

### Requirement: Users Can Intentionally Change Scope When Needed
FusionCanvas SHALL allow users to intentionally change scope when needed.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** the user can intentionally change scope when needed

### Requirement: Context-aware Behavior Applies Across Idea, Concept, Design, Listing, Asset, And Automation Workflows
FusionCanvas SHALL ensure that context-aware behavior applies across idea, concept, design, listing, asset, and automation workflows.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Context-aware behavior applies across idea, concept, design, listing, asset, and automation workflows

### Requirement: Acceptance Criteria Remain Satisfied
FusionCanvas SHALL satisfy the acceptance criteria captured in the source PRD.

#### Scenario: Creating New Work From A Selected Topic Places It Under That Topic By Default
- **WHEN** the corresponding capability is delivered
- **THEN** Creating new work from a selected topic places it under that topic by default.

#### Scenario: Context-aware Tools Know The Active Store, Niche, Topic Path, And Item Where Applicable
- **WHEN** the corresponding capability is delivered
- **THEN** Context-aware tools know the active store, niche, topic path, and item where applicable.

#### Scenario: New Work Can Inherit Applicable Tags And Metadata From The Active Topic Context
- **WHEN** the corresponding capability is delivered
- **THEN** New work can inherit applicable tags and metadata from the active topic context.

#### Scenario: Generative Tools Can Use Existing Items In The Current Topic To Reduce Redundancy
- **WHEN** the corresponding capability is delivered
- **THEN** Generative tools can use existing items in the current topic to reduce redundancy.

#### Scenario: Generative Tools Can Use Rejected Suggestions As Negative Guidance Where Relevant
- **WHEN** the corresponding capability is delivered
- **THEN** Generative tools can use rejected suggestions as negative guidance where relevant.

#### Scenario: Users Can Understand What Scope A Tool Is Using
- **WHEN** the corresponding capability is delivered
- **THEN** Users can understand what scope a tool is using.

#### Scenario: Users Can Skip Earlier Workflow Stages When They Already Have Enough Information
- **WHEN** the corresponding capability is delivered
- **THEN** Users can skip earlier workflow stages when they already have enough information.

#### Scenario: Item-bound Tools Can Require An Item And Communicate Clearly When Only A Topic Is Selected
- **WHEN** the corresponding capability is delivered
- **THEN** Item-bound tools can require an item and communicate clearly when only a topic is selected.

#### Scenario: Context-aware Behavior Is Treated As A Cross-cutting Product Principle, Not Only An AI Feature
- **WHEN** the corresponding capability is delivered
- **THEN** Context-aware behavior is treated as a cross-cutting product principle, not only an AI feature.

## Source PRD

# FC-0010 - Context-Aware Tools

## Summary

Context-Aware Tools use the current store, niche, topic path, item, stage, metadata, and nearby work to make actions more relevant.

FusionCanvas should avoid generic creative output when the user is working inside a specific context. Tools should inherit that context automatically, so creating or generating work from a topic places the result in the right part of the workspace and uses the right creative constraints.

## User Need

As a creator, I need FusionCanvas tools to understand where I am in the workspace so new ideas, concepts, designs, and listings are targeted instead of random.

## Goals

- Make the current navigation context meaningful to all creation tools.
- Automatically place newly created work in the current topic when appropriate.
- Use parent topics, niche context, store context, tags, and metadata to guide creative tools.
- Avoid redundant idea generation within the current topic.
- Support skipping directly to Concept or Design when the creator already has enough information.
- Distinguish topic-based tools that create items from item-bound tools that refine an existing item.

## Requirements

- Tools can determine the active store.
- Tools can determine the active niche.
- Tools can determine the current topic and full parent topic path.
- Tools can determine the active item and workflow stage where applicable.
- Tools can access relevant metadata from the store, niche, topic, and item.
- Tools can access inherited tags and metadata that apply to newly created work.
- Tools can inspect existing items in the current topic to avoid obvious duplicates.
- Tools can inspect accepted, created, rejected, or archived nearby work when that context helps the current workflow.
- Creating a new item from a topic places the item in that topic by default.
- Creating a new item from a topic can automatically apply inherited tags and metadata from the selected topic and parent context.
- Item-bound tools can require a selected item and should receive that item's full parent context.
- Item-bound tools can still create related new items when the user explicitly branches or saves an alternate idea.
- Generative tools use the current context automatically unless the user changes scope.
- Users should be able to see or understand the scope a tool is using.
- Users can intentionally change scope when needed.
- Context-aware behavior applies across idea, concept, design, listing, asset, and automation workflows.

## Example

If the user is working inside:

```text
Store: Dog Shop
Niche: Dogs
Topic: Dogs and coffee
Subtopic: Funny dogs and coffee
```

then an idea generation tool should know:

- the store context
- the Dogs niche
- the full topic path
- the current topic: Funny dogs and coffee
- existing ideas in that topic
- relevant topic or niche metadata such as humor style, tone, visual style, and constraints

The tool should generate ideas for that current context, not generic dog ideas.

## User Workflows

### Create in Place

The user selects a topic and creates a new idea. The idea is automatically placed inside that topic.

### Generate Targeted Ideas

The user selects Funny dogs and coffee and asks for suggestions. The tool uses the current topic, parent topics, niche metadata, and existing sibling items to generate relevant non-duplicative ideas.

### Skip Ahead

The user already has a phrase and graphic direction, so they create an item directly in Concept or Design stage with minimal friction.

### Refine an Existing Item

The user opens an existing item in the Concept stage. The concept tool uses the item, selected concept version, topic path, niche context, tags, metadata, and nearby sibling work to improve the idea, phrase, or graphic direction without requiring the user to restate the context.

### Reuse Context in Design

The user asks an image generation tool to create a design. The tool uses the phrase, graphic direction, topic path, niche style, and store context automatically.

### Import or Select Design Variants

The user opens an existing item in the Design stage and imports artwork created manually or in an external AI tool. The design tool uses the current item, selected concept, topic path, tags, and metadata to attach the imported file to the right design record and classify it for later listing or product-variant work.

### Generate Design Variants

The user opens an existing item in the Design stage and asks FusionCanvas to generate artwork. The design tool uses the selected concept, idea, phrase, graphic direction, niche context, style guidance, constraints, and prior accepted or rejected work to create draft variants without making them final until the user promotes one.

## Acceptance Criteria

- Creating new work from a selected topic places it under that topic by default.
- Context-aware tools know the active store, niche, topic path, and item where applicable.
- New work can inherit applicable tags and metadata from the active topic context.
- Generative tools can use existing items in the current topic to reduce redundancy.
- Generative tools can use rejected suggestions as negative guidance where relevant.
- Users can understand what scope a tool is using.
- Users can skip earlier workflow stages when they already have enough information.
- Item-bound tools can require an item and communicate clearly when only a topic is selected.
- Context-aware behavior is treated as a cross-cutting product principle, not only an AI feature.

## Out of Scope

- Detailed ideation algorithm
- Specific AI provider behavior
- Fully automatic creative decisions
- Guaranteed originality
- Semantic duplicate detection unless separately specified

## Open Questions

- How should the UI expose the current tool scope without adding clutter?
- Should tools default to current topic only, current subtree, or full niche?
- How much existing sibling-item context should be sent to AI tools?

## Related Notes

- [[Phase 0 - Foundation]]
- [[FC-0005 - Navigation Tree]]
- [[FC-0107 - Basic Search and Filtering]]
- [[FC-0201 - Idea Inbox]]
- [[FC-0210 - Basic Concept Tool]]
- [[FC-0211 - Basic Design Tool]]
- [[FC-0404 - Idea Generation]]
- [[FC-0407 - Image Prompt Generation]]
- [[FC-0011 - Stage Tool Host]]
