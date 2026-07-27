using FusionCanvas.Application.Ideation;

namespace FusionCanvas.Integration.Ideation;

public sealed class InMemorySnowcloneCatalog : ISnowcloneCatalog
{
    private static readonly string[] Templates =
    [
        "Talk to me about X",
        "Whatever X your Y",
        "Keep calm and X",
        "I came, I saw, I X",
        "X is my happy place",
        "You had me at X",
        "All I need is X",
        "Home is where the X is",
        "Powered by X",
        "Born to X, forced to Y",
        "This is my X face",
        "Less Y, more X"
    ];

    private readonly Random _random;

    public InMemorySnowcloneCatalog(Random? random = null)
    {
        _random = random ?? Random.Shared;
    }

    public Task<SnowcloneCatalogResult> GetSelectionsAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        var selected = new List<IdeationSnowcloneSelection>(count);
        while (selected.Count < count)
        {
            var cycle = Templates.ToArray();
            Shuffle(cycle);
            selected.AddRange(cycle
                .Take(count - selected.Count)
                .Select(template => new IdeationSnowcloneSelection(
                    Guid.NewGuid(),
                    template,
                    "Replace X, Y, and Z using the creative context.",
                    [])));
        }

        return Task.FromResult(SnowcloneCatalogResult.Success(selected));
    }

    private void Shuffle(string[] values)
    {
        for (var index = values.Length - 1; index > 0; index--)
        {
            var swap = _random.Next(index + 1);
            (values[index], values[swap]) = (values[swap], values[index]);
        }
    }
}
