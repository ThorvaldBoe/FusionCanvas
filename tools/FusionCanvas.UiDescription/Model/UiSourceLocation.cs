namespace FusionCanvas.UiDescription.Model;

public sealed record UiSourceLocation(string Path, int Line, int Column)
{
    public static UiSourceLocation Unknown(string path) => new(path, 0, 0);
}
