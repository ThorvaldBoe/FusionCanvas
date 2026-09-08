namespace FusionCanvas.Application.Stores.Printify;

// This result is confined to the credential boundary; UI receives only Status.
public sealed class PrintifyCredentialReadResult(PrintifyConfigurationResult status, string? secret = null)
{
    public PrintifyConfigurationResult Status { get; } = status;
    public string? Secret { get; } = secret;
    public override string ToString() => Status.ToString();
}
