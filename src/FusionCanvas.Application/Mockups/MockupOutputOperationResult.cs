namespace FusionCanvas.Application.Mockups;

public sealed record MockupOutputOperationResult(bool Succeeded, string? Error = null)
{
    public static MockupOutputOperationResult Success() => new(true);

    public static MockupOutputOperationResult Failure(string error) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "The mockup operation could not be completed." : error);
}
