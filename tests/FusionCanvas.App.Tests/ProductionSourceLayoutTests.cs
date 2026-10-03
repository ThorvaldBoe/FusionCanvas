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

        Assert.True(
            violations.Count == 0,
            $"Production files with multiple top-level types:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
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
        var result = new System.Text.StringBuilder(line.Length);
        var inString = false;
        var stringDelimiter = '\0';
        var escaped = false;
        var interpolatedString = false;
        var interpolationDepth = 0;
        var nestedStringDelimiter = '\0';
        var nestedStringEscaped = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (inString)
            {
                if (interpolationDepth > 0)
                {
                    if (nestedStringDelimiter != '\0')
                    {
                        if (nestedStringEscaped)
                        {
                            nestedStringEscaped = false;
                        }
                        else if (character == '\\')
                        {
                            nestedStringEscaped = true;
                        }
                        else if (character == nestedStringDelimiter)
                        {
                            nestedStringDelimiter = '\0';
                        }

                        result.Append(' ');
                        continue;
                    }

                    if (character is '"' or '\'')
                    {
                        nestedStringDelimiter = character;
                    }
                    else if (character == '{')
                    {
                        interpolationDepth++;
                    }
                    else if (character == '}')
                    {
                        interpolationDepth--;
                    }

                    result.Append(' ');
                    continue;
                }

                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (interpolatedString && character == '{')
                {
                    interpolationDepth = 1;
                }
                else if (character == stringDelimiter)
                {
                    inString = false;
                }

                result.Append(' ');
                continue;
            }

            if (character == '/' && index + 1 < line.Length && line[index + 1] == '/')
            {
                break;
            }

            if (character is '"' or '\'')
            {
                inString = true;
                stringDelimiter = character;
                interpolatedString = character == '"' && index > 0 && line[index - 1] == '$';
                result.Append("string");
                continue;
            }

            result.Append(character);
        }

        return result.ToString();
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
