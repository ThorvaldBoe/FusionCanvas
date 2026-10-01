using System.Text;
using FusionCanvas.Application.Snowclones;

namespace FusionCanvas.Integration.Snowclones;

public sealed class SnowcloneCsvCodec : ISnowcloneCsvCodec
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private const int ReadBufferSize = 1024;

    public async Task<SnowcloneCsvReadResult> ReadAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var parser = new AsyncCsvRecordReader(stream);
            var header = await parser.ReadRecordAsync(cancellationToken).ConfigureAwait(false);
            if (header is null)
            {
                return SnowcloneCsvReadResult.Failure(
                    "CSV must begin with the exact header Phrase,Guidance.");
            }

            if (header.Fields is not [var first, var second] ||
                first != "Phrase" ||
                second != "Guidance")
            {
                return SnowcloneCsvReadResult.Failure(
                    "CSV must contain exactly the headers Phrase,Guidance in that order.");
            }

            var rows = new List<SnowcloneCsvRow>();
            while (await parser.ReadRecordAsync(cancellationToken).ConfigureAwait(false) is { } record)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (record.Fields is not [var phrase, var guidance])
                {
                    return SnowcloneCsvReadResult.Failure(
                        $"Row {record.LineNumber} must contain exactly Phrase and Guidance.");
                }

                rows.Add(new SnowcloneCsvRow(phrase, guidance, record.LineNumber));
            }

            return SnowcloneCsvReadResult.Success(rows);
        }
        catch (MalformedCsvException ex)
        {
            return SnowcloneCsvReadResult.Failure($"Row {ex.LineNumber} contains malformed CSV.");
        }
        catch (DecoderFallbackException)
        {
            return SnowcloneCsvReadResult.Failure("CSV must be valid UTF-8 text.");
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

    private sealed class AsyncCsvRecordReader : IDisposable
    {
        private readonly StreamReader _reader;
        private readonly char[] _buffer = new char[ReadBufferSize];
        private int _bufferIndex;
        private int _bufferLength;
        private int _lineNumber = 1;
        private bool _lastCharacterWasCarriageReturn;
        private int? _pendingCharacter;

        public AsyncCsvRecordReader(Stream stream)
        {
            _reader = new StreamReader(
                stream,
                StrictUtf8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: ReadBufferSize,
                leaveOpen: true);
        }

        public async ValueTask<CsvRecord?> ReadRecordAsync(CancellationToken cancellationToken)
        {
            var fields = new List<string>();
            var field = new StringBuilder();
            var fieldStarted = false;
            var inQuotes = false;
            var afterClosingQuote = false;
            var hasInput = false;
            var recordLineNumber = _lineNumber;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var value = await ReadCharacterAsync(cancellationToken).ConfigureAwait(false);
                if (value < 0)
                {
                    if (!hasInput && fields.Count == 0 && field.Length == 0)
                    {
                        return null;
                    }

                    if (inQuotes)
                    {
                        throw new MalformedCsvException(recordLineNumber);
                    }

                    fields.Add(field.ToString());
                    return new CsvRecord(fields, recordLineNumber);
                }

                hasInput = true;
                var character = (char)value;

                if (inQuotes)
                {
                    if (character == '"')
                    {
                        inQuotes = false;
                        afterClosingQuote = true;
                    }
                    else
                    {
                        field.Append(character);
                        AdvanceLineNumber(character);
                    }

                    continue;
                }

                if (afterClosingQuote)
                {
                    if (character == '"')
                    {
                        field.Append('"');
                        inQuotes = true;
                        afterClosingQuote = false;
                    }
                    else if (character == ',')
                    {
                        fields.Add(field.ToString());
                        field.Clear();
                        fieldStarted = false;
                        afterClosingQuote = false;
                    }
                    else if (IsLineBreak(character))
                    {
                        fields.Add(field.ToString());
                        await CompleteRecordAsync(character, cancellationToken).ConfigureAwait(false);
                        return new CsvRecord(fields, recordLineNumber);
                    }
                    else
                    {
                        throw new MalformedCsvException(recordLineNumber);
                    }

                    continue;
                }

                if (character == '"' && !fieldStarted)
                {
                    fieldStarted = true;
                    inQuotes = true;
                }
                else if (character == ',')
                {
                    fields.Add(field.ToString());
                    field.Clear();
                    fieldStarted = false;
                }
                else if (IsLineBreak(character))
                {
                    fields.Add(field.ToString());
                    await CompleteRecordAsync(character, cancellationToken).ConfigureAwait(false);
                    return new CsvRecord(fields, recordLineNumber);
                }
                else
                {
                    fieldStarted = true;
                    field.Append(character);
                }
            }
        }

        public void Dispose() => _reader.Dispose();

        private async ValueTask<int> ReadCharacterAsync(CancellationToken cancellationToken)
        {
            if (_pendingCharacter is { } pendingCharacter)
            {
                _pendingCharacter = null;
                return pendingCharacter;
            }

            if (_bufferIndex >= _bufferLength)
            {
                _bufferLength = await _reader.ReadAsync(_buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                _bufferIndex = 0;
                if (_bufferLength == 0)
                {
                    return -1;
                }
            }

            return _buffer[_bufferIndex++];
        }

        private async ValueTask CompleteRecordAsync(
            char lineBreak,
            CancellationToken cancellationToken)
        {
            if (lineBreak == '\r')
            {
                AdvanceLineNumber(lineBreak);
                var next = await ReadCharacterAsync(cancellationToken).ConfigureAwait(false);
                if (next >= 0 && next != '\n')
                {
                    _pendingCharacter = next;
                }

                return;
            }

            AdvanceLineNumber(lineBreak);
        }

        private void AdvanceLineNumber(char character)
        {
            if (character == '\r')
            {
                _lineNumber = checked(_lineNumber + 1);
                _lastCharacterWasCarriageReturn = true;
            }
            else if (character == '\n')
            {
                if (!_lastCharacterWasCarriageReturn)
                {
                    _lineNumber = checked(_lineNumber + 1);
                }

                _lastCharacterWasCarriageReturn = false;
            }
            else
            {
                _lastCharacterWasCarriageReturn = false;
            }
        }

        private static bool IsLineBreak(char character) => character is '\r' or '\n';
    }

    private sealed record CsvRecord(IReadOnlyList<string> Fields, int LineNumber);

    private sealed class MalformedCsvException(int lineNumber) : Exception
    {
        public int LineNumber { get; } = lineNumber;
    }
}
