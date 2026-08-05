using System.Text;
using FusionCanvas.Application.Items;
using FusionCanvas.Integration.Items;

namespace FusionCanvas.Integration.Tests.Items;

public sealed class ItemCsvCodecTests
{
    private const string Header = "Title;Base Idea;Concept Idea;Phrase;Graphic;Notes;Tags";
    private readonly ItemCsvCodec _codec = new();

    [Fact]
    public async Task WriteAsync_UsesExactHeaderAndCrLf()
    {
        await using var stream = new MemoryStream();

        await _codec.WriteAsync(stream, [], TestContext.Current.CancellationToken);

        var text = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal(Header + "\r\n", text);
    }

    [Fact]
    public async Task WriteAsync_EmitsRowsInOrder()
    {
        await using var stream = new MemoryStream();

        await _codec.WriteAsync(stream, new[]
        {
            new ItemCsvRow("One", "i", null, null, null, null, string.Empty),
            new ItemCsvRow("Two", "i2", null, null, null, null, string.Empty)
        }, TestContext.Current.CancellationToken);

        var text = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal(
            $"{Header}\r\nOne;i;;;;;\r\nTwo;i2;;;;;\r\n",
            text);
    }

    [Fact]
    public async Task WriteAsync_QuotesFieldContainingSemiColon()
    {
        await using var stream = new MemoryStream();

        await _codec.WriteAsync(stream, new[]
        {
            new ItemCsvRow("Alpha; Beta", null, null, null, null, null, string.Empty)
        }, TestContext.Current.CancellationToken);

        var text = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("\"Alpha; Beta\"", text);
    }

    [Fact]
    public async Task WriteAsync_QuotesFieldContainingEmbeddedQuote()
    {
        await using var stream = new MemoryStream();

        await _codec.WriteAsync(stream, new[]
        {
            new ItemCsvRow("He said \"hi\"", null, null, null, null, null, string.Empty)
        }, TestContext.Current.CancellationToken);

        var text = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("\"He said \"\"hi\"\"\"", text);
    }

    [Fact]
    public async Task WriteAsync_PreservesMultilineFieldWithinOneQuotedField()
    {
        await using var stream = new MemoryStream();

        await _codec.WriteAsync(stream, new[]
        {
            new ItemCsvRow("Title", null, null, null, null, "Line1\nLine2", string.Empty)
        }, TestContext.Current.CancellationToken);

        var text = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("\"Line1\nLine2\"", text);
    }

    [Fact]
    public async Task WriteAsync_NullFieldsWriteAsEmpty()
    {
        await using var stream = new MemoryStream();

        await _codec.WriteAsync(stream, new[]
        {
            new ItemCsvRow("Title", null, null, null, null, null, string.Empty)
        }, TestContext.Current.CancellationToken);

        var text = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"{Header}\r\nTitle;;;;;;\r\n", text);
    }

    [Fact]
    public async Task WriteAsync_WithMidStreamFailureThrowsAndFlushesPartial()
    {
        await using var failing = new ThrowingStream();

        await Assert.ThrowsAsync<IOException>(
            () => _codec.WriteAsync(failing, new[]
            {
                new ItemCsvRow("One", null, null, null, null, null, string.Empty)
            }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task WriteAsync_HonorsCancellation()
    {
        await using var stream = new MemoryStream();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _codec.WriteAsync(stream, [new ItemCsvRow("T", null, null, null, null, null, string.Empty)], cancellation.Token));
    }

    private sealed class ThrowingStream : MemoryStream
    {
        public override void Flush() => throw new IOException("write failed");

        public override Task FlushAsync(CancellationToken cancellationToken) =>
            throw new IOException("write failed");
    }
}
