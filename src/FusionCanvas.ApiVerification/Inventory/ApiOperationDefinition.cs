namespace FusionCanvas.ApiVerification.Inventory;

internal sealed record ApiOperationDefinition(
    string Id,
    string Provider,
    string Method,
    string Path,
    string ProductionCall,
    string Scenario,
    string Effect,
    bool MayIncurCost = false,
    IReadOnlyList<string>? AdditionalScenarios = null,
    ApiOperationCostClass CostClassification = ApiOperationCostClass.Unknown);
