namespace FusionCanvas.Domain.Navigation;

public sealed record NavigationTreeSnapshot(IReadOnlyList<NavigationNode> Stores)
{
    public IReadOnlyList<NavigationNode> Flatten() =>
        Stores.SelectMany(Flatten).ToArray();

    public NavigationNode Find(Guid nodeId) =>
        Flatten().FirstOrDefault(node => node.Id == nodeId)
        ?? throw new InvalidOperationException("Navigation node was not found.");

    public IReadOnlyList<Guid> GetPath(Guid nodeId)
    {
        var nodes = Flatten().ToDictionary(node => node.Id);
        if (!nodes.ContainsKey(nodeId))
        {
            throw new InvalidOperationException("Navigation node was not found.");
        }

        var path = new List<Guid>();
        Guid? currentId = nodeId;
        while (currentId is not null)
        {
            var current = nodes[currentId.Value];
            path.Add(current.Id);
            currentId = current.ParentNodeId;
        }

        path.Reverse();
        return path;
    }

    private static IEnumerable<NavigationNode> Flatten(NavigationNode node)
    {
        yield return node;

        foreach (var child in node.Children.SelectMany(Flatten))
        {
            yield return child;
        }
    }
}
