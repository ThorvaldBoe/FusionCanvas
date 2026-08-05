using System.Text;
using FusionCanvas.Application.Items;

namespace FusionCanvas.Integration.Items;

public sealed class ItemCsvCodec : IItemCsvCodec
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private const string Header = "Title;Base Idea;Concept Idea;Phrase;Graphic;Notes;Tags";

    public async Task WriteAsync(
        Stream stream,
        IReadOnlyList<ItemCsvRow> rows,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(rows);

        await using var writer = new StreamWriter(
            stream,
            StrictUtf8,
            bufferSize: 1024,
            leaveOpen: true)
        {
            NewLine = "\r\n"
        };

        cancellationToken.ThrowIfCancellationRequested();
        await writer.WriteLineAsync(Header.AsMemory(), cancellationToken);
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = $"{Escape(row.Title)};{Escape(row.BaseIdea)};{Escape(row.ConceptIdea)};{Escape(row.Phrase)};{Escape(row.Graphic)};{Escape(row.Notes)};{Escape(row.Tags)}";
            await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
        }

        await writer.FlushAsync(cancellationToken);
    }

    private static string Escape(string? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value.IndexOfAny([';', '"', '\r', '\n']) < 0)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
