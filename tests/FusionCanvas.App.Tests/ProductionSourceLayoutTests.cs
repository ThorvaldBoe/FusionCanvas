namespace FusionCanvas.App.Tests;

public sealed class ProductionSourceLayoutTests
{
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
        var tokens = Tokenize(lines);
        var names = new List<string>();
        var scopes = new Stack<bool>();
        var pendingTypeBody = false;
        var consumedHeaderThrough = -1;

        for (var index = 0; index < tokens.Count; index++)
        {
            if (index <= consumedHeaderThrough)
            {
                continue;
            }

            var token = tokens[index];
            if (token.Kind == TokenKind.Identifier && IsTypeKeyword(token.Text))
            {
                var declaration = FindTypeDeclaration(tokens, index);
                if (declaration is not null)
                {
                    if (!IsInsideType(scopes))
                    {
                        names.Add(declaration.Value.Name);
                    }

                    consumedHeaderThrough = declaration.Value.NameIndex;
                    pendingTypeBody = declaration.Value.HasBody;
                }

                continue;
            }

            if (token.Kind == TokenKind.OpenBrace)
            {
                scopes.Push(pendingTypeBody);
                pendingTypeBody = false;
            }
            else if (token.Kind == TokenKind.CloseBrace && scopes.Count > 0)
            {
                scopes.Pop();
            }
            else if (token.Kind == TokenKind.Semicolon)
            {
                pendingTypeBody = false;
            }
        }

        return names;
    }

    [Fact]
    public void FindTopLevelTypeNames_IgnoresNestedTypesAndCSharpLiterals()
    {
        var lines = new[]
        {
            "namespace Sample",
            "{",
            "    public class Outer",
            "    {",
            "        private sealed class Nested { }",
            "        private const string Raw = \"\"\"",
            "            class FakeInRaw { }",
            "        \"\"\";",
            "        private string Interpolated => $\"value {{ class FakeInInterpolation {{ }} }}\";",
            "    }",
            "}"
        };

        Assert.Equal(["Outer"], FindTopLevelTypeNames(lines));
    }

    private static IReadOnlyList<Token> Tokenize(IReadOnlyList<string> lines)
    {
        var source = string.Join(Environment.NewLine, lines);
        var tokens = new List<Token>();
        var index = 0;

        while (index < source.Length)
        {
            if (char.IsWhiteSpace(source[index]))
            {
                index++;
                continue;
            }

            if (source[index] == '#')
            {
                index = SkipToLineEnd(source, index);
                continue;
            }

            if (source[index] == '/' && index + 1 < source.Length && source[index + 1] == '/')
            {
                index = SkipToLineEnd(source, index + 2);
                continue;
            }

            if (source[index] == '/' && index + 1 < source.Length && source[index + 1] == '*')
            {
                index = SkipBlockComment(source, index + 2);
                continue;
            }

            if (TrySkipStringOrCharacterLiteral(source, ref index))
            {
                continue;
            }

            if (IsIdentifierStart(source[index]))
            {
                var start = index++;
                while (index < source.Length && IsIdentifierPart(source[index]))
                {
                    index++;
                }

                tokens.Add(new Token(TokenKind.Identifier, source[start..index]));
                continue;
            }

            var kind = source[index] switch
            {
                '{' => TokenKind.OpenBrace,
                '}' => TokenKind.CloseBrace,
                '(' => TokenKind.OpenParenthesis,
                ';' => TokenKind.Semicolon,
                _ => TokenKind.Other
            };
            tokens.Add(new Token(kind, null));
            index++;
        }

        return tokens;
    }

    private static TypeDeclaration? FindTypeDeclaration(IReadOnlyList<Token> tokens, int keywordIndex)
    {
        var keyword = tokens[keywordIndex].Text;
        var nameIndex = keyword switch
        {
            "record" when keywordIndex + 1 < tokens.Count && tokens[keywordIndex + 1].Text is "class" or "struct"
                => keywordIndex + 2,
            "class" or "interface" or "enum" or "struct" => keywordIndex + 1,
            "delegate" => FindDelegateNameIndex(tokens, keywordIndex + 1),
            _ => -1
        };

        if (nameIndex < 0 || nameIndex >= tokens.Count || tokens[nameIndex].Kind != TokenKind.Identifier)
        {
            return null;
        }

        return new TypeDeclaration(tokens[nameIndex].Text!, nameIndex, keyword != "delegate");
    }

    private static int FindDelegateNameIndex(IReadOnlyList<Token> tokens, int startIndex)
    {
        var candidate = -1;
        for (var index = startIndex; index < tokens.Count; index++)
        {
            if (tokens[index].Kind == TokenKind.Identifier)
            {
                candidate = index;
            }
            else if (tokens[index].Kind is TokenKind.OpenParenthesis or TokenKind.Semicolon)
            {
                return candidate;
            }
        }

        return -1;
    }

    private static bool IsInsideType(IEnumerable<bool> scopes) => scopes.Any(static isTypeBody => isTypeBody);

    private static bool IsTypeKeyword(string? text) => text is "class" or "record" or "interface" or "enum" or "struct" or "delegate";

    private static bool IsIdentifierStart(char character) => character == '_' || char.IsLetter(character);

    private static bool IsIdentifierPart(char character) => character == '_' || char.IsLetterOrDigit(character);

    private static int SkipToLineEnd(string source, int index)
    {
        while (index < source.Length && source[index] is not '\r' and not '\n')
        {
            index++;
        }

        return index;
    }

    private static int SkipBlockComment(string source, int index)
    {
        while (index + 1 < source.Length && !(source[index] == '*' && source[index + 1] == '/'))
        {
            index++;
        }

        return Math.Min(index + 2, source.Length);
    }

    private static bool TrySkipStringOrCharacterLiteral(string source, ref int index)
    {
        var start = index;
        var prefixEnd = index;
        while (prefixEnd < source.Length && source[prefixEnd] is '$' or '@')
        {
            prefixEnd++;
        }

        if (prefixEnd >= source.Length || source[prefixEnd] is not ('"' or '\''))
        {
            return false;
        }

        var quote = source[prefixEnd];
        var quoteCount = 0;
        while (prefixEnd + quoteCount < source.Length && source[prefixEnd + quoteCount] == quote)
        {
            quoteCount++;
        }

        index = prefixEnd + quoteCount;
        if (quote == '"' && quoteCount >= 3)
        {
            while (index < source.Length)
            {
                if (index + quoteCount <= source.Length && source.AsSpan(index, quoteCount).IndexOf('"') == 0)
                {
                    index += quoteCount;
                    return true;
                }

                index++;
            }

            return true;
        }

        var isVerbatim = source[start..prefixEnd].Contains('@');
        while (index < source.Length)
        {
            if (source[index] == '\\' && !isVerbatim)
            {
                index = Math.Min(index + 2, source.Length);
                continue;
            }

            if (source[index] == quote)
            {
                if (isVerbatim && index + 1 < source.Length && source[index + 1] == quote)
                {
                    index += 2;
                    continue;
                }

                index++;
                return true;
            }

            index++;
        }

        return true;
    }

    private readonly record struct TypeDeclaration(string Name, int NameIndex, bool HasBody);

    private readonly record struct Token(TokenKind Kind, string? Text);

    private enum TokenKind
    {
        Identifier,
        OpenBrace,
        CloseBrace,
        OpenParenthesis,
        Semicolon,
        Other
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
