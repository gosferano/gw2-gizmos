using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Gw2Gizmos.Desktop.Controls;

/// <summary>
/// Shows a copper amount (the unit GW2 prices are stored in) as gold/silver/copper numbers with their coin icons.
/// Drops leading-zero denominations like the text <see cref="Converters.Coin"/> formatter; <see cref="Signed"/>
/// prefixes +/− for gains/losses. Bind <see cref="Copper"/> to the raw value.
/// </summary>
public partial class CoinDisplay : UserControl
{
    public CoinDisplay()
    {
        InitializeComponent();
        Render();
    }

    public static readonly DependencyProperty CopperProperty = DependencyProperty.Register(
        nameof(Copper), typeof(object), typeof(CoinDisplay), new PropertyMetadata(null, OnChanged));

    /// <summary>The amount in copper. Accepts any numeric (int/long/decimal); null renders nothing (e.g. an
    /// unpriced item), matching the text Coin converter.</summary>
    public object? Copper
    {
        get => GetValue(CopperProperty);
        set => SetValue(CopperProperty, value);
    }

    public static readonly DependencyProperty SignedProperty = DependencyProperty.Register(
        nameof(Signed), typeof(bool), typeof(CoinDisplay), new PropertyMetadata(false, OnChanged));

    /// <summary>When true, prefix "+" for positive amounts (negatives always show "−"). For gains/losses.</summary>
    public bool Signed
    {
        get => (bool)GetValue(SignedProperty);
        set => SetValue(SignedProperty, value);
    }

    public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(
        nameof(IconSize), typeof(double), typeof(CoinDisplay), new PropertyMetadata(13.0));

    /// <summary>Coin icon width/height in px (scale up next to larger text).</summary>
    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((CoinDisplay)d).Render();

    private void Render()
    {
        if (!TryGetCopper(out long value))
        {
            // No value (null / non-numeric) — render nothing, like the text converter's empty string.
            Sign.Visibility = GoldNum.Visibility = GoldIcon.Visibility =
                SilverNum.Visibility = SilverIcon.Visibility =
                    CopperNum.Visibility = CopperIcon.Visibility = Visibility.Collapsed;
            return;
        }

        long total = Math.Abs(value);
        long gold = total / 10000;
        long silver = total % 10000 / 100;
        long copper = total % 100;

        bool hasGold = gold > 0;
        bool hasSilver = hasGold || silver > 0;

        Sign.Text = value < 0 ? "−" : Signed ? "+" : "";
        Sign.Visibility = string.IsNullOrEmpty(Sign.Text) ? Visibility.Collapsed : Visibility.Visible;

        GoldNum.Text = gold.ToString();
        SilverNum.Text = silver.ToString();
        CopperNum.Text = copper.ToString();

        GoldNum.Visibility = GoldIcon.Visibility = hasGold ? Visibility.Visible : Visibility.Collapsed;
        SilverNum.Visibility = SilverIcon.Visibility = hasSilver ? Visibility.Visible : Visibility.Collapsed;
        CopperNum.Visibility = CopperIcon.Visibility = Visibility.Visible;
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
            value = (long)Math.Round(System.Convert.ToDecimal(Copper, CultureInfo.InvariantCulture));
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
