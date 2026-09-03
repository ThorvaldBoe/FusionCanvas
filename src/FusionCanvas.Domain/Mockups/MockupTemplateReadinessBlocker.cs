using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.Domain.Mockups;

public enum MockupTemplateReadinessBlocker
{
    Archived,
    MissingTargetDesignArea,
    InvalidTargetDesignArea,
    MissingColors,
    InvalidColors,
    MissingCompatibleVariants,
    IncompatibleVariants,
    MissingImage,
    MissingMapping,
    KnownImageColorIncompatibility
}
