using FusionCanvas.Application.Items.Import;

namespace FusionCanvas.Integration.Items.Import;

/// <summary>
/// Parses and writes the item import CSV format: seven semi-colon-delimited
/// columns (Title;Base Idea;Concept Idea;Phrase;Graphic;Notes;Tags).
/// A double semi-colon (;;) is an escaped literal semi-colon; a lone semi-colon
/// is a column separator. Because an empty field between two non-empty fields is
/// written as ;; and therefore read as an escaped semi-colon, empty fields are
/// only representable as the trailing field; users resolve ambiguous rows in the
/// import dialog's raw-source editor.
/// </summary>
public sealed class ItemCsvCodec : IItemCsvCodec
{
    private const int ExpectedColumnCount = 7;

    private static readonly string[] Headings =
    [
        "Title", "Base Idea", "Concept Idea", "Phrase", "Graphic", "Notes", "Tags"
    ];

    private const string SampleCsv =
        "Title;Base Idea;Concept Idea;Phrase;Graphic;Notes;Tags\r\n" +
        "Retro coffee tee;Coffee pun;Retro vibe;Coffee time;Retro cup;Use ;; for a semi-colon;funny,caffeine\r\n" +
        "Summer quote tee;Summer slogan;Beach vibe;Summer in the city;Sun graphic;Beach summer notes;summer,fresh\r\n";

    public ItemCsvParseResult Parse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var rows = new List<ItemCsvRow>();
        var errors = new List<ItemCsvParseError>();

        var normalized = source.Replace("\r\n", "\n");
        var rawLines = normalized.Split('\n');

        for (var index = 0; index < rawLines.Length; index++)
        {
            if (index == rawLines.Length - 1 && string.IsNullOrEmpty(rawLines[index]))
            {
                continue;
            }

            var lineNumber = index + 1;
            var fields = SplitFields(rawLines[index]);
            if (fields.Count != ExpectedColumnCount)
            {
                errors.Add(new ItemCsvParseError(
                    lineNumber,
                    $"Line must contain exactly {ExpectedColumnCount} columns separated by single semi-colons."));
                continue;
            }

            if (index == 0 && IsHeader(fields))
            {
                continue;
            }

            var title = fields[0].Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                errors.Add(new ItemCsvParseError(lineNumber, "Title is required."));
                continue;
            }

            rows.Add(new ItemCsvRow(
                title,
                NormalizeOptional(fields[1]),
                NormalizeOptional(fields[2]),
                NormalizeOptional(fields[3]),
                NormalizeOptional(fields[4]),
                NormalizeOptional(fields[5]),
                SplitTags(fields[6]),
                lineNumber));
        }

        return new ItemCsvParseResult(rows, errors);
    }

    public string WriteSample() => SampleCsv;

    private static bool IsHeader(IReadOnlyList<string> fields) =>
        fields.Count == Headings.Length &&
        fields.Select((field, index) => string.Equals(field.Trim(), Headings[index], StringComparison.OrdinalIgnoreCase))
            .All(isMatch => isMatch);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IReadOnlyList<string> SplitTags(string tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return [];
        }

        return tags
            .Split(',', StringSplitOptions.TrimEntries)
            .Where(tag => tag.Length > 0)
            .ToArray();
    }

    private static List<string> SplitFields(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();

        for (var index = 0; index < line.Length; index++)
        {
            if (line[index] != ';')
            {
                current.Append(line[index]);
                continue;
            }

            if (index + 1 < line.Length && line[index + 1] == ';')
            {
                current.Append(';');
                index++;
            }
            else
            {
                fields.Add(current.ToString());
                current.Clear();
            }
        }

        fields.Add(current.ToString());
        return fields;
    }
}
