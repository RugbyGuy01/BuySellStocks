namespace SellSignalLedger.Core.Models;

public class Position
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Ticker { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal CostBasis { get; set; }
    public decimal PeakPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal TriggerAnchorPrice { get; set; }
    public PositionState State { get; set; } = PositionState.Watching;

    public decimal MustSellPct { get; set; }
    public decimal SellProfitPct { get; set; }
    public decimal SellDropProfitPct { get; set; }

    public decimal RealizedGainLoss { get; set; }

    public DateTime PurchaseDate { get; set; }

    /// <summary>The ratchet anchor: the original buy price until the position starts
    /// trailing, then the highest price seen since — so Must Sell and Sell Profit
    /// both move up together off the same peak once trailing begins.</summary>
    private decimal RatchetAnchor() => State == PositionState.Trailing
        ? PeakPrice
        : (TriggerAnchorPrice > 0 ? TriggerAnchorPrice : CostBasis);

    public decimal MustSellFloor() => RatchetAnchor() * (1 - MustSellPct / 100m);

    public decimal SellProfitTarget() => RatchetAnchor() * (1 + SellProfitPct / 100m);

    public decimal TrailingStopPrice() => PeakPrice * (1 - SellDropProfitPct / 100m);

    public decimal UnrealizedGainLoss() => (CurrentPrice - CostBasis) * Quantity;

    public decimal UnrealizedGainLossPct() =>
        CostBasis == 0 ? 0 : (CurrentPrice - CostBasis) / CostBasis * 100m;
}
