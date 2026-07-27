using FusionCanvas.Application.Snowclones;
using FusionCanvas.Domain.Snowclones;

namespace FusionCanvas.Application.Ideation;

public sealed class PersistedSnowcloneCatalog(
    ISnowcloneLibraryService library,
    Random? random = null) : ISnowcloneCatalog
{
    private readonly ISnowcloneLibraryService _library =
        library ?? throw new ArgumentNullException(nameof(library));
    private readonly Random _random = random ?? Random.Shared;

    public async Task<SnowcloneCatalogResult> GetSelectionsAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (count == 0)
        {
            return SnowcloneCatalogResult.Success([]);
        }

        var loaded = await _library.LoadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!loaded.Succeeded)
        {
            return SnowcloneCatalogResult.Failure(
                loaded.Error ?? "The Snowclone Library could not be loaded.");
        }

        var confirmed = loaded.State.AllSnowclones
            .Select(summary =>
            {
                var validation = SnowcloneTemplatePolicy.Validate(summary.Phrase, summary.Guidance);
                return validation.IsValid
                    ? new IdeationSnowcloneSelection(
                        summary.Id,
                        validation.Phrase,
                        validation.Guidance,
                        validation.PlaceholderTokens)
                    : null;
            })
            .Where(selection => selection is not null)
            .Cast<IdeationSnowcloneSelection>()
            .ToArray();
        if (confirmed.Length == 0)
        {
            return SnowcloneCatalogResult.Failure(
                "Add or import at least one Snowclone before generating in Snowclones mode.");
        }

        var selected = new List<IdeationSnowcloneSelection>(count);
        while (selected.Count < count)
        {
            var cycle = confirmed.ToArray();
            Shuffle(cycle);
            selected.AddRange(cycle.Take(count - selected.Count));
        }

        return SnowcloneCatalogResult.Success(selected);
    }

    private void Shuffle(IdeationSnowcloneSelection[] values)
    {
        for (var index = values.Length - 1; index > 0; index--)
        {
            var swap = _random.Next(index + 1);
            (values[index], values[swap]) = (values[swap], values[index]);
        }
    }
}
