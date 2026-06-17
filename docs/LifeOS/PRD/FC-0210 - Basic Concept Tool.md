# FC-0210 - Basic Concept Tool

## Summary

The Basic Concept Tool is the default built-in tool for the Concept stage.

It helps the creator clarify and improve the design triangle for a single existing item:

```text
        Idea

Phrase        Graphic
```

The tool can be used manually as a visual aid, or with AI assistance when an AI provider and API key are configured. It should follow the same extensibility and context-awareness principles as the basic ideation tool, but with one important difference: the Concept tool always works inside an existing item.

There is no concept work outside an item. The item must already exist, and it must live somewhere in the navigation structure under a niche, topic, or subtopic.

## User Need

As a creator, I need a focused place to turn a raw idea, phrase, or graphic direction into a stronger product concept.

As an AI-assisted creator, I need FusionCanvas to improve one part of the concept at a time while using the current item, niche, topic path, existing idea, phrase, graphic direction, and creative history.

As a plugin author, I need the default Concept-stage experience to use the same stage tool host and context model as future advanced concept tools.

## Goals

- Provide the default Concept-stage tool for a single selected item.
- Make the design triangle visible and editable.
- Support manual concept refinement without requiring AI.
- Use optional AI assistance to improve the selected triangle element in context.
- Help the creator iteratively strengthen the weakest part of the concept.
- Preserve concept history so the creator can return to earlier directions.
- Save promising alternate AI suggestions as new items without leaving the tool.
- Keep the tool extensible through the Stage Tool Host.

## Core Principle

The Concept stage exists to establish three things:

- The idea: what the design is about and what makes it funny, noteworthy, emotional, or interesting.
- The phrase: the text used in the design, when the design has text.
- The graphic: the visual direction used in the design, when the design has a graphic.

Every design has an idea. A design may have no phrase, and a design may have no graphic, but it cannot have no idea.

The design triangle theory says the quality of a design depends on the relationship between idea, phrase, and graphic. A strong phrase cannot save a bad idea. A good idea can be weakened by a generic phrase. A clear phrase and idea can still fail if the graphic direction does not support them. The tool should help the creator improve the weak part in context of the stronger parts.

## Requirements

- The Basic Concept Tool is available from the Concept stage for an existing item.
- The tool is not available as a free-floating concept workspace without an item.
- If no item is selected, the Stage Tool Host should show an appropriate empty state or creation path instead of opening the tool.
- The selected item must have a valid navigation context, including store, niche, and topic path where applicable.
- The tool receives context from the Stage Tool Host instead of scraping UI state.
- The tool displays the current item context, including store, niche, topic path, item title, stage, inherited tags, and relevant metadata.
- The tool displays a triangle with idea at the top, phrase at the lower left, and graphic at the lower right.
- Each triangle node is editable.
- Each triangle node is clickable as the target for improvement.
- The idea node is required.
- The phrase node can be marked as not used for designs without text.
- The graphic node can be marked as not used for text-only designs.
- Manual edits are valid even when no AI provider is configured.
- Manual edits can be saved to the current concept version.
- If an AI provider is configured, the user can ask AI to improve the selected node.
- AI improvement uses all available context, including store, niche, topic path, selected item, existing triangle values, tags, metadata, prior concept versions, relevant accepted or rejected suggestions, and nearby sibling items.
- AI improvement affects only the selected node unless the user accepts a broader rewrite.
- AI suggestions do not overwrite saved concept data without approval.
- The user can accept, edit, reject, regenerate, or save an AI suggestion as an alternate direction.
- Accepted changes update the current concept version or create a new concept version according to the user's chosen action.
- Rejected suggestions are preserved as negative guidance where practical.
- The tool shows a quality indicator or score in the middle of the triangle when AI scoring is available.
- The score should indicate overall quality and, where useful, weak triangle elements.
- The score supports human judgment and does not automatically approve or reject a concept.
- A green check or similar ready indicator may appear when the concept is precise enough to proceed.
- The tool includes a history pane beside the triangle.
- The history pane receives a new entry when a triangle value is changed, an AI suggestion is requested, an AI suggestion is accepted or rejected, a score is updated, or the current concept version changes.
- The user can restore a previous history entry.
- The user can save a promising alternate suggestion as a new item under the current topic without leaving the Concept tool.
- Saved alternate items inherit the current topic context, tags, and relevant metadata.
- The tool can advance the item toward Design once the creator is satisfied with the concept.

## Basic Tool Layout

The default tool should use a focused Concept-stage layout:

- context summary near the top
- stage tool selector provided by the Stage Tool Host when multiple Concept tools are available
- main design triangle canvas
- editable idea, phrase, and graphic nodes
- center quality indicator
- action area for regenerate, accept, reject, save alternate, and proceed
- history pane to the right of the triangle

The triangle is the primary interaction model. The user should be able to understand that clicking a node means, "Improve this part using the other parts as context."

## Manual Workflow

The user opens an existing item in the Concept stage. The item already has:

```text
Idea: Funny pug drinking coffee
Phrase:
Graphic:
```

The user manually writes:

```text
Phrase: I am not human until my first coffee
Graphic: A cute, grumpy pug drinking coffee
```

The tool saves the concept triangle to the current item. The user can continue editing manually or proceed to Design.

## AI-Assisted Workflow

The user opens an existing item in the Concept stage with this rough idea:

```text
Funny pug drinking coffee
```

The tool places that text in the idea node.

The user clicks the phrase node. FusionCanvas asks AI to generate or improve the phrase using the item, niche, topic path, idea, tags, and relevant nearby work.

The first suggestion is:

```text
I'm adorable
```

The user rejects or regenerates it.

The next suggestion is:

```text
I'm not human until my first coffee
```

The user accepts it.

The user then clicks the graphic node. AI suggests:

```text
A cute, grumpy pug drinking coffee
```

The user accepts it.

The user clicks the idea node. AI refines the vague idea into:

```text
A cute, grumpy pug drinking coffee. The design plays on the absurdity of a dog needing coffee, the recognizable grumpiness of pugs, and the familiar joke of not feeling human before the first cup.
```

The triangle is now more complete, the score improves, and the user can move toward Design.

## Save as New Idea

Sometimes an AI suggestion is not right for the current item but is still useful.

The Concept tool should provide a one-click action to save a suggestion as a new idea or item. This action should:

- create a new item under the current topic by default
- copy the relevant suggested idea, phrase, and graphic values
- preserve source metadata showing it came from the current concept session
- inherit applicable tags and topic metadata
- leave the current item open in the Concept tool

This supports creative branching without forcing the creator to interrupt the current concept workflow.

## Concept History

The history pane is local to the current item and concept workflow.

History entries may include:

- manual idea edit
- manual phrase edit
- manual graphic edit
- AI suggestion requested
- AI suggestion accepted
- AI suggestion rejected
- score updated
- concept version created
- previous state restored
- suggestion saved as new item

Each entry should preserve enough data to understand what changed and restore a useful previous state. The history pane is not intended to be a full audit log.

## Concept Versions

The Basic Concept Tool works with concept versions.

Small edits may update the current concept version. Larger accepted alternatives may create a new version, depending on the command used. The current selected concept version should remain clear.

Superseded or rejected versions should remain available for later review without cluttering the active triangle.

## Scoring and Readiness

When AI scoring is available, the center of the triangle can show:

- overall concept score
- weak triangle element
- confidence or readiness state
- brief critique or improvement hint

Scoring should be interpreted as a creative assistant, not a gatekeeper. A creator can proceed even when the score is low, and a high score does not guarantee marketplace success.

## Extensibility

The Basic Concept Tool is the default implementation for the Concept stage.

Future plugins may provide alternative or companion concept tools, such as:

- phrase-first concept refinement
- graphic-first concept refinement
- brand-specific concept review
- marketplace-aware concept validation
- legal or trademark risk review
- multi-concept comparison
- bulk concept scoring
- advanced prompt-driven concept workshops

These tools should be selectable through the Stage Tool Host when they support the current item and stage context.

## Acceptance Criteria

- A user can open the Basic Concept Tool for an existing item in the Concept stage.
- The tool does not allow concept work without an existing item.
- The tool receives store, niche, topic path, item, stage, tags, metadata, and nearby work context from the Stage Tool Host.
- The user can manually edit idea, phrase, and graphic nodes.
- The idea node is required.
- The user can mark phrase or graphic as not used.
- The user can click a triangle node to request AI improvement for that node when AI is configured.
- AI suggestions use the other triangle values and the current navigation context.
- AI suggestions do not overwrite saved concept data without user approval.
- The user can accept, reject, regenerate, edit, or save an AI suggestion as a new item.
- The history pane records manual edits, AI actions, accepted suggestions, rejected suggestions, scoring changes, and restored states.
- The user can restore a previous history entry.
- The quality indicator can show concept readiness when AI scoring is available.
- The selected concept can be advanced toward Design when the creator is satisfied.
- The default concept tool can coexist with future plugin-provided Concept-stage tools.

## Out of Scope

- Final design image generation
- Detailed design production UI
- Marketplace listing metadata generation
- Sales prediction
- Guaranteed originality
- Full legal validation
- Full undo/redo across the entire application
- Multi-item batch concept refinement

## Related Notes

- [[Roadmap]]
- [[Product]]
- [[Data Model]]
- [[FC-0008 - Workflow Stage Navigator]]
- [[FC-0010 - Context-Aware Tools]]
- [[FC-0011 - Stage Tool Host]]
- [[FC-0201 - Idea Inbox]]
- [[FC-0202 - Concept Versions]]
- [[FC-0208 - Design Triangle View]]
- [[FC-0209 - Creative History Timeline]]
- [[FC-0401 - AI Provider Abstraction]]
- [[FC-0402 - AI Settings]]
- [[FC-0405 - Concept Refinement]]
- [[FC-0406 - Phrase Generation]]
- [[FC-0407 - Image Prompt Generation]]
- [[FC-0409 - Critique and Scoring]]
