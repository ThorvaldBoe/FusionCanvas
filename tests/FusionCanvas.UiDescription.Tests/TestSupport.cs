using FusionCanvas.UiDescription.Model;
using FusionCanvas.UiDescription.Parsing;
using FusionCanvas.UiDescription.Validation;

namespace FusionCanvas.UiDescription.Tests;

internal static class TestSupport
{
    public const string MinimalYaml = """
        schemaVersion: 1
        tokenProfile: fusioncanvas-wireframe-v1
        screen:
          id: sample
          title: Sample
          viewport:
            width: 640
            height: 480
          root:
            id: root
            kind: stack
            gap: section
            padding: section
            children:
              - id: heading
                kind: text
                variant: screen-heading
                text: Sample heading
              - id: action
                kind: button
                variant: primary
                text: Continue
        states:
          default:
            overrides: []
          disabled:
            overrides:
              - target: action
                enabled: false
                text: Unavailable
        """;

    public static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "FusionCanvas.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new InvalidOperationException("Could not locate the repository root.");
        }
    }

    public static UiDescriptionDocument ParseValidated(string source = MinimalYaml)
    {
        var parsed = new UiDescriptionParser().Parse(source);
        Assert.True(parsed.IsValid, string.Join(Environment.NewLine, parsed.Diagnostics));
        var validated = new UiDescriptionValidator().Validate(parsed.Document!);
        Assert.True(validated.IsValid, string.Join(Environment.NewLine, validated.Diagnostics));
        return validated.Document!;
    }

    public static IEnumerable<UiComponent> Flatten(UiComponent root)
    {
        yield return root;
        foreach (var child in root.Children.SelectMany(Flatten))
        {
            yield return child;
        }
    }
}
