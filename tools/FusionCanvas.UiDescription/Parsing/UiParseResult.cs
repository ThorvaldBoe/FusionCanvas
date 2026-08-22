using FusionCanvas.UiDescription.Diagnostics;
using FusionCanvas.UiDescription.Model;

namespace FusionCanvas.UiDescription.Parsing;

public sealed record UiParseResult(UiDescriptionDocument? Document, IReadOnlyList<UiDiagnostic> Diagnostics)
{
    public bool IsValid => Document is not null && Diagnostics.Count == 0;
}
