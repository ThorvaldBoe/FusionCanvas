namespace FusionCanvas.Application.Updates;

public sealed record UpdateDownloadProgress(long BytesReceived, long? TotalBytes);
