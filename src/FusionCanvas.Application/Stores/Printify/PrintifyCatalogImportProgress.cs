namespace FusionCanvas.Application.Stores.Printify;

public sealed record PrintifyCatalogImportProgress(string Phase, int? Completed = null, int? Total = null);
