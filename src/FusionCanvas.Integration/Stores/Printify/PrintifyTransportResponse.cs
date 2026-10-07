using System.Net;

namespace FusionCanvas.Integration.Stores.Printify;

public sealed record PrintifyTransportResponse(PrintifyTransportOutcome Outcome, HttpStatusCode? StatusCode, byte[] Body)
{
    public bool Succeeded => Outcome == PrintifyTransportOutcome.Succeeded;
}
