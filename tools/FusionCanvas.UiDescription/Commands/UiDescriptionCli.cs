using System.Text;
using FusionCanvas.UiDescription.Diagnostics;
using FusionCanvas.UiDescription.Layout;
using FusionCanvas.UiDescription.Parsing;
using FusionCanvas.UiDescription.Rendering;
using FusionCanvas.UiDescription.Validation;

namespace FusionCanvas.UiDescription.Commands;

public static class UiDescriptionCli
{
    public const int Success = 0;
    public const int ValidationFailure = 2;
    public const int OperationalFailure = 3;

    public static Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (args.Length == 2 && args[0] == "validate")
        {
            return Task.FromResult(Validate(args[1], output, error));
        }

        if (args.Length == 6 && args[0] == "render" && args[2] == "--state" && args[4] == "--output")
        {
            return Task.FromResult(Render(args[1], args[3], args[5], output, error));
        }

        error.WriteLine("Usage:");
        error.WriteLine("  FusionCanvas.UiDescription validate <source.ui.yaml>");
        error.WriteLine("  FusionCanvas.UiDescription render <source.ui.yaml> --state <name> --output <wireframe.svg>");
        return Task.FromResult(ValidationFailure);
    }

    private static int Validate(string sourcePath, TextWriter output, TextWriter error)
    {
        var result = Load(sourcePath, error);
        if (result.ExitCode != Success)
        {
            return result.ExitCode;
        }

        output.WriteLine($"Valid UI description: {result.Document!.Screen.Id}");
        return Success;
    }

    private static int Render(string sourcePath, string stateName, string outputPath, TextWriter output, TextWriter error)
    {
        var loaded = Load(sourcePath, error);
        if (loaded.ExitCode != Success)
        {
            return loaded.ExitCode;
        }

        var projected = new UiStateProjector().Project(loaded.Document!, stateName);
        if (!projected.IsValid)
        {
            WriteDiagnostics(projected.Diagnostics, error);
            return ValidationFailure;
        }

        var layout = new UiLayoutEngine().Layout(projected.Document!);
        if (!layout.IsValid)
        {
            WriteDiagnostics(layout.Diagnostics, error);
            return ValidationFailure;
        }

        var svg = new SvgWireframeRenderer().Render(
            layout.Root!,
            projected.Document!.Screen.ViewportWidth,
            projected.Document.Screen.ViewportHeight,
            projected.Document.Screen.Title);

        try
        {
            WriteAtomically(outputPath, svg);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            error.WriteLine($"{outputPath}: error UIDL300: {exception.Message}");
            return OperationalFailure;
        }

        output.WriteLine($"Rendered {projected.Document.Screen.Id}:{stateName} -> {Path.GetFullPath(outputPath)}");
        return Success;
    }

    private static LoadResult Load(string sourcePath, TextWriter error)
    {
        var parsed = new UiDescriptionParser().ParseFile(sourcePath);
        if (!parsed.IsValid)
        {
            WriteDiagnostics(parsed.Diagnostics, error);
            var operational = parsed.Diagnostics.Any(diagnostic => diagnostic.Code == "UIDL001");
            return new LoadResult(null, operational ? OperationalFailure : ValidationFailure);
        }

        var validated = new UiDescriptionValidator().Validate(parsed.Document!);
        if (!validated.IsValid)
        {
            WriteDiagnostics(validated.Diagnostics, error);
            return new LoadResult(null, ValidationFailure);
        }

        return new LoadResult(validated.Document, Success);
    }

    private static void WriteAtomically(string outputPath, string contents)
    {
        var absolute = Path.GetFullPath(outputPath);
        var directory = Path.GetDirectoryName(absolute) ?? Directory.GetCurrentDirectory();
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, $".{Path.GetFileName(absolute)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporary, contents, new UTF8Encoding(false));
            File.Move(temporary, absolute, true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    private static void WriteDiagnostics(IEnumerable<UiDiagnostic> diagnostics, TextWriter error)
    {
        foreach (var diagnostic in diagnostics.Order())
        {
            error.WriteLine(diagnostic);
        }
    }

    private sealed record LoadResult(Model.UiDescriptionDocument? Document, int ExitCode);
}
