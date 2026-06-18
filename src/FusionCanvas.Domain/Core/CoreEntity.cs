namespace FusionCanvas.Domain.Core;

public abstract record CoreEntity
{
    protected CoreEntity(Guid id, string name, string? notes)
    {
        Id = DomainGuards.RequireId(id, nameof(id));
        Name = DomainGuards.RequireText(name, nameof(name));
        Notes = DomainGuards.NormalizeOptionalText(notes);
    }

    public Guid Id { get; }

    public string Name { get; }

    public string? Notes { get; }
}
