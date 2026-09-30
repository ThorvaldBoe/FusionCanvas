using FusionCanvas.App.Mockups;
using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.App.Tests;

public sealed class MockupTemplateReadinessMessageTranslatorTests
{
    [Theory]
    [InlineData(MockupTemplateReadinessBlocker.Archived, "Restore the template before use.")]
    [InlineData(MockupTemplateReadinessBlocker.MissingTargetDesignArea, "Choose a Design Area.")]
    [InlineData(MockupTemplateReadinessBlocker.InvalidTargetDesignArea, "Choose an active Design Area from this Offering.")]
    [InlineData(MockupTemplateReadinessBlocker.MissingColors, "Choose at least one applicable Color.")]
    [InlineData(MockupTemplateReadinessBlocker.InvalidColors, "Remove unavailable Colors.")]
    [InlineData(MockupTemplateReadinessBlocker.MissingCompatibleVariants, "Add an active Variant that uses the selected Colors.")]
    [InlineData(MockupTemplateReadinessBlocker.IncompatibleVariants, "Choose a Design Area compatible with every implied Variant.")]
    [InlineData(MockupTemplateReadinessBlocker.MissingImage, "Choose a mockup image.")]
    [InlineData(MockupTemplateReadinessBlocker.MissingMapping, "Add a valid design-area placement mapping.")]
    [InlineData(MockupTemplateReadinessBlocker.KnownImageColorIncompatibility, "Choose Colors supported by the selected image.")]
    [InlineData(MockupTemplateReadinessBlocker.MissingSourceApplicability, "Choose applicability options for each source image.")]
    [InlineData(MockupTemplateReadinessBlocker.InvalidSourceApplicability, "Remove unavailable applicability options from source images.")]
    [InlineData(MockupTemplateReadinessBlocker.MissingVariantSourceImage, "Configure a matching source image for every compatible Variant.")]
    [InlineData(MockupTemplateReadinessBlocker.AmbiguousVariantSourceImages, "Adjust source-image applicability so each compatible Variant matches exactly one image.")]
    public void Translate_ReturnsTheEstablishedGuidance(MockupTemplateReadinessBlocker blocker, string expectedMessage)
    {
        Assert.Equal(expectedMessage, MockupTemplateReadinessMessageTranslator.Translate(blocker));
    }
}
