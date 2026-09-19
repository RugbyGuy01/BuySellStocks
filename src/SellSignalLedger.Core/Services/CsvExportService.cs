using System.Globalization;
using System.Text;
using SellSignalLedger.Core.Models;

namespace SellSignalLedger.Core.Services;

/// <summary>
/// Writes holdings in the same column layout CsvImportService expects, so a file
/// exported here can be re-imported unchanged via the Import Holdings window.
/// </summary>
public static class CsvExportService
{
    public static string Export(IEnumerable<Position> positions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Ticker,Quantity,CostBasis,PurchaseDate,MustSellPct,SellProfitPct,SellDropProfitPct");

        foreach (var p in positions)
        {
            sb.AppendLine(string.Join(",",
                p.Ticker,
                p.Quantity.ToString(CultureInfo.InvariantCulture),
                p.CostBasis.ToString(CultureInfo.InvariantCulture),
                p.PurchaseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                p.MustSellPct.ToString(CultureInfo.InvariantCulture),
                p.SellProfitPct.ToString(CultureInfo.InvariantCulture),
                p.SellDropProfitPct.ToString(CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }
}
