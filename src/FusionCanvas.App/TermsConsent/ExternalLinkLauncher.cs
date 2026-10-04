using System.Diagnostics;

namespace FusionCanvas.App.TermsConsent;

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
