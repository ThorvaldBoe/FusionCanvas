# PoD Design Framework

This area contains the canonical theory for creating print-on-demand designs that function as wearable statements.

The framework assumes a typical business using Shopify as the storefront and Printify as the production platform. Its principles are platform-independent, but they account for product thumbnails, garment printing, distance readability, and the social setting in which a design is worn.

## Canonical pages

1. [[01 - Foundations of PoD Design]] — why PoD design is a social communication system
2. [[02 - The Design Triangle and Design Pyramid]] — how a concept becomes a realizable design
3. [[03 - Sketch Layout Language]] — the unified SLL artifact and notation
4. [[04 - Generating SLL]] — the method and contract for producing SLL from a Design Triangle

Together, these pages define the theoretical basis for a FusionCanvas feature that accepts an idea, phrase, and graphic description and returns a complete ASCII design sketch with execution notes.

## Status of the Sources folder

The documents in `Sources/` are preserved as historical reference. They contain useful ideas from earlier iterations, but they are not canonical and may contradict one another.

When a source document conflicts with one of the canonical pages above, the canonical page takes precedence.

## Core vocabulary

- **Design Triangle:** the idea, phrase, and graphic that form the concept
- **Design Pyramid:** the Design Triangle realized through concrete design decisions
- **SLL:** Sketch Layout Language; the first complete, inspectable representation of the Design Pyramid
- **Wearer signal:** what wearing the design communicates about the wearer
- **Viewer inference:** what another person is expected to conclude or feel



---

# Foundations of PoD Design

## 1. PoD design is wearable communication

A print-on-demand design is not merely an image printed on a product. It is a message performed in public.

The design sits between two primary actors:

- the **wearer**, who chooses to display it
- the **viewer**, who sees it and interprets what that choice means

A third role, the **buyer**, makes the purchase. The buyer and wearer may be the same person or different people. In either case, the buyer is selecting a future social signal.

This makes PoD different from illustration. A beautiful image can succeed as art while failing as a shirt design. A PoD design succeeds when wearing it says something worth displaying and seeing it produces the intended recognition, emotion, or response.

## 2. The social meaning model

Every viable design should be explainable as a short communication loop:

```text
wearer chooses the design
        |
        v
design expresses a signal
        |
        v
viewer forms an inference
        |
        v
recognition, emotion, or interaction occurs
```

The signal may communicate:

- **identity:** “This is the kind of person I am.”
- **belonging:** “I am one of the people who understands this.”
- **value:** “This matters to me.”
- **experience:** “I have lived this too.”
- **attitude:** “This is how I respond to the world.”
- **invitation:** “Recognize this and connect with me.”
- **defiance:** “I reject the expected position.”

Humor is an emotional mechanism, not a sufficient message by itself. A generic joke often communicates only “I am trying to be funny.” A strong humorous design exposes an identity, habit, tension, weakness, opinion, or shared experience. The viewer does not merely understand the joke; the viewer understands why this wearer chose it.

## 3. The statement principle

Every PoD design should make a statement, even when the statement is subtle or image-led.

A useful statement has three layers:

1. **Literal content:** what the words and image directly show
2. **Wearer signal:** what choosing to wear it says about the wearer
3. **Viewer effect:** what the intended viewer recognizes, feels, or does

Example:

```text
Literal content: A phrase about negotiating with a dragon
Wearer signal: I overthink fantasy danger and identify with the social problem-solver
Viewer effect: A D&D player recognizes the behavior and laughs at the self-exposure
```

If only the literal content can be described, the concept is probably decorative, generic, or underdeveloped.

## 4. Two contexts of use

A design must work in two related environments.

### Commercial context

On an ad, Shopify collection page, or product page, the design must:

- attract attention without becoming cluttered
- reveal its main visual idea at thumbnail size
- make the relevant audience recognize itself quickly
- remain understandable on the intended garment color

### Social context

When worn, the design must:

- read at an appropriate distance
- create the intended inference about the wearer
- reward closer inspection only when subtlety is deliberate
- avoid requiring a verbal explanation

Commercial visibility gets the design considered. Social meaning makes it worth buying and wearing.

## 5. The communication priorities

The framework uses this dependency order:

1. **Signal:** Say something meaningful about the wearer, audience, or shared world.
2. **Recognition:** Give the intended audience enough context to know the design is for them.
3. **Comprehension:** Make the central message understandable at the intended viewing distance.
4. **Emotion:** Produce humor, pride, affection, nostalgia, defiance, solidarity, or another useful response.
5. **Attraction:** Make the execution strong enough to earn attention and desire.
6. **Production fitness:** Preserve the design through printing, garment variation, and storefront presentation.

These priorities interact, but later qualities cannot reliably rescue an empty signal.

## 6. Core quality principles

### Meaning before decoration

Style amplifies meaning. It does not create meaning that is absent from the concept.

### Recognition before explanation

Use language, symbols, situations, and rituals the intended audience already knows. The design should activate shared knowledge rather than teach background information.

### Specificity creates identity

Broad statements may reach more people but often give fewer people a reason to wear them. Specific habits, roles, tensions, and references make self-selection possible.

### Each element must have a job

Text, graphics, texture, and ornament should communicate, organize, emphasize, or create the intended tone. Elements with no job are noise.

### Hierarchy is meaning

What is seen first changes how the message is interpreted. Visual hierarchy is therefore a semantic decision, not merely an aesthetic one.

### Simplicity is compression

A simple design is not an underdeveloped design. It is a design in which the important meaning survives with little visual friction.

### The product is part of the design

Placement, print area, garment color, fabric, and viewing distance affect the message. A design is not complete in isolation from its intended product.

## 7. Foundation test

Before developing a concept, answer:

- Who is expected to wear this?
- What does wearing it say about them?
- Who is expected to notice it?
- What should that viewer infer or feel?
- What shared knowledge makes the message work?
- Why is this worth wearing rather than merely viewing?

If these answers are vague, improve the concept before investing in execution.



---

# The Design Triangle and Design Pyramid

## 1. Two levels of commitment

The framework separates **what the design means** from **how the design will exist**.

- The **Design Triangle** defines a complete concept.
- The **Design Pyramid** turns that concept into a concrete design system.

This separation prevents layout, fonts, effects, or fashionable styles from hiding a weak idea. It also prevents a good concept from remaining too abstract to produce.

## 2. The Design Triangle

The Design Triangle contains three mutually supporting corners:

```text
                    IDEA
        intended social meaning and effect
                   /    \
                  /      \
                 /        \
            PHRASE ------ GRAPHIC
         verbal carrier   visual carrier
```

### Idea

The Idea is the social proposition behind the design. It combines:

- the subject or situation
- the wearer signal
- the intended viewer inference
- the intended emotional effect

The idea should be expressible in one sentence. “A funny dragon shirt” is a topic, not an idea. “A cautious player admits they would negotiate with a dragon, signaling familiar D&D overthinking” is an idea.

### Phrase

The Phrase is the complete verbal carrier of the idea. At Triangle stage it is recorded as one semantic statement, before line breaks, emphasis, or typography.

A good phrase:

- sounds natural in the audience’s language
- contains a clear identity, experience, value, tension, or point of view
- works without spoken delivery
- is concise enough to become visual material
- gives the viewer a useful inference about the wearer

The phrase can be internally present but intentionally omitted from the final artwork in an image-only design. Omission must be a design decision, not a missing corner.

### Graphic

The Graphic is the visual carrier of the idea. It is described conceptually at Triangle stage, not drawn or positioned.

A good graphic:

- creates recognition or carries meaning
- is appropriate to the audience’s visual vocabulary
- remains distinguishable at the intended scale
- reinforces, completes, or productively contrasts the phrase
- can be simplified for reliable garment printing

The Graphic may be one illustration, a symbol, several icons, a sequence, a pattern, or an intentional absence in a text-only execution. As with the phrase, absence in execution must be explicit.

## 3. Relationships inside the Triangle

Phrase and Graphic can relate in three useful ways:

- **Reinforcement:** both make the same message faster or more recognizable
- **Completion:** each supplies information the other lacks
- **Contrast:** the difference between them creates irony, surprise, or commentary

Simple reinforcement is appropriate when recognition and distance readability are priorities. Completion and contrast can create a stronger payoff but demand more from composition.

Accidental duplication is not useful. If the phrase says “dragon” and the graphic merely shows a dragon, ask what the graphic adds: threat, personality, role, action, tone, or world context.

## 4. Completing and testing the Triangle

A concept may begin with any corner. Develop the missing corners by asking:

### From an idea

- What would a member of the audience naturally say?
- What symbol, object, character, or scene triggers recognition?

### From a phrase

- What situation and identity make someone choose these words?
- What should a viewer conclude about the wearer?
- What graphic shortens, deepens, or reframes the message?

### From a graphic

- What does choosing this image say about the wearer?
- What phrase gives it a point of view rather than a caption?

The Triangle is ready when:

- the Idea names a wearer signal and viewer effect
- the Phrase expresses rather than explains the Idea
- the Graphic has a semantic role rather than a decorative role
- Phrase and Graphic have an intentional relationship
- the intended audience can recognize the shared context
- there is enough meaning to justify wearing the result

## 5. Phrase viability

Before execution, reject or repair phrases that depend on:

- a long explanation before the point arrives
- vocal tone or timing
- a weak punchline hidden by a strong setup
- generic sentiment with no audience-specific signal
- graphics that merely illustrate nouns

Useful repairs include compressing the setup, strengthening the identity claim or consequence, moving humor into contrast or self-exposure, and choosing a graphic that participates in the meaning.

There is no universal rule that a payload must always be largest or that every phrase needs a punchline. Hierarchy follows the specific statement. An identity label, warning, confession, or emblem may organize meaning differently.

## 6. The Design Pyramid

The Pyramid adds realization to the Triangle:

```text
                         REALIZATION
                  a concrete design solution
                         /       \
                        /         \
                       /           \
                    IDEA -------- PHRASE
                       \           /
                        \         /
                         GRAPHIC
```

The apex is not a fourth message component. It is the coordinated set of choices that makes the three corners visible on a product.

Realization includes:

- **composition:** the overall arrangement and silhouette
- **hierarchy:** what is noticed first, second, and last
- **placement:** where the design sits on the product and within the print area
- **scale and spacing:** size, density, rhythm, and negative space
- **typography:** font categories, casing, line breaks, and typographic behavior
- **color:** palette, contrast, garment interaction, and ink economy
- **graphic treatment:** illustration style, simplification, line weight, and detail
- **surface treatment:** distress, grunge, texture, outlines, shadows, or other effects
- **production constraints:** print method, safe margins, minimum detail, and product variants

## 7. Composition grammars

Composition should follow how the message is read. Six broad grammars cover most PoD designs:

1. **Statement stack:** a direct phrase with clear typographic hierarchy
2. **Narrative stack:** meaning unfolds in a controlled reading order
3. **Emblem:** identity or authority is presented as a unified badge, seal, or mark
4. **Image hero:** a graphic carries the first impression and text supports it
5. **Integrated interaction:** text and graphic physically combine to create meaning
6. **Collection or sequence:** repetition, grouping, or progression carries meaning

These are reasoning categories, not mandatory templates. Hybrid designs are valid, but one grammar should normally control the composition.

## 8. The viable execution region

Many realizations can express the same Triangle. The objective is not to find a mathematically perfect layout, but to stay inside a viable region where:

- the intended statement remains intact
- the reading order matches the meaning
- phrase and graphic do not compete accidentally
- the design works at thumbnail and worn size
- complexity is justified by communicative value
- the artwork remains practical to print

Moving outside this region produces predictable failures: clutter, weak hierarchy, unreadable detail, decorative graphics, generic styling, or a layout that changes the intended joke or identity signal.

## 9. From Pyramid to SLL

The first complete representation of the Design Pyramid is an **SLL sketch**. SLL forces the realization decisions to become inspectable before detailed artwork is created.

The workflow is:

```text
social purpose
    -> Design Triangle
    -> semantic and visual reasoning
    -> SLL sketch
    -> artwork and product variants
    -> storefront and market learning
```

Iteration is allowed. If SLL exposes a weak phrase, redundant graphic, or confused signal, return to the Triangle rather than using execution to conceal the problem.



---

# Sketch Layout Language

## 1. Definition

**SLL** means **Sketch Layout Language**.

An SLL is a compact, human-readable specification of a complete PoD design. Its central feature is an ASCII sketch that shows the approximate composition and reading order. Side notes define visual and production properties that ASCII cannot express reliably.

An SLL is:

- the first visible form of the Design Pyramid
- detailed enough to guide artwork creation
- simple enough to inspect and revise quickly
- structured enough for FusionCanvas to generate and store

An SLL is not final art, a pixel-perfect mockup, or a promise that all proportions are exact.

## 2. One concept, not SLL plus SLL-V

Earlier source documents separated a symbolic SLL from a visual SLL-V projection. The canonical framework retires that distinction.

The user-facing output is one SLL containing:

1. the design intent
2. one ASCII composition sketch
3. execution notes
4. validation notes

Semantic segmentation, weights, element relations, and candidate layouts may still exist as an internal **layout model**. FusionCanvas may persist that model for reproducibility, but users do not need to author, read, or request it.

“Create SLL for this Design Triangle” therefore means “return the complete visual sketch and its design notes.”

## 3. Required SLL structure

Every SLL uses this order.

### Identity

- title or working name
- SLL version
- intended product and placement

### Communication intent

- wearer signal
- viewer inference
- intended emotion
- shared context required for recognition

### Design Triangle

- idea
- exact phrase
- graphic description
- phrase/graphic relationship: reinforcement, completion, or contrast

### ASCII sketch

A bounded sketch of the print area showing:

- all printed words with proposed line breaks and relative emphasis
- the graphic’s approximate form, position, and size
- grouping, overlap, curvature, framing, and negative space when relevant
- the intended top-to-bottom or left-to-right reading order

### Execution notes

- composition grammar and visual lead
- typography
- graphic style
- colors and garment interaction
- texture or effects
- placement and scale
- production cautions

### Validation

- what reads first, second, and third
- thumbnail and distance behavior
- how the design expresses the wearer signal
- the largest unresolved risk, or “none”

## 4. ASCII conventions

SLL sketches use plain ASCII so they remain portable and machine-safe.

```text
+------------------------------+  approximate print boundary
| ACTUAL PRINTED WORDS         |  words without brackets are printed
|                              |
| [GRAPHIC: short description] |  bracketed text is an art instruction
|                              |
+------------------------------+
```

Conventions:

- The outer box represents the artwork or print boundary; it is not printed.
- Unbracketed words are literal printed text.
- `[GRAPHIC: ...]` describes imagery when representational ASCII would be unclear.
- A recognizable ASCII silhouette may replace or supplement a graphic label.
- Parenthetical or bracketed labels are instructions, not printed copy.
- Capitalization approximates emphasis but typography notes are authoritative.
- Blank lines represent meaningful negative space.
- Repeated characters may indicate a border, banner, rays, texture, or motion only when labelled in the notes.
- Curved text may be approximated by staggered lines and must be confirmed in the notes.
- The sketch should show proportion and relationship, not every decorative detail.

ASCII is successful when a person can understand the composition without reading a structural notation first.

## 5. Example SLL

### Identity

- **Title:** Negotiate With the Dragon
- **Version:** SLL 1.0
- **Product:** dark unisex T-shirt, centered upper chest

### Communication intent

- **Wearer signal:** I am the cautious, talk-first player who overthinks fantasy danger.
- **Viewer inference:** This wearer has recognizable D&D habits and can laugh at them.
- **Emotion:** insider recognition followed by dry humor
- **Shared context:** tabletop players understand dragons as threats and negotiation as a player strategy

### Design Triangle

- **Idea:** A cautious adventurer admits they would try diplomacy even when facing a dragon.
- **Phrase:** “I’M NOT SAYING I’D SURVIVE A DRAGON ATTACK, BUT I’D DEFINITELY TRY TO NEGOTIATE.”
- **Graphic:** a simplified dragon head leaning toward a tiny speech bubble
- **Relationship:** completion; the phrase supplies self-exposure while the graphic makes the dangerous situation immediate

### ASCII sketch

```text
+----------------------------------+
|     I'M NOT SAYING I'D SURVIVE   |
|          A DRAGON ATTACK         |
|                                  |
|          /\        .----.        |
|     ____/  \__    / ...  \       |
|   <  DRAGON   >--<  ...?  |      |
|     ----\  /--    \______/       |
|          \/                      |
|                 BUT              |
|                                  |
|       I'D DEFINITELY TRY         |
|          TO NEGOTIATE            |
+----------------------------------+
```

### Execution notes

- **Grammar / lead:** narrative stack; graphic is the central pivot, final phrase is the semantic payload
- **Typography:** condensed rough sans for setup; small neutral sans for “BUT”; bold slab serif for the final two lines
- **Graphic style:** one-color woodcut silhouette with low detail and strong outer contour
- **Colors:** warm bone ink on charcoal garment; optional muted red only in speech bubble
- **Texture:** light distress across text and graphic as one unified surface, not separate filters
- **Placement:** centered, approximately 28–30 cm wide, beginning below the collar safe area
- **Production:** keep dragon details and speech-bubble punctuation legible at thumbnail size; avoid thin isolated strokes

### Validation

- **Reading order:** danger setup -> dragon encounter -> self-revealing response
- **Thumbnail:** “DRAGON ATTACK” and “NEGOTIATE” remain the two readable anchors
- **Signal:** the final line makes the humor about the wearer’s behavior, not merely about dragons
- **Largest risk:** phrase length; setup must remain subordinate and compact

## 6. Quality requirements

A valid SLL must:

- represent the entire intended design in one sketch
- preserve the phrase exactly unless a phrase revision is explicitly recorded
- show an unambiguous visual lead and reading order
- make the graphic’s role and scale visible
- state fonts as categories or characteristics, not invented font names
- state colors in practical terms, including garment color assumptions
- distinguish semantic elements from decoration
- include print and thumbnail constraints
- remain understandable without exposing the internal layout model

An SLL fails when it is only a list of elements, an abstract token sequence, a mood board in prose, or ASCII that does not show the proposed composition.

## 7. Controlled flexibility

SLL should be concrete but not falsely precise. Approximate proportions, font categories, palette roles, and effect intensity are useful. Exact coordinates, final color values, and production dimensions may be added later when the product and print provider are known.

One SLL should present one recommended solution. Alternatives belong in separate SLL variants rather than being mixed into the same sketch.



---

# Generating SLL

## 1. Purpose

This page defines how a person or FusionCanvas should transform a Design Triangle into one complete SLL.

The public interaction is intentionally simple:

> Take this Design Triangle and create SLL for it.

The system performs the semantic decomposition and layout exploration internally. The output is the visual artifact defined in [[03 - Sketch Layout Language]].

## 2. Input contract

### Required input

- **Idea:** the intended concept, including wearer signal when known
- **Phrase:** the exact unbroken phrase
- **Graphic description:** the visual subject or system and its intended role

### Useful optional context

- target niche or audience
- intended emotion and tone
- buyer mode: self-purchase, gift, or mixed
- product type and print location
- garment colors
- brand style or collection rules
- required or forbidden motifs
- print constraints

Missing optional context may be inferred, but the SLL must label consequential assumptions.

## 3. Internal layout model

Before drawing ASCII, the generator creates a private working model. This replaces the old user-facing symbolic SLL notation.

The model should contain:

- wearer signal and viewer inference
- phrase segments and their semantic roles
- relative semantic importance
- graphic elements and their roles
- phrase/graphic relationship
- visual lead
- reading order
- composition grammar
- product and production constraints

Suggested semantic roles include context, identity, subject, setup, pivot, payload, qualifier, and reinforcement. They are descriptive, not a mandatory grammar. The role that carries the central statement receives priority; this is not always a punchline.

## 4. Generation method

### Step 1 — Normalize the Triangle

Restate the three corners without changing their meaning. Identify missing assumptions and distinguish a true idea from a topic.

If the Idea does not contain a social proposition, infer and state:

- what wearing the design says
- what the intended viewer should infer
- which emotion should follow

If no credible proposition can be formed, stop and request concept repair rather than drawing an attractive but empty layout.

### Step 2 — Test the carriers

Check whether the Phrase and Graphic reinforce, complete, or contrast with each other.

Repair or flag:

- a phrase that explains instead of expresses
- a graphic that is decorative or merely repeats a noun
- a joke with no wearer-relevant implication
- an identity claim too generic to create recognition

The generator may recommend revised wording, but it must not silently change the supplied phrase.

### Step 3 — Build semantic hierarchy

Segment the phrase by meaning, not by desired line length. Determine:

- what must be understood first
- what supplies context
- what carries the central statement
- what can be visually subordinate

Assign the graphic a role: lead, co-lead, context, payoff, connector, reinforcement, or texture. Decorative treatment is not a semantic role.

### Step 4 — Choose the visual responsibility

Classify the design:

- **text-led:** wording carries the primary statement
- **graphic-led:** the image carries the primary statement
- **balanced:** phrase and graphic need each other

This choice controls scale and hierarchy. It does not mean the secondary carrier is unimportant.

### Step 5 — Choose one composition grammar

Select the grammar that best matches the reading experience:

- statement stack
- narrative stack
- emblem
- image hero
- integrated interaction
- collection or sequence

Use a hybrid only when the secondary grammar solves a specific semantic problem. Do not choose a familiar template before understanding the message.

### Step 6 — Explore internally

Generate at least two rough layout candidates. Compare them using:

- statement fidelity
- clarity of wearer signal
- accuracy of viewer inference
- reading order
- thumbnail recognition
- phrase/graphic cooperation
- visual balance and negative space
- production robustness

Select one recommended candidate. Internal alternatives are not part of the normal output.

### Step 7 — Draw the ASCII sketch

Create a bounded portrait or product-appropriate canvas. Add actual phrase text, the graphic form or label, approximate scale, spacing, alignment, and interaction.

The sketch must make the full composition visible. Do not replace it with element IDs, relation statements, or prose.

### Step 8 — Add execution notes

Describe only decisions that materially affect the result:

- composition and visual lead
- typography characteristics
- graphic style and detail level
- palette and garment relationship
- texture and effects
- product placement and approximate scale
- production cautions

The notes and sketch must agree. If the notes say the graphic is dominant, it must appear dominant in the sketch.

### Step 9 — Validate

Run the SLL quality gates below. If a gate fails, revise the candidate or return to the Triangle.

## 5. Quality gates

### Social meaning

- Can the wearer signal be stated in one sentence?
- Will the intended viewer infer approximately that meaning?
- Does humor, if present, reveal identity, experience, attitude, or tension?

### Concept coherence

- Do Idea, Phrase, and Graphic support the same proposition?
- Does every visible element have a communicative or structural job?
- Is any accidental redundancy or contradiction present?

### Visual communication

- Is the first read intentional?
- Does the second read complete or deepen it?
- Is the central statement visible at the intended distance?
- Does the design retain a recognizable silhouette at thumbnail size?

### Product fitness

- Does the layout fit the product and print location?
- Is contrast sufficient on the assumed garment color?
- Are detail, line weight, spacing, and color count practical for printing?
- Will the design remain coherent across likely garment variants?

### SLL completeness

- Is the entire design present in one ASCII sketch?
- Are printed words distinguishable from art instructions?
- Are typography, color, style, placement, and effects covered in notes?
- Is the largest remaining risk stated honestly?

## 6. FusionCanvas feature contract

The feature should accept a Design Triangle and return a single SLL artifact. A practical conceptual output model is:

```text
SLL
  version
  title
  assumptions[]
  product
    type
    placement
    garment_color
  communication
    wearer_signal
    viewer_inference
    emotion
    shared_context
  triangle
    idea
    phrase
    graphic
    relationship
  composition
    grammar
    visual_lead
    reading_order[]
  ascii_sketch
  notes
    typography
    graphic_style
    colors
    texture_effects
    placement_scale
    production
  validation
    thumbnail_read
    distance_read
    signal_check
    largest_risk
  internal_layout_model (optional, hidden)
```

The stored representation may use JSON, database fields, or domain objects. The theory requires the meaning of these fields, not a specific serialization.

## 7. Behavioral rules for the feature

FusionCanvas should:

- preserve the supplied Triangle and record proposed revisions separately
- expose assumptions that change the design direction
- output one clear recommendation by default
- allow regeneration as a separate SLL variant
- keep the ASCII sketch and notes synchronized
- retain the internal layout model when reproducibility is useful
- permit later editing of both the sketch and annotations

FusionCanvas should not:

- expose symbolic element notation as a prerequisite for users
- select a layout solely because it matches a known template
- use style to compensate for an incoherent Triangle
- silently rewrite the phrase
- claim production precision that the available product data cannot support

## 8. Minimal command semantics

When asked:

> Create SLL for this Design Triangle.

the generator should return, in order:

1. important assumptions, if any
2. communication intent
3. normalized Design Triangle
4. one complete ASCII sketch
5. execution notes
6. validation and largest risk

That result is the handoff from concept theory to artwork production.


