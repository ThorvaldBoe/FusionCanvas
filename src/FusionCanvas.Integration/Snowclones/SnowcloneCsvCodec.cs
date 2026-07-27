using System.Text;
using FusionCanvas.Application.Snowclones;
using Microsoft.VisualBasic.FileIO;

namespace FusionCanvas.Integration.Snowclones;

public sealed class SnowcloneCsvCodec : ISnowcloneCsvCodec
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public Task<SnowcloneCsvReadResult> ReadAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var parser = new TextFieldParser(
                stream,
                StrictUtf8,
                detectEncoding: true,
                leaveOpen: true)
            {
                HasFieldsEnclosedInQuotes = true,
                TrimWhiteSpace = false
            };
            parser.SetDelimiters(",");

            if (parser.EndOfData)
            {
                return Task.FromResult(SnowcloneCsvReadResult.Failure(
                    "CSV must begin with the exact header Phrase,Guidance."));
            }

            var headers = parser.ReadFields();
            if (headers is not [var first, var second] ||
                first != "Phrase" ||
                second != "Guidance")
            {
                return Task.FromResult(SnowcloneCsvReadResult.Failure(
                    "CSV must contain exactly the headers Phrase,Guidance in that order."));
            }

            var rows = new List<SnowcloneCsvRow>();
            while (!parser.EndOfData)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rowNumber = checked((int)parser.LineNumber);
                var fields = parser.ReadFields();
                if (fields is not [var phrase, var guidance])
                {
                    return Task.FromResult(SnowcloneCsvReadResult.Failure(
                        $"Row {rowNumber} must contain exactly Phrase and Guidance."));
                }

                rows.Add(new SnowcloneCsvRow(phrase, guidance, rowNumber));
            }

            return Task.FromResult(SnowcloneCsvReadResult.Success(rows));
        }
        catch (MalformedLineException ex)
        {
            return Task.FromResult(SnowcloneCsvReadResult.Failure(
                $"Row {ex.LineNumber} contains malformed CSV."));
        }
        catch (DecoderFallbackException)
        {
            return Task.FromResult(SnowcloneCsvReadResult.Failure(
                "CSV must be valid UTF-8 text."));
        }
    }

    public async Task WriteAsync(
        Stream stream,
        IReadOnlyList<SnowcloneCsvRow> rows,
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
        await writer.WriteLineAsync("Phrase,Guidance".AsMemory(), cancellationToken);
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = $"{Escape(row.Phrase)},{Escape(row.Guidance)}";
            await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
        }

        await writer.FlushAsync(cancellationToken);
    }

    private static string Escape(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.IndexOfAny([',', '"', '\r', '\n']) < 0)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
