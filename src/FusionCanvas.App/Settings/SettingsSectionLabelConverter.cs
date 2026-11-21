using System.Globalization;
using Avalonia.Data.Converters;

namespace FusionCanvas.App.Settings;

public sealed class SettingsSectionLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        SettingsSection.WorkspaceDiagnostics => "Workspace Diagnostics",
        SettingsSection.AI => "AI",
        SettingsSection.General => "General",
        SettingsSection.Terms => "Terms",
        SettingsSection.About => "About",
        _ => string.Empty
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
