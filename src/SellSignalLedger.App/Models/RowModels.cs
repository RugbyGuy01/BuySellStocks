using System.Windows.Media;
using SellSignalLedger.Core.Models;

namespace SellSignalLedger.App.Models;

public static class Brushes2
{
    public static readonly Brush Gain = new SolidColorBrush(Color.FromRgb(0x2F, 0x7D, 0x54));
    public static readonly Brush Loss = new SolidColorBrush(Color.FromRgb(0xA8, 0x41, 0x2F));
    public static readonly Brush Accent = new SolidColorBrush(Color.FromRgb(0x8F, 0x65, 0x26));
    public static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(0x1C, 0x23, 0x1F));
    public static readonly Brush TrailingHighlight = new SolidColorBrush(Color.FromRgb(0xFC, 0xEF, 0xA8));
    public static readonly Brush MustSellHighlight = new SolidColorBrush(Color.FromRgb(0xF5, 0xB8, 0xAD));
    public static readonly Brush BuySignalHighlight = new SolidColorBrush(Color.FromRgb(0xC8, 0xE6, 0xC9));
    public static readonly Brush NoHighlight = Brushes.Transparent;
}

public class OwnedStockRow
{
    public Guid Id { get; init; }
    public string Ticker { get; init; } = "";
    public int Quantity { get; init; }
    public string CostBasis { get; init; } = "";
    public string CurrentPrice { get; init; } = "";
    public string State { get; init; } = "";
    public string UnrealizedGainLoss { get; init; } = "";
    public Brush UnrealizedBrush { get; init; } = Brushes2.Ink;
    public string RealizedGainLoss { get; init; } = "";
    public Brush RealizedBrush { get; init; } = Brushes2.Ink;
    public string MustSell { get; init; } = "";
    public string SellProfit { get; init; } = "";
    public string TrailingStop { get; init; } = "";
    public Brush RowBackground { get; init; } = Brushes2.NoHighlight;

    public static OwnedStockRow From(Position p)
    {
        var unrealized = p.UnrealizedGainLoss();
        var unrealizedPct = p.UnrealizedGainLossPct();

        // Must Sell breach always wins the highlight, even if the trailing stop is
        // also breached — it's the more urgent condition. Yellow is specifically for
        // a breached trailing stop, not merely being in Trailing state — a position
        // that's trailing but hasn't pulled back to its trailing stop is unhighlighted.
        var mustSellTriggered = p.CurrentPrice <= p.MustSellFloor();
        var trailingStopBreached = p.State == PositionState.Trailing && p.CurrentPrice <= p.TrailingStopPrice();
        var rowBackground = mustSellTriggered ? Brushes2.MustSellHighlight
            : trailingStopBreached ? Brushes2.TrailingHighlight
            : Brushes2.NoHighlight;

        return new OwnedStockRow
        {
            Id = p.Id,
            Ticker = p.Ticker,
            Quantity = p.Quantity,
            CostBasis = p.CostBasis.ToString("C"),
            CurrentPrice = p.CurrentPrice.ToString("C"),
            State = p.State.ToString(),
            UnrealizedGainLoss = $"{unrealized:C} ({unrealizedPct:F1}%)",
            UnrealizedBrush = unrealized >= 0 ? Brushes2.Gain : Brushes2.Loss,
            RealizedGainLoss = p.RealizedGainLoss == 0 ? "—" : p.RealizedGainLoss.ToString("C"),
            RealizedBrush = p.RealizedGainLoss >= 0 ? Brushes2.Gain : Brushes2.Loss,
            MustSell = p.MustSellFloor().ToString("C"),
            SellProfit = p.SellProfitTarget().ToString("C"),
            TrailingStop = p.State == PositionState.Trailing ? p.TrailingStopPrice().ToString("C") : "—",
            RowBackground = rowBackground
        };
    }
}

public class SoldStockRow
{
    public string Ticker { get; init; } = "";
    public int QuantitySold { get; init; }
    public string SaleType { get; init; } = "";
    public string BuyDate { get; init; } = "";
    public string BuyPrice { get; init; } = "";
    public string SellDate { get; init; } = "";
    public string SellPrice { get; init; } = "";
    public string ExitReason { get; init; } = "";
    public string RealizedGainLoss { get; init; } = "";
    public Brush RealizedBrush { get; init; } = Brushes2.Ink;
    public string HoldingPeriod { get; init; } = "";
    public Brush RowBackground { get; init; } = Brushes2.NoHighlight;
    public bool IsBuySignal { get; init; }

    public static SoldStockRow From(Sale s, bool isBuySignal = false)
    {
        var days = (s.SellDate - s.BuyDate).Days;
        return new SoldStockRow
        {
            Ticker = s.Ticker,
            QuantitySold = s.QuantitySold,
            SaleType = s.ClosesPosition ? "Full" : "Partial",
            BuyDate = s.BuyDate.ToString("yyyy-MM-dd"),
            BuyPrice = s.BuyPrice.ToString("C"),
            SellDate = s.SellDate.ToString("yyyy-MM-dd"),
            SellPrice = s.SellPrice.ToString("C"),
            ExitReason = s.ExitReason switch
            {
                SellSignalLedger.Core.Models.ExitReason.MustSell => "Stop-loss",
                SellSignalLedger.Core.Models.ExitReason.SellDropProfit => "Trailing-stop",
                _ => "Manual"
            },
            RealizedGainLoss = $"{s.RealizedGainLoss:C} ({s.RealizedGainLossPct:F1}%)",
            RealizedBrush = s.RealizedGainLoss >= 0 ? Brushes2.Gain : Brushes2.Loss,
            // Standard US tax convention: more than one year held is long-term.
            HoldingPeriod = $"{days}d — {(days > 365 ? "Long Term" : "Short Term")}",
            RowBackground = isBuySignal ? Brushes2.BuySignalHighlight : Brushes2.NoHighlight,
            IsBuySignal = isBuySignal
        };
    }
}

public class LedgerRow
{
    public DateTime TimestampSort { get; init; }
    public string Timestamp { get; init; } = "";
    public string Type { get; init; } = "";
    public decimal AmountSort { get; init; }
    public string Amount { get; init; } = "";
    public Brush AmountBrush { get; init; } = Brushes2.Ink;
    public decimal BalanceAfterSort { get; init; }
    public string BalanceAfter { get; init; } = "";
    public string Note { get; init; } = "";

    public static LedgerRow From(CashLedgerEntry e)
    {
        return new LedgerRow
        {
            TimestampSort = e.Timestamp,
            Timestamp = e.Timestamp.ToString("yyyy-MM-dd HH:mm"),
            Type = e.Type == CashEntryType.Sell ? "Sold" : e.Type.ToString(),
            AmountSort = e.Amount,
            Amount = e.Amount.ToString("C"),
            AmountBrush = e.Amount >= 0 ? Brushes2.Gain : Brushes2.Loss,
            BalanceAfterSort = e.BalanceAfter,
            BalanceAfter = e.BalanceAfter.ToString("C"),
            Note = e.Note ?? ""
        };
    }
}

public class AlertRow
{
    public string Kind { get; init; } = "";
    public string Ticker { get; init; } = "";
    public string Detail { get; init; } = "";
    public Brush Accent { get; init; } = Brushes2.Ink;
}
