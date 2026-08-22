namespace SellSignalLedger.Core.Services;

public interface IPriceService
{
    /// <summary>Fetches the latest available end-of-day close for each ticker.</summary>
    Task<Dictionary<string, decimal>> GetLatestClosesAsync(IEnumerable<string> tickers);
}
