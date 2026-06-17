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
