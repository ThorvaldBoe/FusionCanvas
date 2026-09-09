using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FusionCanvas.App.Stores;
using FusionCanvas.Application.Stores;
using FusionCanvas.Application.Stores.Printify;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests;

public class StorePrintifyTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 7, 0, 0, 0, TimeSpan.Zero);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static StoreSummary Store(string name = "Store", FulfillmentStrategy strategy = FulfillmentStrategy.ShopifyPrintify) =>
        new(Guid.NewGuid(), WorkspaceDefaults.DefaultWorkspaceId, name, new StoreContext(), false, Now, Now, strategy);

    [AvaloniaFact]
    public async Task PresenceAndStrategies_KeepKeysApplicableOnlyToSavedActiveStores()
    {
        var service = new Service();
        var model = new StorePrintifyCredentialsViewModel(service);
        var store = Store();
        model.SetContext(store, FulfillmentStrategy.Manual, false, true);
        Assert.False(model.IsVisible);
        model.SetContext(store, FulfillmentStrategy.ShopifyManual, false, true);
        Assert.Equal(0, service.Reads);
        model.SetContext(store, FulfillmentStrategy.ShopifyPrintify, true, true);
        Assert.False(model.CanManage);
        Assert.Contains("Save", model.Status);
        Assert.Equal(0, service.Reads);
        model.SetContext(store, FulfillmentStrategy.ShopifyPrintify, false, true);
        await model.PendingOperation;
        Assert.True(model.IsMissing);
        Assert.Equal("Add", model.ManageLabel);
        Assert.Equal("Printify api key is required", model.Status);
        Assert.False(model.HasKey);
        service.Kind = PrintifyConfigurationKind.Available;
        await model.RefreshAsync();
        Assert.Equal("Manage", model.ManageLabel);
        Assert.True(model.CanVerify);
        model.SetContext(store with { IsArchived = true }, FulfillmentStrategy.ShopifyPrintify, false, true);
        Assert.False(model.CanManage);
        Assert.False(model.CanVerify);
        model.SetContext(store with { FulfillmentStrategy = FulfillmentStrategy.Manual }, FulfillmentStrategy.ShopifyPrintify, false, true);
        await model.PendingOperation;
        Assert.True(model.HasKey);
        Assert.True(model.ShowSaveGuidance);
        Assert.False(model.CanVerify);
        model.SetContext(store, FulfillmentStrategy.ShopifyPrintify, false, false);
        Assert.False(model.IsVisible);
    }

    [AvaloniaFact]
    public async Task UnreadableStorage_PreventsOverwriteAndRetries()
    {
        var service = new Service { Kind = PrintifyConfigurationKind.Unavailable };
        var model = new StorePrintifyCredentialsViewModel(service);
        model.SetContext(Store(), FulfillmentStrategy.ShopifyPrintify, false, true);
        await model.PendingOperation;
        Assert.True(model.HasError);
        Assert.False(model.IsMissing);
        Assert.False(model.CanManage);
        Assert.Null(model.CreateEditor());
        service.Kind = PrintifyConfigurationKind.Missing;
        await model.RefreshAsync();
        Assert.True(model.CanManage);
    }

    [AvaloniaFact]
    public async Task Verification_UsesCapturedContextAndIgnoresLateResults()
    {
        var service = new Service { Kind = PrintifyConfigurationKind.Available, VerificationCompletion = new() };
        var model = new StorePrintifyCredentialsViewModel(service);
        var a = Store("A");
        model.SetContext(a, FulfillmentStrategy.ShopifyPrintify, false, true);
        await model.PendingOperation;
        var pending = model.VerifyAsync();
        Assert.True(model.IsBusy);
        Assert.False(model.CanManage);
        await model.VerifyAsync();
        Assert.Equal(1, service.Verifications);
        var b = Store("B");
        model.SetContext(b, FulfillmentStrategy.ShopifyPrintify, false, true);
        await model.PendingOperation;
        Assert.True(service.LastCancellation.IsCancellationRequested);
        service.VerificationCompletion.SetResult(new(PrintifyConfigurationKind.Verified, "old Store result"));
        await pending;
        Assert.Equal(string.Empty, model.Verification);
        Assert.Equal(a.Id, service.VerifiedScope!.StoreId);
        Assert.False(model.IsBusy);
    }

    [AvaloniaFact]
    public async Task Dialog_SaveFailureAndBusyStatePreserveDraftThenClearOnSuccess()
    {
        var service = new Service { SaveCompletion = new() };
        var scope = new StoreCredentialScope(Guid.NewGuid(), Guid.NewGuid());
        var model = new PrintifyApiKeyViewModel(service, scope, "Named Store") { Draft = "synthetic-token" };
        var save = model.SaveAsync();
        Assert.True(model.IsBusy);
        Assert.False(model.RequestClose());
        model.Draft = "replacement-during-save";
        Assert.Equal("synthetic-token", model.Draft);
        await model.SaveAsync();
        Assert.Equal(1, service.Saves);
        service.SaveCompletion.SetResult(PrintifyConfigurationResult.Unavailable);
        await save;
        Assert.False(model.Saved);
        Assert.Equal("synthetic-token", model.Draft);
        Assert.NotEmpty(model.Error);
        service.SaveCompletion = null;
        await model.SaveAsync();
        Assert.True(model.Saved);
        Assert.Equal(string.Empty, model.Draft);
        Assert.Equal(scope, service.SavedScope);
    }

    [AvaloniaFact]
    public async Task Dialog_IsMaskedAndKeyboardDismissalProtectsDraft()
    {
        var service = new Service();
        var model = new PrintifyApiKeyViewModel(service, new(Guid.NewGuid(), Guid.NewGuid()), "Store A");
        var owner = new Window();
        owner.Show();
        var dialog = new PrintifyApiKeyWindow { DataContext = model };
        var closed = dialog.ShowDialog(owner);
        Dispatcher.UIThread.RunJobs();
        var field = dialog.FindControl<TextBox>("KeyInput")!;
        Assert.NotEqual('\0', field.PasswordChar);
        Assert.True(field.IsFocused);
        Assert.True(string.IsNullOrEmpty(field.Text));
        field.Text = "synthetic-token";
        Assert.Equal("synthetic-token", model.Draft);
        dialog.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, string.Empty);
        Assert.True(model.ShowDiscard);
        Assert.True(dialog.IsVisible);
        model.KeepEditingCommand.Execute(null);
        Assert.False(model.ShowDiscard);
        dialog.Close();
        Assert.True(dialog.IsVisible);
        Assert.True(model.ShowDiscard);
        model.DiscardCommand.Execute(null);
        await closed;
        Assert.Equal(string.Empty, model.Draft);
        Assert.Equal(0, service.Saves);
        owner.Close();
    }

    [AvaloniaFact]
    public async Task StoreEditor_BindsThreeStrategiesAndCredentialControls()
    {
        var repository = new Repository();
        var stores = new StoreManagementService(repository);
        var created = await stores.CreateStoreAsync(new("Store", FulfillmentStrategy: FulfillmentStrategy.ShopifyPrintify), Ct);
        var native = new Native();
        var model = new StoreManagementViewModel(stores);
        model.ConfigurePrintify(native, new Verifier());
        await model.LoadAsync(Ct);
        model.OpenStoreEditorCommand.Execute(null);
        var window = new StoreEditorWindow { DataContext = model };
        window.Show();
        await model.PrintifyCredentials!.PendingOperation;
        window.UpdateLayout();
        var selector = window.GetVisualDescendants().OfType<ComboBox>()
            .Single(c => Avalonia.Automation.AutomationProperties.GetAutomationId(c) == "StoreEditor.FulfillmentStrategy");
        Assert.Equal(4, selector.ItemCount);
        Assert.Equal(FulfillmentStrategy.ShopifyPrintify, selector.SelectedItem);
        var manage = window.FindControl<Button>("PrintifyManageButton")!;
        Assert.Equal("Add", manage.Content);
        Assert.True(manage.IsEnabled);
        selector.SelectedItem = FulfillmentStrategy.Printify;
        Assert.Equal(FulfillmentStrategy.Printify, model.SelectedFulfillmentStrategy);
        Assert.True(model.PrintifyCredentials.IsVisible);
        selector.SelectedItem = FulfillmentStrategy.ShopifyPrintify;
        selector.SelectedItem = FulfillmentStrategy.ShopifyManual;
        Assert.Equal(FulfillmentStrategy.ShopifyManual, model.SelectedFulfillmentStrategy);
        Assert.False(model.PrintifyCredentials.IsVisible);
        await model.SaveSelectedStoreAsync(Ct);
        Assert.True(model.ShowStrategyWarning);
        model.CancelStrategyCommand.Execute(null);
        Assert.Equal(FulfillmentStrategy.ShopifyPrintify, model.SelectedFulfillmentStrategy);
        Assert.Equal(FulfillmentStrategy.ShopifyPrintify, (await stores.LoadAsync(Ct)).ActiveStore!.FulfillmentStrategy);
        selector.SelectedItem = FulfillmentStrategy.ShopifyManual;
        await model.SaveSelectedStoreAsync(Ct);
        model.ConfirmStrategyCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(FulfillmentStrategy.ShopifyManual, (await stores.LoadAsync(Ct)).ActiveStore!.FulfillmentStrategy);
        Assert.Equal(created.Store!.Id, model.SelectedStore!.Id);
        window.Close();
    }


    private sealed class Service : IStorePrintifyConfigurationService
    {
        public PrintifyConfigurationKind Kind { get; set; } = PrintifyConfigurationKind.Missing;
        public int Reads { get; private set; }
        public int Saves { get; private set; }
        public int Verifications { get; private set; }
        public TaskCompletionSource<PrintifyConfigurationResult>? VerificationCompletion { get; set; }
        public TaskCompletionSource<PrintifyConfigurationResult>? SaveCompletion { get; set; }
        public StoreCredentialScope? VerifiedScope { get; private set; }
        public StoreCredentialScope? SavedScope { get; private set; }
        public CancellationToken LastCancellation { get; private set; }
        public Task<PrintifyConfigurationResult> ReadStatusAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default)
        {
            Reads++;
            return Task.FromResult(new PrintifyConfigurationResult(Kind, Kind switch
            {
                PrintifyConfigurationKind.Missing => "Printify api key is required",
                PrintifyConfigurationKind.Available => "Printify api key is provided",
                _ => "Storage unavailable"
            }));
        }
        public Task<PrintifyConfigurationResult> SaveAsync(StoreCredentialScope scope, string key, CancellationToken cancellationToken = default)
        {
            Saves++; SavedScope = scope;
            return SaveCompletion?.Task ?? Task.FromResult(new PrintifyConfigurationResult(PrintifyConfigurationKind.Saved, "Saved"));
        }
        public Task<PrintifyConfigurationResult> VerifyAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default)
        {
            Verifications++; VerifiedScope = scope; LastCancellation = cancellationToken;
            return VerificationCompletion?.Task ?? Task.FromResult(new PrintifyConfigurationResult(PrintifyConfigurationKind.Verified, "Verified"));
        }
    }

    private sealed class Repository : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = WorkspaceSnapshot.Empty;
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(_snapshot);
        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        { _snapshot = snapshot; return Task.CompletedTask; }
    }
    private sealed class Native : IStorePrintifyCredentialStore
    {
        public Task<PrintifyCredentialReadResult> ReadAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PrintifyCredentialReadResult(new(PrintifyConfigurationKind.Missing, "Printify api key is required")));
        public Task<PrintifyConfigurationResult> SaveAsync(StoreCredentialScope scope, string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PrintifyConfigurationResult(PrintifyConfigurationKind.Saved, "Saved"));
    }
    private sealed class Verifier : IPrintifyCredentialVerifier
    {
        public Task<PrintifyConfigurationResult> VerifyAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PrintifyConfigurationResult(PrintifyConfigurationKind.Verified, "Verified"));
    }

}
