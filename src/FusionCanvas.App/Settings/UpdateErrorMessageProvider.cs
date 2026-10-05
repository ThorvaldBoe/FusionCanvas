using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace FusionCanvas.App.Settings;

internal static class UpdateErrorMessageProvider
{
    public static string ForCheck(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Log("check", exception);

        return exception switch
        {
            HttpRequestException => "We couldn't check for updates. Check your internet connection and try again.",
            JsonException or InvalidDataException => "The update information couldn't be read. Try again later.",
            _ => "We couldn't check for updates. Try again later."
        };
    }

    public static string ForDownload(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Log("download", exception);

        return exception switch
        {
            HttpRequestException => "We couldn't download the update. Check your internet connection and try again.",
            UnauthorizedAccessException or IOException => "We couldn't save the update. Check available disk space and permissions, then try again.",
            InvalidOperationException => "The downloaded update could not be verified. Try downloading it again later.",
            _ => "We couldn't download the update. Try again later."
        };
    }

    public static string ForInstall(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Log("install", exception);

        return exception switch
        {
            FileNotFoundException => "The update installer is no longer available. Download the update again.",
            UnauthorizedAccessException or IOException or Win32Exception => "We couldn't start the installer. Check that FusionCanvas can access its installation folder, then try again.",
            _ => "We couldn't start the update. Try again later."
        };
    }

    private static void Log(string operation, Exception exception)
    {
        var status = exception is HttpRequestException { StatusCode: { } statusCode }
            ? $" HTTP {(int)statusCode} ({statusCode})"
            : string.Empty;
        Trace.TraceError(
            "Update {0} failed with {1} (HRESULT 0x{2:X8}).{3}",
            operation,
            exception.GetType().FullName ?? exception.GetType().Name,
            exception.HResult,
            status);
    }
}
