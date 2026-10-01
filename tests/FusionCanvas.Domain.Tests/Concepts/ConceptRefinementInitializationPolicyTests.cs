using FusionCanvas.Domain.Concepts;

namespace FusionCanvas.Domain.Tests.Concepts;

public sealed class ConceptRefinementInitializationPolicyTests
{
    [Fact]
    public void CanInitializeFromBaseIdea_RequiresBaseIdeaAndEmptyCorners()
    {
        Assert.True(ConceptRefinementInitializationPolicy.CanInitializeFromBaseIdea(
            "Base idea", "", " ", null));
        Assert.False(ConceptRefinementInitializationPolicy.CanInitializeFromBaseIdea(
            " ", "", "", ""));
        Assert.False(ConceptRefinementInitializationPolicy.CanInitializeFromBaseIdea(
            "Base idea", "Concept", "", ""));
        Assert.False(ConceptRefinementInitializationPolicy.CanInitializeFromBaseIdea(
            "Base idea", "", "Phrase", ""));
        Assert.False(ConceptRefinementInitializationPolicy.CanInitializeFromBaseIdea(
            "Base idea", "", "", "Graphic"));
    }

    [Fact]
    public void AreConceptFieldsEmpty_TreatsWhitespaceAsEmpty()
    {
        Assert.True(ConceptRefinementInitializationPolicy.AreConceptFieldsEmpty("", " ", null));
        Assert.False(ConceptRefinementInitializationPolicy.AreConceptFieldsEmpty("Idea", "", ""));
    }
}
