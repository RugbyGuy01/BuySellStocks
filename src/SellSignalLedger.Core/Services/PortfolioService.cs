using Microsoft.EntityFrameworkCore;
using SellSignalLedger.Core.Data;
using SellSignalLedger.Core.Models;

namespace SellSignalLedger.Core.Services;

public class InsufficientFundsException : Exception
{
    public InsufficientFundsException(string message) : base(message) { }
}

public record DailyCycleEvent(TriggerOutcome Outcome, Position Position, decimal Price);

public class PortfolioService
{
    private readonly AppDbContext _db;

    public PortfolioService(AppDbContext db)
    {
        _db = db;
    }

    public decimal GetCashBalance()
    {
        var last = _db.CashLedgerEntries.OrderByDescending(e => e.Timestamp).FirstOrDefault();
        return last?.BalanceAfter ?? 0m;
    }

    public AppSettings GetSettings() => _db.AppSettings.First();

    private CashLedgerEntry RecordCashEntry(CashEntryType type, decimal amount, Guid? positionId, string? note)
    {
        var newBalance = GetCashBalance() + amount;
        var entry = new CashLedgerEntry
        {
            Type = type,
            Amount = amount,
            BalanceAfter = newBalance,
            RelatedPositionId = positionId,
            Note = note
        };
        _db.CashLedgerEntries.Add(entry);
        return entry;
    }

    public void Deposit(decimal amount, string? note = null)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Deposit must be positive.");
        RecordCashEntry(CashEntryType.Deposit, amount, null, note);
        _db.SaveChanges();
    }

    public void Withdraw(decimal amount, string? note = null)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Withdrawal must be positive.");
        if (amount > GetCashBalance())
            throw new InsufficientFundsException("Withdrawal exceeds available cash balance.");
        RecordCashEntry(CashEntryType.Withdrawal, -amount, null, note);
        _db.SaveChanges();
    }

    public Position Buy(string ticker, int quantity, decimal price, DateTime purchaseDate,
        decimal? mustSellPct = null, decimal? sellProfitPct = null, decimal? sellDropProfitPct = null)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Whole shares only, must be positive.");
        if (price <= 0) throw new ArgumentOutOfRangeException(nameof(price));

        var cost = quantity * price;
        if (cost > GetCashBalance())
            throw new InsufficientFundsException("Insufficient cash balance for this buy.");

        var settings = GetSettings();
        var position = new Position
        {
            Ticker = ticker.ToUpperInvariant(),
            Quantity = quantity,
            CostBasis = price,
            PeakPrice = price,
            CurrentPrice = price,
            TriggerAnchorPrice = price,
            State = PositionState.Watching,
            MustSellPct = mustSellPct ?? settings.DefaultMustSellPct,
            SellProfitPct = sellProfitPct ?? settings.DefaultSellProfitPct,
            SellDropProfitPct = sellDropProfitPct ?? settings.DefaultSellDropProfitPct,
            PurchaseDate = purchaseDate
        };

        _db.Positions.Add(position);
        RecordCashEntry(CashEntryType.Buy, -cost, position.Id, $"Buy {quantity} {position.Ticker} @ {price:C}");
        _db.SaveChanges();
        return position;
    }

    /// <summary>
    /// Imports a pre-existing holding without touching the cash ledger — these shares
    /// were not purchased through the app.
    /// </summary>
    public Position ImportHolding(string ticker, int quantity, decimal costBasis, DateTime purchaseDate,
        decimal? mustSellPct, decimal? sellProfitPct, decimal? sellDropProfitPct)
    {
        var settings = GetSettings();
        var position = new Position
        {
            Ticker = ticker.ToUpperInvariant(),
            Quantity = quantity,
            CostBasis = costBasis,
            PeakPrice = costBasis,
            CurrentPrice = costBasis,
            TriggerAnchorPrice = costBasis,
            State = PositionState.Watching,
            MustSellPct = mustSellPct ?? settings.DefaultMustSellPct,
            SellProfitPct = sellProfitPct ?? settings.DefaultSellProfitPct,
            SellDropProfitPct = sellDropProfitPct ?? settings.DefaultSellDropProfitPct,
            PurchaseDate = purchaseDate
        };
        _db.Positions.Add(position);
        _db.SaveChanges();
        return position;
    }

    /// <summary>
    /// Manual sell, full or partial — the only way a position is ever closed.
    /// Automatic triggers (Must Sell, Trailing Stop) only flag a position; they
    /// never sell it.
    /// </summary>
    public Sale ManualSell(Guid positionId, int quantitySold, decimal price, DateTime sellDate)
    {
        var position = _db.Positions.First(p => p.Id == positionId);
        if (quantitySold <= 0 || quantitySold > position.Quantity)
            throw new ArgumentOutOfRangeException(nameof(quantitySold), "Quantity must be between 1 and the position's current shares.");

        var closes = quantitySold == position.Quantity;
        var sale = BuildSale(position, quantitySold, price, sellDate, ExitReason.Manual, closes);

        // The sale price is the most recent real quote for this ticker — reflect it
        // immediately in Owned Stocks rather than waiting for the next price refresh.
        position.CurrentPrice = price;

        ApplySaleToPosition(position, sale);

        if (!sale.ClosesPosition)
        {
            position.TriggerAnchorPrice = price;
            position.PeakPrice = price;
            position.State = PositionState.Watching;
            TriggerEngine.Apply(position, price);
        }

        RecordCashEntry(CashEntryType.Sell, quantitySold * price, position.Id,
            $"Sell {quantitySold} {position.Ticker} @ {price:C}");

        _db.SaveChanges();
        return sale;
    }

    public void RecalculateOpenPositionTriggersFromLatestPartialSale()
    {
        var partialSales = _db.Sales
            .Where(s => !s.ClosesPosition)
            .GroupBy(s => s.PositionId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.SellDate).First());

        foreach (var position in _db.Positions)
        {
            if (!partialSales.TryGetValue(position.Id, out var sale)) continue;

            position.TriggerAnchorPrice = sale.SellPrice;
            position.PeakPrice = sale.SellPrice;
            position.State = PositionState.Watching;
            TriggerEngine.Apply(position, position.CurrentPrice);
        }

        _db.SaveChanges();
    }

    private Sale BuildSale(Position position, int quantitySold, decimal price, DateTime sellDate,
        ExitReason reason, bool closes)
    {
        var realized = (price - position.CostBasis) * quantitySold;
        var realizedPct = position.CostBasis == 0 ? 0 : (price - position.CostBasis) / position.CostBasis * 100m;

        return new Sale
        {
            PositionId = position.Id,
            Ticker = position.Ticker,
            BuyPrice = position.CostBasis,
            SellPrice = price,
            QuantitySold = quantitySold,
            ClosesPosition = closes,
            ExitReason = reason,
            RealizedGainLoss = realized,
            RealizedGainLossPct = realizedPct,
            BuyDate = position.PurchaseDate,
            SellDate = sellDate
        };
    }

    private void ApplySaleToPosition(Position position, Sale sale)
    {
        _db.Sales.Add(sale);
        position.Quantity -= sale.QuantitySold;

        if (sale.ClosesPosition)
        {
            _db.Positions.Remove(position);
        }
        else
        {
            position.RealizedGainLoss += sale.RealizedGainLoss;
        }
    }

    /// <summary>
    /// Runs the daily EOD price refresh: fetches the latest close for every open
    /// position and applies the ratchet trigger logic. Triggers are alerts only —
    /// no position is ever sold or removed automatically. Must Sell and Trailing
    /// Stop breaches are reported back as events so the UI can flag them; selling
    /// only happens when the user does it manually.
    /// </summary>
    public async Task<List<DailyCycleEvent>> RunDailyPriceCycle(IPriceService priceService, DateTime asOf)
    {
        var events = new List<DailyCycleEvent>();
        var positions = _db.Positions.ToList();

        GetSettings().LastPriceRefresh = asOf;

        if (positions.Count == 0)
        {
            _db.SaveChanges();
            return events;
        }

        var tickers = positions.Select(p => p.Ticker).Distinct();
        var closes = await priceService.GetLatestClosesAsync(tickers);

        foreach (var position in positions)
        {
            if (!closes.TryGetValue(position.Ticker, out var price)) continue;

            var result = TriggerEngine.Apply(position, price);
            events.Add(new DailyCycleEvent(result.Outcome, position, price));
        }

        _db.SaveChanges();
        return events;
    }
}
