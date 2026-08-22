using SellSignalLedger.Core.Models;

namespace SellSignalLedger.Core.Services;

public enum TriggerOutcome
{
    None,
    EnteredTrailing,
    MustSellTriggered,
    TrailingStopTriggered
}

public record TriggerResult(TriggerOutcome Outcome, Position Position);

/// <summary>
/// Applies the ratcheting stop-loss / take-profit / trailing-stop mechanism to a
/// position given a new price. A position only ever moves forward through its
/// states (Watching -> Trailing), never backward, and every new high re-tightens
/// both the stop-loss floor and the trailing-stop trigger around it.
///
/// Triggers are alerts, not automatic sales: a position is never closed or removed
/// by this engine. Must Sell and Trailing Stop breaches are reported as outcomes so
/// the UI can flag the position (red / yellow); the user decides whether to act on
/// it through a manual sell.
/// </summary>
public static class TriggerEngine
{
    public static TriggerResult Apply(Position position, decimal newPrice)
    {
        position.CurrentPrice = newPrice;

        if (newPrice > position.PeakPrice)
        {
            position.PeakPrice = newPrice;
        }

        // Stop-loss floor always tracks the peak once trailing; otherwise the cost basis.
        if (newPrice <= position.MustSellFloor())
        {
            return new TriggerResult(TriggerOutcome.MustSellTriggered, position);
        }

        if (position.State == PositionState.Watching)
        {
            if (newPrice >= position.SellProfitTarget())
            {
                position.State = PositionState.Trailing;
                return new TriggerResult(TriggerOutcome.EnteredTrailing, position);
            }

            return new TriggerResult(TriggerOutcome.None, position);
        }

        // Trailing state: check the trailing stop against the (possibly just-updated) peak.
        if (newPrice <= position.TrailingStopPrice())
        {
            return new TriggerResult(TriggerOutcome.TrailingStopTriggered, position);
        }

        return new TriggerResult(TriggerOutcome.None, position);
    }
}
