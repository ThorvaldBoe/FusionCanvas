namespace FusionCanvas.Application.AI;

public static class ArtworkGenerationReadinessPolicy
{
    public static ArtworkGenerationReadiness Evaluate(bool isEditable, bool artworkProfileReady, bool compatibleEndpointReady, bool hasListingConfiguration, bool hasActiveTarget, bool designTriangleComplete, bool hasDefaultRowWithSelectedColor)
    {
        var blockers = new List<string>();
        if (!isEditable) blockers.Add("Design is read-only.");
        if (!artworkProfileReady) blockers.Add("Configure an Artwork image model in AI Settings.");
        if (!compatibleEndpointReady) blockers.Add("No privacy-compatible image endpoint is ready.");
        if (!hasListingConfiguration) blockers.Add("Select a Listing Configuration.");
        if (!hasActiveTarget) blockers.Add("Choose an active Design Area target.");
        if (!designTriangleComplete) blockers.Add("Complete the Concept idea, Phrase, and Graphic direction.");
        if (!hasDefaultRowWithSelectedColor) blockers.Add("Select at least one product color.");
        return new ArtworkGenerationReadiness(blockers.Count == 0, blockers);
    }
}
