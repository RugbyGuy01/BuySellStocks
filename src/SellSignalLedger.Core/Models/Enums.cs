namespace SellSignalLedger.Core.Models;

public enum PositionState
{
    Watching,
    Trailing
}

public enum ExitReason
{
    MustSell,
    SellDropProfit,
    Manual
}

public enum CashEntryType
{
    Deposit,
    Withdrawal,
    Buy,
    Sell
}
