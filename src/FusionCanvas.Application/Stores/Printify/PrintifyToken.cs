namespace FusionCanvas.Application.Stores.Printify;

public static class PrintifyToken
{
    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && !value.Trim().Any(char.IsControl);
}
