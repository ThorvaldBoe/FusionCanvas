namespace FusionCanvas.Application.AI;

public sealed record AiTextMessage
{
    public AiTextMessage(AiMessageRole role, string text, IReadOnlyList<AiImageInput>? images = null)
    {
        Role = role;
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Images = images ?? [];
    }

    public AiMessageRole Role { get; }

    public string Text { get; }

    public IReadOnlyList<AiImageInput> Images { get; }
}
