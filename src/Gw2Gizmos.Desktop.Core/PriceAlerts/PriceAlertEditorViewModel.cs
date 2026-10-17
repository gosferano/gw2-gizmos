using System;
using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Gw2Api.Client;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Backs the price alert dialog: which price (sell or buy), which way (below or above), the amount as gold / silver /
/// copper, and whether it re-arms. Shows the item's current prices, fetched fresh; a new alert's amount follows the
/// chosen side's current price until the user types one.
/// </summary>
public sealed class PriceAlertEditorViewModel : ViewModelBase
{
    private readonly PriceAlert _alert;
    private readonly bool _isNew;
    private string _selectedSide;
    private string _selectedDirection;
    private decimal? _gold;
    private decimal? _silver;
    private decimal? _copper;
    private bool _repeat;
    private int? _currentBuy;
    private int? _currentSell;
    private bool _amountEdited;

    public PriceAlertEditorViewModel(PriceAlert alert, bool isNew)
    {
        _alert = alert;
        _isNew = isNew;
        _selectedSide = alert.Side == PriceAlertSide.Buy ? SideOptions[1] : SideOptions[0];
        _selectedDirection = alert.Direction == PriceAlertDirection.Above ? DirectionOptions[1] : DirectionOptions[0];
        _repeat = alert.Repeat;
        SetPrice(alert.PriceCopper);
    }

    public string Title => _isNew ? "New price alert" : "Edit price alert";

    public int ItemId => _alert.ItemId;

    public string ItemName => _alert.ItemName;

    public string[] SideOptions { get; } = { "Sell price", "Buy price" };

    public string[] DirectionOptions { get; } = { "at or below", "at or above" };

    public string SelectedSide
    {
        get => _selectedSide;
        set
        {
            if (value is not null && SetProperty(ref _selectedSide, value))
            {
                OnPropertyChanged(nameof(CurrentPrice));
                OnPropertyChanged(nameof(HasCurrentPrice));
                PrefillAmount();
            }
        }
    }

    public string SelectedDirection
    {
        get => _selectedDirection;
        set
        {
            if (value is not null)
            {
                SetProperty(ref _selectedDirection, value);
            }
        }
    }

    public decimal? Gold
    {
        get => _gold;
        set => SetAmount(ref _gold, value);
    }

    public decimal? Silver
    {
        get => _silver;
        set => SetAmount(ref _silver, value);
    }

    public decimal? Copper
    {
        get => _copper;
        set => SetAmount(ref _copper, value);
    }

    public bool Repeat
    {
        get => _repeat;
        set => SetProperty(ref _repeat, value);
    }

    /// <summary>The amount entered, in copper.</summary>
    public int PriceCopper => (int)Math.Min(int.MaxValue, ((_gold ?? 0) * 10_000) + ((_silver ?? 0) * 100) + (_copper ?? 0));

    public bool IsValid => PriceCopper > 0;

    private bool IsBuy => SelectedSide == SideOptions[1];

    /// <summary>The chosen side's current price, for reference; null until fetched or when that side is empty.</summary>
    public int? CurrentPrice => IsBuy ? _currentBuy : _currentSell;

    public bool HasCurrentPrice => CurrentPrice is not null;

    /// <summary>Fetches the item's current prices; a new alert takes the sell price as its starting amount.</summary>
    public async Task LoadPricesAsync(IGw2ApiClientFactory clients)
    {
        try
        {
            var prices = (await clients.Create(Locale.English).V2.Commerce.Prices.GetByIds(new[] { ItemId })).Content;
            var price = prices?.FirstOrDefault();
            _currentBuy = price?.Buys is { Quantity: > 0 } buys ? buys.UnitPrice : null;
            _currentSell = price?.Sells is { Quantity: > 0 } sells ? sells.UnitPrice : null;
            OnPropertyChanged(nameof(CurrentPrice));
            OnPropertyChanged(nameof(HasCurrentPrice));
            PrefillAmount();
        }
        catch (Exception)
        {
            // No current price to show; the amount is entered by hand.
        }
    }

    /// <summary>The alert as edited, armed afresh (it's re-checked right away).</summary>
    public PriceAlert ToAlert() =>
        _alert with
        {
            Side = IsBuy ? PriceAlertSide.Buy : PriceAlertSide.Sell,
            Direction = SelectedDirection == DirectionOptions[1] ? PriceAlertDirection.Above : PriceAlertDirection.Below,
            PriceCopper = PriceCopper,
            Repeat = Repeat,
            IsTriggered = false,
            TriggeredAtUtc = null,
            LastPrice = null,
            LastCheckedUtc = null,
        };

    /// <summary>A new alert takes the chosen side's current price as its amount, until the user enters their own.</summary>
    private void PrefillAmount()
    {
        if (_isNew && !_amountEdited && CurrentPrice is { } price)
        {
            SetPrice(price);
        }
    }

    private void SetPrice(int copper)
    {
        _gold = copper / 10_000;
        _silver = copper / 100 % 100;
        _copper = copper % 100;
        OnPropertyChanged(nameof(Gold));
        OnPropertyChanged(nameof(Silver));
        OnPropertyChanged(nameof(Copper));
        OnPropertyChanged(nameof(IsValid));
    }

    private void SetAmount(ref decimal? field, decimal? value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        if (SetProperty(ref field, value, name))
        {
            _amountEdited = true;
            OnPropertyChanged(nameof(IsValid));
        }
    }
}
