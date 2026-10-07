using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FusionCanvas.Application.Telemetry;

namespace FusionCanvas.Integration.Stores.Printify;

public sealed class PrintifyHttpTransport
{
    public const int DefaultMaximumResponseBytes = 4 * 1024 * 1024;
    private readonly HttpClient _client;
    private readonly TimeSpan _timeout;
    private readonly ITelemetryRecorder? _telemetry;

    public PrintifyHttpTransport(HttpClient client, TimeSpan? timeout = null, ITelemetryRecorder? telemetry = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _timeout = timeout ?? TimeSpan.FromSeconds(30);
        _telemetry = telemetry;
    }

    public async Task<PrintifyTransportResponse> SendAsync(
        HttpMethod method,
        Uri requestUri,
        string secret,
        HttpContent? content = null,
        int maximumResponseBytes = DefaultMaximumResponseBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(requestUri);
        if (string.IsNullOrWhiteSpace(secret))
        {
            return new(PrintifyTransportOutcome.InvalidCredential, HttpStatusCode.Unauthorized, []);
        }

        var stopwatch = Stopwatch.StartNew();
        var requestDetails = JsonSerializer.Serialize(new { method = method.Method, uri = requestUri.ToString() });
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);
        using var request = new HttpRequestMessage(method, requestUri) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret.Trim());
        request.Headers.UserAgent.ParseAdd("FusionCanvas");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            var outcome = Classify(response.StatusCode);
            if (outcome != PrintifyTransportOutcome.Succeeded)
            {
                return await FinishAsync(new(outcome, response.StatusCode, []), requestDetails, stopwatch, cancellationToken).ConfigureAwait(false);
            }

            if (response.Content.Headers.ContentLength is long declaredLength
                && declaredLength > maximumResponseBytes)
            {
                return await FinishAsync(new(PrintifyTransportOutcome.UnexpectedResponse, response.StatusCode, []), requestDetails, stopwatch, cancellationToken).ConfigureAwait(false);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            while (true)
            {
                var count = await stream.ReadAsync(chunk, timeout.Token).ConfigureAwait(false);
                if (count == 0) break;
                if (buffer.Length + count > maximumResponseBytes)
                {
                    return await FinishAsync(new(PrintifyTransportOutcome.UnexpectedResponse, response.StatusCode, []), requestDetails, stopwatch, cancellationToken).ConfigureAwait(false);
                }

                buffer.Write(chunk, 0, count);
            }

            return await FinishAsync(new(PrintifyTransportOutcome.Succeeded, response.StatusCode, buffer.ToArray()), requestDetails, stopwatch, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return await FinishAsync(new(PrintifyTransportOutcome.NetworkFailure, null, []), requestDetails, stopwatch, CancellationToken.None).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            return await FinishAsync(new(PrintifyTransportOutcome.NetworkFailure, null, []), requestDetails, stopwatch, CancellationToken.None).ConfigureAwait(false);
        }
        catch (IOException)
        {
            return await FinishAsync(new(PrintifyTransportOutcome.NetworkFailure, null, []), requestDetails, stopwatch, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task<PrintifyTransportResponse> FinishAsync(
        PrintifyTransportResponse response,
        string requestDetails,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        if (_telemetry?.IsCaptureEnabled == true)
        {
            try
            {
                await _telemetry.RecordAsync(
                    new TelemetryEventRequest(
                        "Integration.Printify",
                        "HttpRequest",
                        response.Outcome is PrintifyTransportOutcome.Succeeded ? "Information" : "Warning",
                        response.Outcome.ToString(),
                        response.StatusCode is { } messageStatus ? $"HTTP {(int)messageStatus}" : response.Outcome.ToString(),
                        RequestDetailsJson: requestDetails,
                        ResponseDetailsJson: JsonSerializer.Serialize(new
                        {
                            statusCode = response.StatusCode is { } detailsStatus ? (int)detailsStatus : (int?)null,
                            elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
                            contentBytes = response.Body.Length
                        })),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Diagnostics must never turn a classified Printify response into a failed operation.
            }
        }

        return response;
    }

    private static PrintifyTransportOutcome Classify(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.OK or HttpStatusCode.Created or HttpStatusCode.Accepted or HttpStatusCode.NoContent
            => PrintifyTransportOutcome.Succeeded,
        HttpStatusCode.Unauthorized => PrintifyTransportOutcome.InvalidCredential,
        HttpStatusCode.Forbidden => PrintifyTransportOutcome.PermissionDenied,
        HttpStatusCode.NotFound => PrintifyTransportOutcome.NotFound,
        HttpStatusCode.TooManyRequests => PrintifyTransportOutcome.RateLimited,
        HttpStatusCode.Locked or HttpStatusCode.Conflict => PrintifyTransportOutcome.Locked,
        _ when (int)statusCode >= 500 => PrintifyTransportOutcome.NetworkFailure,
        _ => PrintifyTransportOutcome.UnexpectedResponse
    };
}
