namespace FusionCanvas.Application.Items;

public sealed record ItemCreationDestinationResult(ItemTopicReference? Topic, string? Error)
{
    public bool Succeeded => Topic is not null;
}
