namespace SellSignalLedger.Core.Models;

public class CashLedgerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public CashEntryType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public Guid? RelatedPositionId { get; set; }
    public string? Note { get; set; }
}
