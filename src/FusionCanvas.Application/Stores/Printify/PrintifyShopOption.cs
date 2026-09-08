namespace FusionCanvas.Application.Stores.Printify;

public sealed record PrintifyShopOption(int Id, string Title)
{
    public string DisplayName => $"{Title} ({Id})";
}
