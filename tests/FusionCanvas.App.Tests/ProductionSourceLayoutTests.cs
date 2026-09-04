using System.Text.RegularExpressions;

namespace FusionCanvas.App.Tests;

public sealed class ProductionSourceLayoutTests
{
    private static readonly Regex TopLevelTypeDeclaration = new(
        @"^(?:(?:public|internal|file|private|protected|abstract|sealed|static|partial|readonly)\s+)*(?:class|record(?:\s+struct)?|interface|enum|struct|delegate)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void ProductionFiles_ContainAtMostOneTopLevelType()
    {
        var repositoryRoot = FindRepositoryRoot();
        var violations = new List<string>();

        foreach (var path in Directory.EnumerateFiles(Path.Combine(repositoryRoot, "src"), "*.cs", SearchOption.AllDirectories))
        {
            var typeNames = FindTopLevelTypeNames(File.ReadAllLines(path));
            if (typeNames.Count > 1)
            {
                violations.Add($"{Path.GetRelativePath(repositoryRoot, path)}: {string.Join(", ", typeNames)}");
            }
        }

        Assert.Empty(violations);
    }

    private static IReadOnlyList<string> FindTopLevelTypeNames(IReadOnlyList<string> lines)
    {
        var names = new List<string>();
        var braceDepth = 0;
        foreach (var sourceLine in lines)
        {
            var line = StripStringsAndComments(sourceLine);
            if (braceDepth == 0 && TopLevelTypeDeclaration.Match(line.Trim()) is { Success: true } match)
            {
                names.Add(match.Groups["name"].Value);
            }

            braceDepth += line.Count(static character => character == '{');
            braceDepth -= line.Count(static character => character == '}');
        }

        return names;
    }

    private static string StripStringsAndComments(string line)
    {
        var withoutComments = line.Split("//", 2, StringSplitOptions.None)[0];
        return Regex.Replace(withoutComments, @"""(?:\\.|[^""\\])*""|'(?:\\.|[^'\\])*'", "string", RegexOptions.CultureInvariant);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "FusionCanvas.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate the FusionCanvas repository root.");
    }
}
