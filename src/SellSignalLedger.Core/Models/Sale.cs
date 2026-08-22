namespace SellSignalLedger.Core.Models;

public class Sale
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The position this sale was made from, kept as a plain historical
    /// reference — not an enforced foreign key. A Sale is a permanent record that
    /// must survive its Position being deleted when the sale closes it out; an
    /// EF-tracked relationship here previously caused the cascade delete to silently
    /// remove the Sale in the same transaction that closed the Position.</summary>
    public Guid PositionId { get; set; }

    public string Ticker { get; set; } = string.Empty;
    public decimal BuyPrice { get; set; }
    public decimal SellPrice { get; set; }
    public int QuantitySold { get; set; }
    public bool ClosesPosition { get; set; }
    public ExitReason ExitReason { get; set; }
    public decimal RealizedGainLoss { get; set; }
    public decimal RealizedGainLossPct { get; set; }
    public DateTime BuyDate { get; set; }
    public DateTime SellDate { get; set; }
}
