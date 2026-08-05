using FusionCanvas.Domain.Concepts;
using FusionCanvas.Integration.SllGeneration;

namespace FusionCanvas.Integration.Tests.SllGeneration;

public sealed class SllDocumentCodecTests
{
    private const string SuppliedPhrase = "LIVE EVERY MOMENT";
    private readonly SllDocumentCodec _codec = new();

    private static SllDocument Sample(string? asciiSketch = "  +-------+\n  | TEXT  |\n  +-------+", string? phrase = null)
        => new(
            ["Assumes a warm tone"],
            new SllCommunication("I am carefree", "Sees a travel lover", "warm", "beach-goers"),
            new SllTriangle(
                "A person enjoying a quiet sunrise",
                phrase ?? SuppliedPhrase,
                "a simplified sun with soft rays",
                "reinforcement",
                null),
            asciiSketch!,
            new SllNotes("statement stack", "bold sans", "flat", "warm bone", "none", "centered 28cm", "legible at thumb"),
            new SllValidation("text first, graphic second", "TEXT anchor", "wearer signal clear", "long phrase"));

    [Fact]
    public void Serialize_RoundTrips_PreservesFields()
    {
        var original = Sample();

        var json = _codec.Serialize(original);
        var ok = _codec.TryDeserialize(json, out var parsed);

        Assert.True(ok);
        Assert.NotNull(parsed);
        Assert.Equal(original.Assumptions, parsed!.Assumptions);
        Assert.Equal(original.Communication.WearerSignal, parsed.Communication.WearerSignal);
        Assert.Equal(original.Triangle.Phrase, parsed.Triangle.Phrase);
        Assert.Equal(original.AsciiSketch, parsed.AsciiSketch);
        Assert.Equal(original.Notes.Typography, parsed.Notes.Typography);
        Assert.Equal(original.Validation.LargestRisk, parsed.Validation.LargestRisk);
    }

    [Fact]
    public void TryDeserialize_InvalidJson_ReturnsFalseWithoutThrowing()
    {
        var ok = _codec.TryDeserialize("not json", out var parsed);

        Assert.False(ok);
        Assert.Null(parsed);
    }

    [Fact]
    public void TryDeserialize_NullInput_ReturnsFalseWithoutThrowing()
    {
        var ok = _codec.TryDeserialize("null", out var parsed);

        Assert.False(ok);
        Assert.Null(parsed);
    }
}
