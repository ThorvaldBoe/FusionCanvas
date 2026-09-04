using FusionCanvas.Domain.Workflow;
using System.ComponentModel;
using System.Windows.Input;
using FusionCanvas.Application.WorkflowNavigation;

namespace FusionCanvas.App.Workflow;

public sealed record DocumentTabWorkflowContext(Guid TabId, ActiveItemWorkflowContext? ActiveItem)
{
    public Guid TabId { get; } = TabId == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(TabId))
        : TabId;
}
