using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FusionCanvas.App.Versioning;
using FusionCanvas.Application.Versioning;

namespace FusionCanvas.App.Tests;

public sealed class SplashWindowTests
{
    [AvaloniaFact]
    public void SplashWindow_ShowsProductVersion()
    {
        var window = new SplashWindow(new ConstantVersionProvider(
            new ApplicationVersionInfo("0.1.42", "0.1.42+g3f91c2a", "3f91c2a")));
        try
        {
            window.Show();
            window.UpdateLayout();

            var texts = window.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text).ToArray();
            Assert.Contains("Version", texts);
            Assert.Contains("0.1.42", texts);
        }
        finally
        {
            window.Close();
        }
    }

    private sealed class ConstantVersionProvider(ApplicationVersionInfo info) : IApplicationVersionProvider
    {
        public ApplicationVersionInfo GetVersion() => info;
    }
}
