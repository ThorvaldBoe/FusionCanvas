using FusionCanvas.Application.Ideation;
using FusionCanvas.Domain.Ideation;
using FusionCanvas.Integration.Ideation;

namespace FusionCanvas.Integration.Tests.Ideation;

public sealed class IdeationAdapterTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("  ", false)]
    [InlineData("placeholder", true)]
    public void EnvironmentAccess_UsesPresenceOnly(string? value, bool expected)
    {
        var status = new EnvironmentIdeationAccessStatus(() => value).GetAvailability();

        Assert.Equal(expected, status.IsAvailable);
        if (!string.IsNullOrWhiteSpace(value))
        {
            Assert.DoesNotContain(value, status.UnavailableReason ?? string.Empty, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task SnowcloneCatalog_ExhaustsUniqueTemplatesBeforeRepeating()
    {
        var templates = (await new InMemorySnowcloneCatalog(new Random(7))
            .GetSelectionsAsync(13, TestContext.Current.CancellationToken)).Selections;

        Assert.Equal(12, templates.Take(12).Select(template => template.Phrase).Distinct().Count());
        Assert.Equal(13, templates.Count);
    }

    [Fact]
    public async Task SnowcloneCatalog_UsesDeterministicInjectedOrdering()
    {
        var first = (await new InMemorySnowcloneCatalog(new Random(42))
            .GetSelectionsAsync(20, TestContext.Current.CancellationToken)).Selections;
        var second = (await new InMemorySnowcloneCatalog(new Random(42))
            .GetSelectionsAsync(20, TestContext.Current.CancellationToken)).Selections;

        Assert.Equal(first.Select(template => template.Phrase), second.Select(template => template.Phrase));
    }

    [Fact]
    public async Task FakeGenerator_UsesGuidanceAndGroupInBasicMode()
    {
        var generator = new FakeIdeaGenerator((_, _) => Task.CompletedTask);
        var context = Context(IdeationMode.Basic, guidance: "Grumpy");

        var text = (await generator.GenerateAsync(context, 0, TestContext.Current.CancellationToken)).Text!;

        Assert.Contains("grumpy", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pugs", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FakeGenerator_FillsSnowcloneTemplate()
    {
        var generator = new FakeIdeaGenerator((_, _) => Task.CompletedTask);
        var context = Context(IdeationMode.Snowclones, guidance: "Grumpy") with
        {
            SnowcloneTemplate = "Talk to me about X"
        };

        var text = (await generator.GenerateAsync(context, 0, TestContext.Current.CancellationToken)).Text!;

        Assert.Equal("Talk to me about grumpy pugs.", text);
    }

    [Fact]
    public async Task FakeGenerator_FillsAllVariablesAndRemainsConcise()
    {
        var generator = new FakeIdeaGenerator((_, _) => Task.CompletedTask);
        var context = Context(IdeationMode.Snowclones, guidance: "Grumpy") with
        {
            SnowcloneTemplate = "X makes Y better at Z"
        };

        var text = (await generator.GenerateAsync(context, 0, TestContext.Current.CancellationToken)).Text!;

        Assert.DoesNotContain("X", text);
        Assert.DoesNotContain("Y", text);
        Assert.DoesNotContain("Z", text);
        Assert.Equal(1, text.Count(character => character == '.'));
    }

    [Fact]
    public async Task FakeGenerator_ObservesCancellation()
    {
        var generator = new FakeIdeaGenerator((_, token) => Task.Delay(Timeout.InfiniteTimeSpan, token));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => generator.GenerateAsync(Context(IdeationMode.Basic, null), 0, cancellation.Token));
    }

    private static IdeationGenerationContext Context(IdeationMode mode, string? guidance) =>
        new(
            new IdeationCreativeContext("Dog Shop", "Funny shirts", new Dictionary<string, string>()),
            new IdeationCreativeContext("Dogs", "Dog owners", new Dictionary<string, string>()),
            new IdeationCreativeContext("Pugs", null, new Dictionary<string, string>()),
            guidance,
            mode,
            null,
            null,
            [],
            [],
            []);
}
