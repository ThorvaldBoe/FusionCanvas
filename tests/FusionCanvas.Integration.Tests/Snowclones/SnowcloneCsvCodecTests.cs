using System.Text;
using FusionCanvas.Application.Snowclones;
using FusionCanvas.Integration.Snowclones;

namespace FusionCanvas.Integration.Tests.Snowclones;

public sealed class SnowcloneCsvCodecTests
{
    private readonly SnowcloneCsvCodec _codec = new();

    [Fact]
    public async Task ReadAsync_ParsesQuotedCommaQuoteAndMultilineGuidance()
    {
        const string csv =
            "Phrase,Guidance\r\n\"The \"\"best\"\" {X}\",\"First line,\r\nsecond line.\"\r\n";

        var result = await ReadAsync(csv);

        Assert.True(result.Succeeded);
        var row = Assert.Single(result.Rows);
        Assert.Equal("The \"best\" {X}", row.Phrase);
        Assert.Equal("First line,\r\nsecond line.", row.Guidance);
        Assert.Equal(2, row.RowNumber);
    }

    [Theory]
    [InlineData("Guidance,Phrase\nA,B")]
    [InlineData("phrase,Guidance\nA,B")]
    [InlineData("Phrase,Guidance,Extra\nA,B,C")]
    [InlineData("Phrase\nA")]
    [InlineData("")]
    public async Task ReadAsync_RejectsNonExactHeader(string csv)
    {
        var result = await ReadAsync(csv);

        Assert.False(result.Succeeded);
        Assert.Contains("Phrase,Guidance", result.Error);
    }

    [Fact]
    public async Task ReadAsync_AcceptsHeaderOnlyAsEmptyValidDocument()
    {
        var result = await ReadAsync("Phrase,Guidance\r\n");

        Assert.True(result.Succeeded);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public async Task ReadAsync_RejectsMalformedQuotedRow()
    {
        var result = await ReadAsync("Phrase,Guidance\r\n\"Broken {X},Guidance\r\n");

        Assert.False(result.Succeeded);
        Assert.Contains("malformed CSV", result.Error);
    }

    [Fact]
    public async Task ReadAsync_RejectsInvalidUtfEight()
    {
        await using var stream = new MemoryStream(
            [0x50, 0x68, 0x72, 0x61, 0x73, 0x65, 0x2C, 0x47, 0x75, 0x69, 0x64, 0x61, 0x6E, 0x63, 0x65, 0x0A, 0xFF]);

        var result = await _codec.ReadAsync(stream, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("UTF-8", result.Error);
    }

    [Fact]
    public async Task WriteAsync_UsesExactHeaderCrLfAndRoundTrips()
    {
        var rows = new[]
        {
            new SnowcloneCsvRow("Alpha {X}", "Plain", 0),
            new SnowcloneCsvRow("The \"best\" {Y}", "First,\nsecond", 0)
        };
        await using var stream = new MemoryStream();

        await _codec.WriteAsync(stream, rows, TestContext.Current.CancellationToken);
        var text = Encoding.UTF8.GetString(stream.ToArray());
        stream.Position = 0;
        var reloaded = await _codec.ReadAsync(stream, TestContext.Current.CancellationToken);

        Assert.StartsWith("Phrase,Guidance\r\n", text);
        Assert.Contains("\"The \"\"best\"\" {Y}\",\"First,\nsecond\"\r\n", text);
        Assert.True(reloaded.Succeeded);
        Assert.Equal(rows.Select(row => (row.Phrase, row.Guidance)),
            reloaded.Rows.Select(row => (row.Phrase, row.Guidance)));
    }

    [Fact]
    public async Task ReadAsync_AcceptsUtfEightBomAndLf()
    {
        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes("Phrase,Guidance\nBom {X},Works\n");
        await using var stream = new MemoryStream([.. preamble, .. body]);

        var result = await _codec.ReadAsync(stream, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("Bom {X}", Assert.Single(result.Rows).Phrase);
    }

    [Fact]
    public async Task ReadAndWriteAsync_HonorCancellation()
    {
        await using var readStream = new MemoryStream(Encoding.UTF8.GetBytes("Phrase,Guidance\n"));
        await using var writeStream = new MemoryStream();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _codec.ReadAsync(readStream, cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _codec.WriteAsync(writeStream, [], cancellation.Token));
    }

    [Fact]
    public async Task EmbeddedStarterResource_UsesTheNormalCsvContract()
    {
        var source = new EmbeddedBundledSnowcloneSource();
        await using var stream = await source.OpenReadAsync(TestContext.Current.CancellationToken);

        var result = await _codec.ReadAsync(stream, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var row = Assert.Single(result.Rows);
        Assert.Equal("Easily distracted by {X}", row.Phrase);
        Assert.Contains("Replace {X}", row.Guidance);
    }

    private async Task<SnowcloneCsvReadResult> ReadAsync(string csv)
    {
        await using var stream = new MemoryStream(new UTF8Encoding(false).GetBytes(csv));
        return await _codec.ReadAsync(stream, TestContext.Current.CancellationToken);
    }
}
