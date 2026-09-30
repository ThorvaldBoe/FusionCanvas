using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.App.Mockups;

internal static class MockupTemplateReadinessMessageTranslator
{
    public static string Translate(MockupTemplateReadinessBlocker blocker) => blocker switch
    {
        MockupTemplateReadinessBlocker.Archived => "Restore the template before use.",
        MockupTemplateReadinessBlocker.MissingTargetDesignArea => "Choose a Design Area.",
        MockupTemplateReadinessBlocker.InvalidTargetDesignArea => "Choose an active Design Area from this Offering.",
        MockupTemplateReadinessBlocker.MissingColors => "Choose at least one applicable Color.",
        MockupTemplateReadinessBlocker.InvalidColors => "Remove unavailable Colors.",
        MockupTemplateReadinessBlocker.MissingCompatibleVariants => "Add an active Variant that uses the selected Colors.",
        MockupTemplateReadinessBlocker.IncompatibleVariants => "Choose a Design Area compatible with every implied Variant.",
        MockupTemplateReadinessBlocker.MissingImage => "Choose a mockup image.",
        MockupTemplateReadinessBlocker.MissingMapping => "Add a valid design-area placement mapping.",
        MockupTemplateReadinessBlocker.KnownImageColorIncompatibility => "Choose Colors supported by the selected image.",
        MockupTemplateReadinessBlocker.MissingSourceApplicability => "Choose applicability options for each source image.",
        MockupTemplateReadinessBlocker.InvalidSourceApplicability => "Remove unavailable applicability options from source images.",
        MockupTemplateReadinessBlocker.MissingVariantSourceImage => "Configure a matching source image for every compatible Variant.",
        MockupTemplateReadinessBlocker.AmbiguousVariantSourceImages => "Adjust source-image applicability so each compatible Variant matches exactly one image.",
        _ => blocker.ToString()
    };
}
