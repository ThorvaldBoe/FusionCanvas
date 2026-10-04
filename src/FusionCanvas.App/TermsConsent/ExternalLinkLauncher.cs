using System.Diagnostics;

namespace FusionCanvas.App.TermsConsent;

public interface IExternalLinkLauncher
{
    void Open(Uri uri);
}

public sealed class ProcessExternalLinkLauncher : IExternalLinkLauncher
{
    public void Open(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        Process.Start(new ProcessStartInfo
        {
            FileName = uri.ToString(),
            UseShellExecute = true
        });
    }
}
