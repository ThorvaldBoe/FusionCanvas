## ADDED Requirements

### Requirement: Rating filter supports comparison thresholds
The navigation rating filter SHALL retain the existing All ratings, Unrated, and exact-rating choices and SHALL additionally offer greater-than 1, greater-than 2, greater-than 3, greater-than 4, less-than 5, less-than 4, less-than 3, and less-than 2 choices. A selected comparison SHALL apply immediately and match only rated Items satisfying the selected strict comparison; unrated Items SHALL not match a comparison.

#### Scenario: Greater-than rating filters match strict thresholds
- **WHEN** the user selects each greater-than choice
- **THEN** Items with ratings strictly above that choice's threshold appear
- **AND** Items equal to or below the threshold and unrated Items are hidden

#### Scenario: Less-than rating filters match strict thresholds
- **WHEN** the user selects each less-than choice
- **THEN** Items with ratings strictly below that choice's threshold appear
- **AND** Items equal to or above the threshold and unrated Items are hidden

#### Scenario: Comparison choices apply immediately
- **WHEN** the user changes the rating dropdown to a comparison choice
- **THEN** the navigation tree refreshes immediately using that comparison
- **AND** clearing the rating filter restores the unfiltered rating dimension

