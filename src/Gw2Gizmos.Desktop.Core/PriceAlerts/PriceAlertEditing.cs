using System.Threading.Tasks;
using Gw2Gizmos.Desktop.Core.Ui;
using Gw2Gizmos.Gw2Api.Client;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>Adds or edits a price alert through the alert dialog, saving it when confirmed.</summary>
public sealed class PriceAlertEditing
{
    private readonly IDialogService _dialogs;
    private readonly IGw2ApiClientFactory _clients;
    private readonly PriceAlertStore _store;

    public PriceAlertEditing(IDialogService dialogs, IGw2ApiClientFactory clients, PriceAlertStore store)
    {
        _dialogs = dialogs;
        _clients = clients;
        _store = store;
    }

    public Task<bool> AddAsync(int itemId, string itemName) =>
        EditAsync(
            new PriceAlert
            {
                ItemId = itemId, ItemName = itemName, Side = PriceAlertSide.Sell, Direction = PriceAlertDirection.Below,
            },
            isNew: true);

    public Task<bool> EditAsync(PriceAlert alert) => EditAsync(alert, isNew: false);

    private async Task<bool> EditAsync(PriceAlert alert, bool isNew)
    {
        var editor = new PriceAlertEditorViewModel(alert, isNew);
        _ = editor.LoadPricesAsync(_clients);
        if (!await _dialogs.EditPriceAlertAsync(editor) || !editor.IsValid)
        {
            return false;
        }

        _store.Save(editor.ToAlert());
        return true;
    }
}
