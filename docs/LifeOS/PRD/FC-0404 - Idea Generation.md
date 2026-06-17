# FC-0404 - Idea Generation

## Summary

Idea Generation helps creators capture, generate, review, accept, reject, and convert early product ideas from the current navigation context.

For the first version, FusionCanvas should provide a basic default ideation tool. The tool should be intentionally lightweight: an idea can be a single line of text, with optional description and reference images. The creator should be able to create new listing items with one click from the selected topic context.

The default ideation tool should be implemented as a stage tool so more advanced ideation tools can later be provided by plugins and selected through the stage tool selector.

## User Need

As a Print on Demand creator, I need to capture and explore ideas quickly without being forced through a heavy item-management form for every thought.

As an AI-assisted creator, I need generated suggestions to use my current niche, topic, metadata, accepted ideas, and rejected ideas so the tool becomes more useful over time.

## Goals

- Keep idea capture fast enough for creative flow.
- Let creators work manually when no AI provider is configured.
- Use AI only as optional assistance.
- Generate ideas from the active store, niche, topic path, metadata, tags, and nearby work.
- Let users create listings from ideas in one click.
- Let created ideas open naturally in the Concept stage when the creator is ready to refine the design triangle.
- Let users reject poor suggestions so later AI calls can avoid similar directions.
- Avoid obvious duplicates by considering existing created ideas and listings.
- Allow broad ideation from broad topics and focused ideation from specific topics.
- Preserve extensibility through the Stage Tool Host and future plugin ideation tools.

## Requirements

- Users can open the default ideation tool from the Idea stage or from a selected topic context.
- Users can create a new idea/listing without opening the full ideation tool when they already know what they want to capture.
- Users can create a new idea/listing by right-clicking a topic or using a readily available New Idea command for the selected topic.
- The minimum required input for quick capture is one line of text.
- Users can optionally add a longer description.
- Users can optionally attach inspiration images.
- Inspiration images can be imported from files.
- Inspiration images can be pasted from the clipboard, including screenshots.
- The ideation tool can show the active context, including store, niche, topic path, current topic, tags, and relevant metadata.
- Creating an item from the tool places it under the selected topic by default.
- Created items receive unique ids automatically.
- Created items inherit applicable tags and metadata from the selected topic and parent context.
- Created items start in the Idea workflow stage unless the user intentionally chooses another stage.
- A created item can be advanced to Concept, where the Basic Concept Tool works on that existing item.
- Users can work manually without any configured AI provider.
- If an AI provider is configured, users can request a batch of generated ideas.
- Generated ideas are shown as simple candidate rows or cards.
- Each generated idea has a Create action.
- Each generated idea has a Reject action.
- Creating a generated idea creates a new listing/item in the selected navigation context.
- Rejecting a generated idea records that the direction was rejected for future ideation context.
- Generated ideas that are neither created nor rejected may be suggested again later.
- The AI request considers the selected context, existing created ideas/listings, and rejected ideas where practical.
- The AI request should avoid obvious duplicates of existing sibling items.
- Users should be able to understand what scope the tool used.
- AI suggestions should not overwrite human work without approval.

## Basic Tool Layout

The default ideation tool should be simple:

- context summary area
- single-line idea input
- optional description field
- optional inspiration image area
- Create action
- Generate Ideas action when AI is available
- generated idea list when AI output exists

The tool selector belongs to the Stage Tool Host rather than the ideation tool itself, but it should be visible near the top right of the tool area when multiple ideation tools are available.

## Manual Workflow

The user selects a topic such as:

```text
Store: Dog Shop
Niche: Dogs
Topic: Funny dogs
Subtopic: Pugs drinking coffee
```

The ideation tool shows that context. The user types:

```text
Pug guarding the coffee mug like a sacred artifact
```

The user clicks Create. FusionCanvas creates a new listing/item under `Pugs drinking coffee`, assigns a unique id, sets the stage to Idea, and applies inherited context from the selected topic.

## AI-Assisted Workflow

The user selects a broad topic such as `Funny dogs` and clicks Generate Ideas. The tool generates varied ideas that might become listings or subtopics.

The user creates the useful suggestions and rejects weak suggestions. Later requests consider the accepted and rejected history so the model avoids duplicates and moves away from rejected directions.

The user can also select a narrow topic such as `Pugs drinking coffee`. Generated suggestions should become more specific because the selected topic and inherited metadata narrow the context.

## Accepted and Rejected Idea Context

Accepted or created ideas should guide future generation away from duplicates and toward useful patterns.

Rejected ideas should be preserved as negative guidance. Rejection does not mean the idea must become a normal listing or clutter the active navigation tree. It means the ideation context can learn that this direction was not wanted.

For the first version, rejected AI suggestions may be stored as prompt metadata, ideation-session metadata, plugin data, or a lightweight idea record depending on the implementation path chosen by the data model. The important behavior is that rejection is available as context for later generation without creating red tape for the user.

## Data Handling

An idea created from the ideation tool should become a normal item/listing as soon as the user clicks Create.

The created item should include:

- unique id
- parent store
- parent niche
- parent topic or group
- working title or idea text
- optional description
- Idea workflow stage
- initial status of Draft
- inherited tags where applicable
- inherited metadata where applicable
- attached inspiration image assets where provided
- source metadata indicating manual capture or AI suggestion where useful

Inspiration images are references for the current idea, not final design assets. They should be stored as assets with a reference or inspiration purpose.

## Concept Handoff

The ideation tool creates or selects items. The Concept tool refines an existing item.

An idea may be a vague idea description, a concrete phrase, a graphic direction, or a mix of these. When the item moves into Concept, the Basic Concept Tool should map the available information into the design triangle where practical:

- vague design description to idea
- phrase-like text to phrase, with user correction available
- visual description to graphic direction
- mixed input to the best initial triangle fields, without hiding uncertainty

The creator can then fill or improve the missing triangle elements manually or with AI assistance.

## Extensibility

The basic ideation tool is the default implementation for the Idea stage. Future plugins may provide alternative ideation tools such as:

- structured niche expansion
- phrase-first ideation
- image-reference ideation
- trend-informed ideation
- scoring-based idea review
- bulk ideation sessions
- marketplace-aware validation

These tools should be selectable through the Stage Tool Host when they support the current context.

## Acceptance Criteria

- A user can quickly create a new idea/listing from a selected topic with only one line of text.
- A user can optionally add description and inspiration images during idea capture.
- A user can paste a screenshot as an inspiration image.
- A user can work in the ideation tool without configuring AI.
- A user with configured AI access can generate a batch of candidate ideas from the selected context.
- Generated ideas reflect the current store, niche, topic path, topic metadata, and nearby work.
- Generated ideas avoid obvious duplication of existing sibling items.
- A user can create a generated idea in one click without leaving the ideation tool.
- A user can reject a generated idea so future generations can use it as negative context.
- Created ideas are placed in the selected navigation context automatically.
- Created ideas inherit applicable tags and metadata from parent topics.
- The default ideation tool can coexist with future plugin-provided ideation tools.

## Out of Scope

- Fully automated product creation
- Trend scraping
- Marketplace validation
- Advanced originality guarantees
- Mandatory scoring before idea creation
- Complex forms for every generated idea
- Final design image generation

## Related Notes

- [[Roadmap]]
- [[FC-0201 - Idea Inbox]]
- [[FC-0403 - Niche AI Context]]
- [[FC-0010 - Context-Aware Tools]]
- [[FC-0011 - Stage Tool Host]]
- [[FC-0104 - Listing Management]]
- [[FC-0109 - Import Existing Assets]]
- [[FC-0210 - Basic Concept Tool]]
- [[FC-0401 - AI Provider Abstraction]]
- [[FC-0402 - AI Settings]]
