using FusionCanvas.ApiVerification.Sessions;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

try
{
    return await ApiVerificationCommand.RunAsync(args, cancellation.Token);
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Verification cancelled.");
    return 130;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Verification tool failed: {SafeDiagnostic.Redact(exception.Message)}");
    return 1;
}
