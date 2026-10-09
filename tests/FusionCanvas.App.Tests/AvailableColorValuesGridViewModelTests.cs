using FusionCanvas.App.Stores;
using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.App.Tests;

public sealed class AvailableColorValuesGridViewModelTests
{
    private static readonly Guid OfferingId = Guid.Parse("713526f8-b4e4-4d2f-a3ad-a1c0b7b7b002");
    private static readonly Guid OptionId = Guid.Parse("cdd26d1d-0ec6-4bb8-960f-2f770a6dd87c");

    [Fact]
    public void Defaults_to_configured_order_and_excludes_archived_values()
    {
        var model = CreateModel([
            Value("White", 2),
            Value("Black", 0),
            Value("Heather", 1),
            Value("Archived", 3, archived: true)
        ]);

        Assert.Equal(["Black", "Heather", "White"], model.Rows.Select(row => row.ColorName));
        Assert.Equal("Configured order", model.SortOption);
        Assert.Equal("Showing 1–3 of 3 colors", model.PageSummary);
    }

    [Fact]
    public void Search_filters_the_complete_set_case_insensitively_and_returns_to_page_one()
    {
        var model = CreateModel(Enumerable.Range(0, 35)
            .Select(index => Value(index == 33 ? "Heather Blue" : $"Color {index:00}", index)));
        model.PageSize = 10;
        model.NextPageCommand.Execute(null);
        model.NextPageCommand.Execute(null);
        Assert.Equal(3, model.CurrentPage);

        model.SearchText = "  hEaThEr  ";

        Assert.Equal(1, model.CurrentPage);
        Assert.Equal(["Heather Blue"], model.Rows.Select(row => row.ColorName));
        Assert.Equal("Showing 1–1 of 1 colors", model.PageSummary);

        model.SearchText = string.Empty;
        Assert.Equal(35, model.MatchingColorCount);
        Assert.Equal("Color 00", model.Rows.First().ColorName);
    }

    [Theory]
    [InlineData(10, 6)]
    [InlineData(25, 3)]
    [InlineData(50, 2)]
    public void Page_sizes_bound_rows_and_report_ceiling_page_count(int pageSize, int pageCount)
    {
        var model = CreateModel(Enumerable.Range(0, 51).Select(index => Value($"Color {index:00}", index)));
        model.PageSize = pageSize;

        Assert.Equal(pageCount, model.PageCount);
        Assert.Equal(Math.Min(pageSize, 51), model.Rows.Count);
        Assert.Equal(1 < pageCount, model.NextPageCommand.CanExecute(null));

        model.NextPageCommand.Execute(null);
        Assert.Equal(2, model.CurrentPage);
        Assert.Equal(Math.Min(2 * pageSize, 51), (model.CurrentPage - 1) * pageSize + model.Rows.Count);
        Assert.Equal(2 < pageCount, model.NextPageCommand.CanExecute(null));
    }

    [Fact]
    public void Sort_modes_are_global_stable_and_configured_order_can_be_restored()
    {
        var model = CreateModel([
            Value("Navy", 0),
            Value("Black", 1),
            Value("black", 2),
            Value("White", 3)
        ]);
        model.SortOption = "Name A–Z";
        Assert.Equal(["Black", "black", "Navy", "White"], model.Rows.Select(row => row.ColorName));

        model.SortOption = "Name Z–A";
        Assert.Equal(["White", "Navy", "Black", "black"], model.Rows.Select(row => row.ColorName));

        model.SortOption = "Configured order";
        Assert.Equal(["Navy", "Black", "black", "White"], model.Rows.Select(row => row.ColorName));

        var pagedModel = CreateModel(Enumerable.Range(0, 35)
            .Reverse()
            .Select(index => Value($"Color {index:00}", 34 - index)));
        pagedModel.SortOption = "Name A–Z";
        Assert.Equal(4, pagedModel.PageCount);
        Assert.Equal(Enumerable.Range(0, 10).Select(index => $"Color {index:00}"), pagedModel.Rows.Select(row => row.ColorName));
        pagedModel.NextPageCommand.Execute(null);
        Assert.Equal(Enumerable.Range(10, 10).Select(index => $"Color {index:00}"), pagedModel.Rows.Select(row => row.ColorName));
    }

    [Fact]
    public void Refresh_preserves_same_offering_preferences_clamps_page_and_context_switch_resets_them()
    {
        var model = CreateModel(Enumerable.Range(0, 30).Select(index => Value($"Color {index:00}", index)));
        model.PageSize = 10;
        model.SortOption = "Name Z–A";
        model.SearchText = "Color";
        model.NextPageCommand.Execute(null);
        model.NextPageCommand.Execute(null);

        model.SetValues(OfferingId, Enumerable.Range(0, 12).Select(index => Value($"Color {index:00}", index)));
        Assert.Equal(2, model.CurrentPage);
        Assert.Equal("Name Z–A", model.SortOption);
        Assert.Equal("Color", model.SearchText);
        Assert.Equal("Color 01", model.Rows.First().ColorName);
        Assert.Equal("Color 00", model.Rows.Last().ColorName);

        model.SetValues(Guid.NewGuid(), [Value("New Offering Color", 0)]);
        Assert.Equal(1, model.CurrentPage);
        Assert.Equal(10, model.PageSize);
        Assert.Equal("Configured order", model.SortOption);
        Assert.Equal(string.Empty, model.SearchText);
        Assert.Equal(["New Offering Color"], model.Rows.Select(row => row.ColorName));
    }

    [Fact]
    public void Empty_search_result_is_explicit_and_alternation_continues_across_pages()
    {
        var model = CreateModel(Enumerable.Range(0, 11).Select(index => Value($"Color {index:00}", index)));
        Assert.False(model.Rows.First().IsAlternate);
        Assert.True(model.Rows.Last().IsAlternate);

        model.NextPageCommand.Execute(null);
        Assert.False(Assert.Single(model.Rows).IsAlternate);

        model.SearchText = "missing";
        Assert.True(model.HasNoSearchResults);
        Assert.Equal("Showing 0 colors", model.PageSummary);
        Assert.Empty(model.Rows);
        Assert.False(model.NextPageCommand.CanExecute(null));
    }

    private static AvailableColorValuesGridViewModel CreateModel(IEnumerable<OfferingOptionValue> values)
    {
        var model = new AvailableColorValuesGridViewModel();
        model.SetValues(OfferingId, values);
        return model;
    }

    private static OfferingOptionValue Value(string name, int sortOrder, bool archived = false) =>
        new(Guid.NewGuid(), OptionId, OfferingId, name, sortOrder, archived);
}
