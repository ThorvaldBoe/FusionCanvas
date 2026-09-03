# Design

Use a checked-in QA note as the durable baseline. Keep the warning count tied to the serial build command and explicitly distinguish formatter execution failure from formatter success. Future cleanup batches can update the measured count and close the debt incrementally.

## Implementation plan

1. Capture the serial build and formatter outcomes.
2. Add the baseline document without changing analyzer configuration.
3. Validate OpenSpec and rerun the build.
