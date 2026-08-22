namespace SellSignalLedger.Core.Models;

public class AppSettings
{
    public int Id { get; set; } = 1;
    public decimal DefaultMustSellPct { get; set; } = 8m;
    public decimal DefaultSellProfitPct { get; set; } = 15m;
    public decimal DefaultSellDropProfitPct { get; set; } = 5m;
    public DateTime? LastPriceRefresh { get; set; }
}
