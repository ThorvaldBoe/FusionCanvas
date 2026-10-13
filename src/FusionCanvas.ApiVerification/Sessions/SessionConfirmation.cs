namespace FusionCanvas.ApiVerification.Sessions;

internal static class SessionConfirmation
{
    public static async Task<bool> RequirePhraseAsync(string instruction, string phrase, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Console.WriteLine(instruction);
        Console.Write($"Type `{phrase}` to continue: ");
        var response = await Console.In.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        return string.Equals(response?.Trim(), phrase, StringComparison.Ordinal);
    }
}
