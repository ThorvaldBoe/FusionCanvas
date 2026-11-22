## ADDED Requirements

### Requirement: Favorite Colors are scoped to a Blueprint Offering
FusionCanvas SHALL let the creator mark active Color Option Values as favorites for one Blueprint Offering. Favorite state SHALL be attached to the stable Color Value identity and SHALL NOT change the Offering's sellable Variant set or automatically select Colors for any Item.

#### Scenario: Creator marks a commonly used Color as a favorite
- **WHEN** the creator marks an active Color Value as a favorite in Offering setup
- **THEN** FusionCanvas persists that preference for that Offering
- **AND** the Color appears in the favorite section for Items using the same Offering
- **AND** other Offerings that happen to use the same display name remain unchanged

#### Scenario: Creator removes a Color from favorites
- **WHEN** the creator unmarks a favorite Color Value
- **THEN** the Color is removed from the favorite section after refresh
- **AND** any Item that already selected that Color keeps its Listing selection

### Requirement: Listing progressively discloses Offering Colors
For an editable Item with a selected fixed-Provider Offering, Listing SHALL present the Offering's favorite Colors as the primary compact selection path and a **More Options** action for the complete collection of active configured Color Values, including favorites. More Options SHALL open a focused, keyboard-accessible selection surface without replacing the Item's Listing context and SHALL provide case-insensitive search over Color names. Color choice SHALL be distinct from preview selection: activating a Color previews the current artwork on its approximate flat swatch, while a separate selection control adds or removes that Color from the Item. Filtering and preview SHALL preserve draft checks. The preview SHALL use the stored approximate Color value when available and SHALL NOT claim to reproduce fabric texture.

#### Scenario: Creator uses a favorite Color
- **WHEN** the creator selects a favorite Color in Listing
- **THEN** that Color becomes selected for the Item
- **AND** its compatible configured Sizes become part of the desired Listing variant set
- **AND** the selected state remains visible after reload

#### Scenario: Creator inspects an uncommon Color
- **WHEN** the creator activates **More Options**
- **THEN** the focused surface lists every active configured Color Value for the Item's Offering, including favorites, with its name and approximate swatch when available
- **AND** selecting a row for preview does not change the Item's selected Colors
- **AND** closing without confirming preserves the prior Listing selection

#### Scenario: Creator searches the full Color collection
- **WHEN** the creator enters a search term in More Options
- **THEN** FusionCanvas filters the complete Offering Color collection case-insensitively by Color name
- **AND** any draft checks remain attached to their stable Color identities while rows are filtered
- **AND** clearing the search restores the full collection without changing the Item selection

#### Scenario: Creator selects several uncommon Colors
- **WHEN** the creator checks multiple Colors in More Options and confirms
- **THEN** all checked Colors are added to the Item's Listing selection
- **AND** previously selected Colors remain selected unless explicitly unchecked
- **AND** each newly selected Color uses the first artwork variant by default

#### Scenario: Color has no approximate value
- **WHEN** a configured Color lacks a usable approximate color value
- **THEN** FusionCanvas displays a neutral swatch with the Color name
- **AND** does not invent a provider color or imply a realistic fabric preview

#### Scenario: Color has no active provider-enabled Variants
- **WHEN** a configured Color has no active provider-enabled sellable Variant
- **THEN** FusionCanvas shows the Color as unavailable with an actionable explanation
- **AND** does not allow the creator to add it to the Item's desired Listing set

#### Scenario: Creator cancels More Options
- **WHEN** the creator closes, cancels, or presses Escape in More Options before confirming
- **THEN** temporary checks and preview selection are discarded
- **AND** the prior Item selection remains unchanged
- **AND** keyboard focus returns to the **More Options** action when practical

### Requirement: Listing Color selection includes all configured Sizes and preserves stock state
When a creator selects a Color for an Item, FusionCanvas SHALL include every active provider-enabled Offering Variant that has that Color, across all configured Sizes, in the desired Listing variant set. Current provider availability SHALL be displayed as status information but SHALL NOT hide or prevent selection of an otherwise active Variant. The Item's selected Color SHALL persist independently of global Offering favorites; compatible Size variants SHALL be derived from the current Offering configuration.

#### Scenario: Color has several configured Sizes
- **WHEN** the creator selects a Color with configured sellable Variants in multiple Sizes
- **THEN** all those Variant identities are included in the Item's desired Listing selection
- **AND** the creator does not need to select each Size individually for this module

#### Scenario: Some configured Sizes are out of stock
- **WHEN** a selected Color has active sellable Variants with both available and out-of-stock provider status
- **THEN** all configured Sizes remain in the desired Listing selection
- **AND** each Variant's current stock status is visible
- **AND** out-of-stock status alone does not archive the Variant or remove it from the selection

#### Scenario: Provider catalog is unavailable
- **WHEN** the provider catalog cannot be refreshed
- **THEN** FusionCanvas keeps locally configured Variants and their last known availability visible with an unverified status
- **AND** does not silently clear the Item's selection

### Requirement: Each selected Listing Color has an explicit artwork variant
FusionCanvas SHALL associate each selected Item Color with exactly one artwork variant belonging to that Item's Design. When a Color is first selected, FusionCanvas SHALL assign the first artwork variant by default. The creator SHALL be able to explicitly assign another artwork variant to a Color without changing the global favorite preference or other Colors' assignments. A Color cannot be published or used for mockup generation as if it had artwork when its assigned variant has no valid Design Area artwork.

#### Scenario: First artwork variant is assigned by default
- **WHEN** the creator selects a Color for an Item with one or more artwork variants
- **THEN** the first artwork variant is assigned to that Color
- **AND** the creator can see and change the assignment in Listing

#### Scenario: Creator assigns an alternate artwork variant
- **WHEN** the creator chooses another artwork variant for one selected Color
- **THEN** only that Color's assignment changes
- **AND** other selected Colors and the Design-stage artwork variants remain unchanged

#### Scenario: Assigned artwork variant has no usable artwork
- **WHEN** the assigned artwork variant has no valid Design Area artwork for the Offering
- **THEN** Listing identifies the affected Color and missing artwork as a readiness issue
- **AND** does not report that Color as ready for publication or mockup generation

#### Scenario: Creator removes a selected Color
- **WHEN** the creator removes a Color from the Item's Listing selection
- **THEN** the Color and its per-Item artwork assignment are removed from the desired Listing set
- **AND** the Offering's favorite state and source artwork variant remain unchanged
