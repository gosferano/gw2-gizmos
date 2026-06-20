using System.Globalization;
using System.Windows.Data;
using Gw2Gizmos.RecipeFinder.Model;

namespace Gw2Gizmos.Desktop.Converters;

/// <summary>
/// Formats a <see cref="VendorOffer"/>'s full cost as one line of text — e.g. "6,250 Airship Part + 1,050 Karma"
/// — for the buy-column hover, where a single wrapping line fits far better than icon rows. Kept out of the
/// model: it's purely a display concern.
/// </summary>
public sealed class VendorOfferCostConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is VendorOffer offer
            ? string.Join(" + ", offer.Cost.Select(component => $"{component.Amount:N0} {component.Currency}"))
            : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
