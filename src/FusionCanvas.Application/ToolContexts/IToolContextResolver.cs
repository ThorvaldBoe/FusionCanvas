namespace FusionCanvas.Application.ToolContexts;

public interface IToolContextResolver
{
    ToolContextResolution Resolve(ToolContextResolveRequest request);

    ToolContextCreationDefaults ResolveCreationDefaults(ToolContextResolution resolution);

    ToolContextResolution ResolveScope(ToolContextResolveRequest request, ToolContextScopeKind scope);
}
