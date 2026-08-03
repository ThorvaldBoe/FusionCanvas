namespace FusionCanvas.Application.Versioning;

public interface IApplicationVersionProvider
{
    ApplicationVersionInfo GetVersion();
}
