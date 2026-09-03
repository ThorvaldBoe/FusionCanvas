using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Workflow;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Application.Stores;
using FusionCanvas.Application.WorkflowNavigation;

namespace FusionCanvas.App.Views;

internal static class NavigationContextFactory
{
    public static IReadOnlyList<NavigationDocumentContext> Create(
        WorkspaceSnapshot snapshot,
        StoreSummary? selectedStore)
    {
        if (selectedStore is null)
        {
            return [];
        }

        var store = snapshot.Stores.SingleOrDefault(candidate => candidate.Id == selectedStore.Id)
            ?? new Store(
                selectedStore.Id,
                selectedStore.Name,
                selectedStore.Context.Description,
                selectedStore.IsArchived,
                selectedStore.CreatedAt,
                selectedStore.UpdatedAt,
                "{}");
        var contexts = new List<NavigationDocumentContext>();

        foreach (var niche in snapshot.Niches.Where(niche => niche.StoreId == store.Id && !niche.IsArchived))
        {
            contexts.Add(NewNavigationContext(
                niche.Id,
                niche.Name,
                WorkflowStage.Idea,
                DocumentContextKind.Topic,
                WorkspaceEntityKind.Niche,
                [store.Id, niche.Id],
                $"{store.Name} / {niche.Name}"));

            foreach (var group in snapshot.Groups.Where(group => group.StoreId == store.Id && group.NicheId == niche.Id && group.ParentGroupId is null))
            {
                AddGroupContexts(snapshot, contexts, group, [store.Id, niche.Id, group.Id], $"{store.Name} / {niche.Name} / {group.Name}");
            }

            foreach (var item in snapshot.Items.Where(item => item.StoreId == store.Id && item.NicheId == niche.Id && item.GroupId is null))
            {
                contexts.Add(NewItemContext(item, [store.Id, niche.Id, item.Id], $"{store.Name} / {niche.Name} / {item.Name}"));
            }
        }

        return contexts;
    }

    private static void AddGroupContexts(
        WorkspaceSnapshot snapshot,
        List<NavigationDocumentContext> contexts,
        TopicGroup group,
        IReadOnlyList<Guid> nodePath,
        string displayPath)
    {
        contexts.Add(NewNavigationContext(
            group.Id,
            group.Name,
            WorkflowStage.Idea,
            DocumentContextKind.Topic,
            WorkspaceEntityKind.Group,
            nodePath,
            displayPath));

        foreach (var childGroup in snapshot.Groups.Where(candidate => candidate.ParentGroupId == group.Id))
        {
            AddGroupContexts(snapshot, contexts, childGroup, [.. nodePath, childGroup.Id], $"{displayPath} / {childGroup.Name}");
        }

        foreach (var item in snapshot.Items.Where(item => item.GroupId == group.Id))
        {
            contexts.Add(NewItemContext(item, [.. nodePath, item.Id], $"{displayPath} / {item.Name}"));
        }
    }

    private static NavigationDocumentContext NewItemContext(
        Item item,
        IReadOnlyList<Guid> nodePath,
        string displayPath) =>
        NewNavigationContext(
            item.Id,
            item.Name,
            item.Stage,
            DocumentContextKind.Item,
            WorkspaceEntityKind.Item,
            nodePath,
            displayPath,
            item);

    private static IReadOnlyList<WorkflowStage> ReachedStages(WorkflowStage stage) =>
        WorkflowStages.Ordered.Where(reached => reached <= stage).ToArray();

    private static (bool IsInactive, string? InactiveLabel) ResolveInactive(Item item)
    {
        if (item.IsArchived)
        {
            return (true, "Archived");
        }

        return item.Status == ItemStatus.Rejected
            ? (true, "Rejected")
            : (false, null);
    }

    private static NavigationDocumentContext NewNavigationContext(
        Guid contextId,
        string title,
        WorkflowStage stage,
        DocumentContextKind kind,
        WorkspaceEntityKind entityKind,
        IReadOnlyList<Guid> nodePath,
        string displayPath,
        Item? item = null)
    {
        ActiveItemWorkflowContext? workflow = null;
        if (kind == DocumentContextKind.Item && item is not null)
        {
            var (isInactive, inactiveLabel) = ResolveInactive(item);
            workflow = new ActiveItemWorkflowContext(
                contextId,
                stage,
                ReachedStages(stage),
                isInactive,
                inactiveLabel);
        }

        return new NavigationDocumentContext(
            title,
            new DocumentContext(
                contextId,
                title,
                kind,
                new DocumentNavigationLocation(nodePath, displayPath),
                workflow,
                stage,
                DocumentWindowViewModel.GetDefaultDetailViewKey(stage),
                entityKind));
    }
}
