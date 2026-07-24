namespace FusionCanvas.Application.WorkspaceTree;

public sealed class WorkspaceTreeClipboard
{
    public WorkspaceTreeClipboardPayload? Payload { get; private set; }

    public void Set(WorkspaceTreeClipboardPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (payload.EntityId == Guid.Empty)
        {
            throw new ArgumentException("Clipboard entity identifier must not be empty.", nameof(payload));
        }

        Payload = payload;
    }

    public void Clear() => Payload = null;
}
