namespace FusionCanvas.Application.Ideation;

public static class IdeationCountPolicy
{
    public const int DefaultCount = 5;
    public const int MinimumCount = 1;
    public const int MaximumCount = 20;

    public static bool IsValid(int count) => count is >= MinimumCount and <= MaximumCount;

    public static bool TryParse(string? text, out int count) =>
        int.TryParse(text, out count) && IsValid(count);
}
