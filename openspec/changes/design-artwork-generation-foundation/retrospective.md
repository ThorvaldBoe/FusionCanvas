# Retrospective: artwork generation corrections

User feedback after generation exposed three gaps that were not apparent from the original acceptance pass:

- Text and image model catalogs can return the same ID with different capability metadata. Keeping duplicate entries made resolver order affect whether a persisted Artwork model appeared unavailable. Future catalog composition should normalize model identity before it feeds readiness and selection.
- A successful generated asset was persisted, but the post-save Design reload reused the generation cancellation token. Since loading cancels the prior artwork operation, the UI reported an interrupted refresh even though save had succeeded. The success path should be verified through the refreshed presentation state, not only through persistence assertions.
- Niche fields existed in Store settings but were not part of the image prompt. Prompt-context review should trace Item relationships through the complete generation path, and artwork prompts should state clearly that the result is printable artwork rather than a product mockup.

The code corrections compile. Focused regression tests and the required solution test baseline remain outstanding.
