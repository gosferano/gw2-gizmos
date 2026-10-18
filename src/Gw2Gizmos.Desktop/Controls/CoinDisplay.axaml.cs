using System.Globalization;
using Avalonia;
using Avalonia.Controls;

namespace Gw2Gizmos.Desktop.Controls;

/// <summary>
/// Shows a copper amount (the unit GW2 prices are stored in) as gold/silver/copper numbers with their coin icons.
/// Drops leading-zero denominations like the text <c>Coin</c> formatter; <see cref="Signed"/> prefixes +/− for
/// gains/losses, and colours them (green gain, red loss) through the <c>gain</c> / <c>loss</c> style classes — an
/// explicitly set Foreground still wins. Bind <see cref="Copper"/> to the raw value.
/// </summary>
public partial class CoinDisplay : UserControl
{
    public static readonly StyledProperty<object?> CopperProperty =
        AvaloniaProperty.Register<CoinDisplay, object?>(nameof(Copper));

    public static readonly StyledProperty<bool> SignedProperty =
        AvaloniaProperty.Register<CoinDisplay, bool>(nameof(Signed));

    public static readonly StyledProperty<double> IconSizeProperty =
        AvaloniaProperty.Register<CoinDisplay, double>(nameof(IconSize), 13.0);

    public CoinDisplay()
    {
        InitializeComponent();
        Render();
    }

    /// <summary>The amount in copper. Accepts any numeric (int/long/decimal/double); null renders nothing (e.g. an
    /// unpriced item), matching the text Coin converter.</summary>
    public object? Copper
    {
        get => GetValue(CopperProperty);
        set => SetValue(CopperProperty, value);
    }

    /// <summary>When true, prefix "+" for positive amounts (negatives always show "−"). For gains/losses.</summary>
    public bool Signed
    {
        get => GetValue(SignedProperty);
        set => SetValue(SignedProperty, value);
    }

    /// <summary>Coin icon width/height in px (scale up next to larger text).</summary>
    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CopperProperty || change.Property == SignedProperty)
        {
            Render();
        }
    }

    private void Render()
    {
        if (!TryGetCopper(out long value))
        {
            // No value (null / non-numeric) — render nothing, like the text converter's empty string.
            Classes.Set("gain", false);
            Classes.Set("loss", false);
            Sign.IsVisible = GoldNum.IsVisible = GoldIcon.IsVisible =
                SilverNum.IsVisible = SilverIcon.IsVisible = CopperNum.IsVisible = CopperIcon.IsVisible = false;
            return;
        }

        long total = Math.Abs(value);
        long gold = total / 10000;
        long silver = total % 10000 / 100;
        long copper = total % 100;

        bool hasGold = gold > 0;
        bool hasSilver = hasGold || silver > 0;

        Classes.Set("gain", Signed && value > 0);
        Classes.Set("loss", Signed && value < 0);

        Sign.Text = value < 0 ? "−" : Signed ? "+" : "";
        Sign.IsVisible = !string.IsNullOrEmpty(Sign.Text);

        GoldNum.Text = gold.ToString(CultureInfo.CurrentCulture);
        SilverNum.Text = silver.ToString(CultureInfo.CurrentCulture);
        CopperNum.Text = copper.ToString(CultureInfo.CurrentCulture);

        GoldNum.IsVisible = GoldIcon.IsVisible = hasGold;
        SilverNum.IsVisible = SilverIcon.IsVisible = hasSilver;
        CopperNum.IsVisible = CopperIcon.IsVisible = true;
    }

    private bool TryGetCopper(out long value)
    {
        value = 0;
        if (Copper is null)
        {
            return false;
        }

        try
        {
            value = (long)Math.Round(Convert.ToDecimal(Copper, CultureInfo.InvariantCulture));
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
