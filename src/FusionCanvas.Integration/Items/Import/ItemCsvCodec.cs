using System.Text;
using FusionCanvas.Application.Items.Import;

namespace FusionCanvas.Integration.Items.Import;

/// <summary>
/// Parses the item import CSV format: seven semi-colon-delimited columns
/// (Title;Base Idea;Concept Idea;Phrase;Graphic;Notes;Tags) using standard CSV
/// field quoting, matching the format produced by the export feature.
/// A field is quoted with double quotes when it contains a `;`, `"`, CR, or LF;
/// an embedded double quote is written as two double quotes (`""`). Empty fields
/// are represented by empty space between separators and can appear anywhere in a
/// row, so an import of an exported file round-trips without column shifts.
/// </summary>
public sealed class ItemCsvCodec : IItemCsvCodec
{
    private const int ExpectedColumnCount = 7;

    private static readonly string[] Headings =
    [
        "Title", "Base Idea", "Concept Idea", "Phrase", "Graphic", "Notes", "Tags"
    ];

    private static readonly string ColumnList = string.Join(", ", Headings);

    private const string SampleCsv =
        "Title;Base Idea;Concept Idea;Phrase;Graphic;Notes;Tags\r\n" +
        "Retro coffee tee;Coffee pun;Retro vibe;Coffee time;Retro cup;\"Use \"\"quotes\"\"; carefully\";funny,caffeine\r\n" +
        "Summer quote tee;Summer slogan;Beach vibe;Summer in the city;Sun graphic;Beach summer notes;summer,fresh\r\n";

    public ItemCsvParseResult Parse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var rows = new List<ItemCsvRow>();
        var errors = new List<ItemCsvParseError>();

        var records = Tokenize(source);

        for (var index = 0; index < records.Count; index++)
        {
            var (lineNumber, fields) = records[index];

            if (fields.Length != ExpectedColumnCount)
            {
                errors.Add(new ItemCsvParseError(
                    lineNumber,
                    $"Expected {ExpectedColumnCount} columns in this order: {ColumnList}; found {fields.Length}."));
                continue;
            }

            if (index == 0 && IsHeader(fields))
            {
                continue;
            }

            var title = fields[0].Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                errors.Add(new ItemCsvParseError(lineNumber, "The Title field is required."));
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

    private static List<(int LineNumber, string[] Fields)> Tokenize(string source)
    {
        var records = new List<(int, string[])>();
        var recordStartLine = 1;
        var line = 1;
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        var index = 0;

        void CloseField()
        {
            fields.Add(current.ToString());
            current.Clear();
        }

        void CloseRecord()
        {
            fields.Add(current.ToString());
            current.Clear();
            records.Add((recordStartLine, fields.ToArray()));
            fields = new List<string>();
        }

        while (index < source.Length)
        {
            var ch = source[index];

            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (index + 1 < source.Length && source[index + 1] == '"')
                    {
                        current.Append('"');
                        index += 2;
                        continue;
                    }

                    inQuotes = false;
                    index++;
                    continue;
                }

                if (ch == '\n')
                {
                    line++;
                }

                current.Append(ch);
                index++;
                continue;
            }

            if (ch == '"' && current.Length == 0)
            {
                inQuotes = true;
                index++;
                continue;
            }

            if (ch == ';')
            {
                CloseField();
                index++;
                continue;
            }

            if (ch == '\r' || ch == '\n')
            {
                var isCrLf = ch == '\r' && index + 1 < source.Length && source[index + 1] == '\n';
                CloseRecord();
                line++;
                recordStartLine = line;
                index += isCrLf ? 2 : 1;
                continue;
            }

            current.Append(ch);
            index++;
        }

        if (current.Length > 0 || fields.Count > 0)
        {
            CloseRecord();
        }

        return records;
    }
}
