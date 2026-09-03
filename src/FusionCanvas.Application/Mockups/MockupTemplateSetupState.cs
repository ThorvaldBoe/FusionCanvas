using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Mockups;

public sealed record MockupTemplateSetupState(Guid StoreId, bool IsReadOnly, IReadOnlyList<MockupTemplate> Templates, IReadOnlyList<MockupTemplateColorVariant> Colors, IReadOnlyList<MockupTemplateRevision> Revisions, IReadOnlyList<MockupTemplateReadinessSummary>? Readiness = null);
