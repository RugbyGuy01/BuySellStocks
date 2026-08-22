using System.Net.Http;
using System.Text.Json;

namespace SellSignalLedger.Core.Services;

/// <summary>
/// Fetches end-of-day closes from Yahoo Finance's public chart endpoint (no API key).
/// One request per ticker; used once per day by the daily price cycle, never for
/// live/intraday pricing. Reads the most recent non-null close from the daily bar
/// series rather than the live quote, so it reflects the latest completed session.
/// </summary>
public class YahooFinancePriceService : IPriceService
{
    private readonly HttpClient _http;

    public YahooFinancePriceService(HttpClient? http = null)
    {
        _http = http ?? new HttpClient();
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
        {
            _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (SellSignalLedger)");
        }
    }

    public async Task<Dictionary<string, decimal>> GetLatestClosesAsync(IEnumerable<string> tickers)
    {
        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var ticker in tickers.Select(t => t.Trim().ToUpperInvariant()).Distinct())
        {
            var close = await FetchLatestClose(ticker);
            if (close.HasValue) result[ticker] = close.Value;
        }

        return result;
    }

    private async Task<decimal?> FetchLatestClose(string ticker)
    {
        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(ticker)}?range=5d&interval=1d";

        string json;
        try
        {
            json = await _http.GetStringAsync(url);
        }
        catch (HttpRequestException)
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var result = doc.RootElement.GetProperty("chart").GetProperty("result")[0];

            var closes = result.GetProperty("indicators").GetProperty("quote")[0].GetProperty("close");
            for (int i = closes.GetArrayLength() - 1; i >= 0; i--)
            {
                var element = closes[i];
                if (element.ValueKind == JsonValueKind.Number)
                {
                    return element.GetDecimal();
                }
            }

            // Fall back to the live/most-recent quoted price if no daily bar close parsed.
            if (result.GetProperty("meta").TryGetProperty("regularMarketPrice", out var live) &&
                live.ValueKind == JsonValueKind.Number)
            {
                return live.GetDecimal();
            }
        }
        catch (Exception ex) when (ex is KeyNotFoundException or IndexOutOfRangeException or InvalidOperationException or JsonException)
        {
            return null;
        }

        return null;
    }
}
