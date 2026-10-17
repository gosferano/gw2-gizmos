using System;
using System.Text;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Formats a copper amount (the unit every GW2 price is stored in) as <c>g/s/c</c> — e.g. 12345 → "1g 23s 45c",
/// 45 → "45c". Leading zero denominations are dropped; copper is always shown. For plain text only — notifications
/// and chart axes; the UI shows amounts with coin icons (the desktop's CoinDisplay), using the same rules.
/// </summary>
public static class Coin
{
    public static string Format(long copper)
    {
        bool negative = copper < 0;
        long total = Math.Abs(copper);

        long gold = total / 10000;
        long silver = total % 10000 / 100;
        long remainder = total % 100;

        var sb = new StringBuilder();
        if (negative)
        {
            sb.Append('-');
        }

        if (gold > 0)
        {
            sb.Append(gold).Append("g ");
        }

        if (gold > 0 || silver > 0)
        {
            sb.Append(silver).Append("s ");
        }

        sb.Append(remainder).Append('c');
        return sb.ToString();
    }
}
