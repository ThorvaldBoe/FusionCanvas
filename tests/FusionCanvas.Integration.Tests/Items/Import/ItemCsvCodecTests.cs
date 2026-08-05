using FusionCanvas.Integration.Items.Import;

namespace FusionCanvas.Integration.Tests.Items.Import;

public sealed class ItemCsvCodecTests
{
    private readonly ItemCsvCodec _codec = new();

    [Fact]
    public void Parse_ParsesValidRowsAndSplitsTags()
    {
        var result = _codec.Parse(
            "Retro tee;Coffee pun;Retro vibe;Coffee time;Retro cup;Notes text;funny,caffeine\r\n" +
            "Summer tee;Summer slogan;Beach vibe;Summer in the city;Sun graphic;Summer notes;summer,fresh\r\n");

        Assert.Empty(result.Errors);
        var rows = result.Rows;
        Assert.Equal(2, rows.Count);

        var first = rows[0];
        Assert.Equal("Retro tee", first.Title);
        Assert.Equal("Coffee pun", first.BaseIdea);
        Assert.Equal("Retro vibe", first.ConceptIdea);
        Assert.Equal("Coffee time", first.Phrase);
        Assert.Equal("Retro cup", first.Graphic);
        Assert.Equal("Notes text", first.Notes);
        Assert.Equal(["funny", "caffeine"], first.Tags);
        Assert.Equal(1, first.LineNumber);

        var second = rows[1];
        Assert.Equal("Summer tee", second.Title);
        Assert.Equal("Summer slogan", second.BaseIdea);
        Assert.Equal("Beach vibe", second.ConceptIdea);
        Assert.Equal("Summer in the city", second.Phrase);
        Assert.Equal("Sun graphic", second.Graphic);
        Assert.Equal("Summer notes", second.Notes);
        Assert.Equal(["summer", "fresh"], second.Tags);
        Assert.Equal(2, second.LineNumber);
    }

    [Fact]
    public void Parse_AcceptsHeaderAndSkipsIt()
    {
        var result = _codec.Parse(
            "Title;Base Idea;Concept Idea;Phrase;Graphic;Notes;Tags\n" +
            "First;Coffee;Concept;Drink;Graphic;Notes;fun\n");

        Assert.Empty(result.Errors);
        var row = Assert.Single(result.Rows);
        Assert.Equal("First", row.Title);
        Assert.Equal(2, row.LineNumber);
    }

    [Fact]
    public void Parse_HeaderDetectionIsCaseInsensitive()
    {
        var result = _codec.Parse(
            "title;base idea;CONCEPT IDEA;phrase;graphic;notes;tags\n" +
            "First;A;B;C;D;E;f,g\n");

        Assert.Empty(result.Errors);
        var row = Assert.Single(result.Rows);
        Assert.Equal("First", row.Title);
    }

    [Fact]
    public void Parse_HeaderOnlyProducesZeroRowsWithoutErrors()
    {
        var result = _codec.Parse("Title;Base Idea;Concept Idea;Phrase;Graphic;Notes;Tags\r\n");

        Assert.Empty(result.Errors);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public void Parse_DecodesDoubleSemicolonsAsLiteralSemicolons()
    {
        var result = _codec.Parse(
            "Title;Base ;; Idea;Concept;Phrase;Graphic;Notes;Tags\n");

        Assert.Empty(result.Errors);
        var row = Assert.Single(result.Rows);
        Assert.Equal("Base ; Idea", row.BaseIdea);
    }

    [Fact]
    public void Parse_WrongColumnCountReportsErrorOnLine()
    {
        var result = _codec.Parse(
            "One;Two;Three;Four;Five;Six\n" +
            "One;Two;Three;Four;Five;Six;Seven;Eight\n");

        Assert.True(result.HasErrors);
        Assert.Equal(2, result.Errors.Count);
        Assert.Equal([1, 2], result.Errors.Select(error => error.LineNumber).ToArray());
        Assert.Equal(["Error on line 1", "Error on line 2"], result.ErrorText);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public void Parse_BlankTitleExcludesRowAndReportsError()
    {
        var result = _codec.Parse(
            ";Base;Concept;Phrase;Graphic;Notes;Tags\n" +
            "Valid;Base;Concept;Phrase;Graphic;Notes;Tag\n");

        Assert.True(result.HasErrors);
        var error = Assert.Single(result.Errors);
        Assert.Equal(1, error.LineNumber);
        Assert.Contains("Title", error.Message);

        var row = Assert.Single(result.Rows);
        Assert.Equal("Valid", row.Title);
    }

    [Fact]
    public void Parse_AllowsEmptyFinalField()
    {
        var result = _codec.Parse("Title;Base;Concept;Phrase;Graphic;Notes;\n");

        Assert.Empty(result.Errors);
        var row = Assert.Single(result.Rows);
        Assert.Equal("Notes", row.Notes);
        Assert.Empty(row.Tags);
    }

    [Fact]
    public void Parse_EmptyLineInMiddleReportsError()
    {
        var result = _codec.Parse(
            "One;Base;Concept;Phrase;Graphic;Notes;Tag\n" +
            "\n" +
            "Two;Base;Concept;Phrase;Graphic;Notes;Tag\n");

        Assert.Equal(["Error on line 2"], result.ErrorText);
        Assert.Equal(2, result.Rows.Count);
    }

    [Fact]
    public void Parse_EmptySourceProducesZeroRowsWithoutErrors()
    {
        var result = _codec.Parse(string.Empty);

        Assert.Empty(result.Errors);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public void Parse_DataRowEqualToHeadingsOnFirstLineIsTreatedAsHeader()
    {
        var result = _codec.Parse(
            "Title;Base Idea;Concept Idea;Phrase;Graphic;Notes;Tags\n" +
            "Second;Base;Concept;Phrase;Graphic;Notes;Tag\n");

        var row = Assert.Single(result.Rows);
        Assert.Equal("Second", row.Title);
    }

    [Fact]
    public void Parse_HeaderDetectionOnlyAppliesToFirstLine_SecondLineHeadingsImported()
    {
        var result = _codec.Parse(
            "Alpha;Base;Concept;Phrase;Graphic;Notes;Tag\n" +
            "Title;Base Idea;Concept Idea;Phrase;Graphic;Notes;Tags\n");

        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(["Alpha", "Title"], result.Rows.Select(row => row.Title).ToArray());
    }

    [Fact]
    public void WriteSample_IsValidAndDemonstratesEscapingAndTags()
    {
        var sample = _codec.WriteSample();

        Assert.StartsWith("Title;Base Idea;Concept Idea;Phrase;Graphic;Notes;Tags", sample);

        var parsed = _codec.Parse(sample);
        Assert.Empty(parsed.Errors);
        Assert.Equal(2, parsed.Rows.Count);
        Assert.Contains(parsed.Rows, row => row.Notes is not null && row.Notes.Contains(';'));
        Assert.All(parsed.Rows, row => Assert.Contains(row.Tags, tag => tag.Length > 0));
    }
}
