using FusionCanvas.Application.Ideation;
using FusionCanvas.Domain.Ideation;

namespace FusionCanvas.Integration.Ideation;

public sealed class FakeIdeaGenerator : IIdeaGenerator
{
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    public FakeIdeaGenerator(Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _delay = delay ?? Task.Delay;
    }

    public async Task<string> GenerateAsync(
        IdeationGenerationContext context,
        int requestIndex,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        await _delay(TimeSpan.FromMilliseconds(90 + (requestIndex % 4 * 25)), cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return context.Mode switch
        {
            IdeationMode.Basic => Basic(context, requestIndex),
            IdeationMode.Snowclones => Snowclone(context, requestIndex),
            _ => throw new ArgumentOutOfRangeException(nameof(context), "Unsupported Ideation mode.")
        };
    }

    private static string Basic(IdeationGenerationContext context, int index)
    {
        var subject = Subject(context);
        var guidance = string.IsNullOrWhiteSpace(context.Guidance) ? null : context.Guidance!.Trim().ToLowerInvariant();
        var direction = guidance is null ? subject : $"{guidance} {subject}";
        return (index % 6) switch
        {
            0 => $"A playful illustration of {direction}.",
            1 => $"{Capitalize(direction)} with a short, funny phrase.",
            2 => $"A retro-style drawing of {direction}.",
            3 => $"{Capitalize(direction)} acting like it owns the place.",
            4 => $"A minimal typographic idea about {direction}.",
            _ => $"{Capitalize(direction)} in an unexpectedly dramatic situation."
        };
    }

    private static string Snowclone(IdeationGenerationContext context, int index)
    {
        var template = context.SnowcloneTemplate
            ?? throw new InvalidOperationException("Snowclones mode requires a template.");
        var subject = SubjectVariation(context, index);
        var action = string.IsNullOrWhiteSpace(context.Guidance)
            ? context.Niche.Name.ToLowerInvariant()
            : context.Guidance!.Trim().ToLowerInvariant();
        var result = template
            .Replace("X", subject, StringComparison.Ordinal)
            .Replace("Y", action, StringComparison.Ordinal)
            .Replace("Z", context.Store.Name.ToLowerInvariant(), StringComparison.Ordinal);
        return Capitalize(result.Trim()) + (result.TrimEnd().EndsWith('.') ? string.Empty : ".");
    }

    private static string Subject(IdeationGenerationContext context) =>
        (context.Group?.Name ?? context.Niche.Name).Trim().ToLowerInvariant();

    private static string SubjectVariation(IdeationGenerationContext context, int index)
    {
        var subject = Subject(context);
        var guidance = string.IsNullOrWhiteSpace(context.Guidance)
            ? null
            : context.Guidance!.Trim().ToLowerInvariant();
        return (index % 3) switch
        {
            0 when guidance is not null => $"{guidance} {subject}",
            1 when guidance is not null => $"{subject} with {guidance} attitude",
            2 when guidance is not null => $"{guidance} {subject} energy",
            1 => $"{subject} lovers",
            2 => $"{subject} life",
            _ => subject
        };
    }

    private static string Capitalize(string value) =>
        value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];
}
