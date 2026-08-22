using System.Globalization;

namespace SellSignalLedger.Core.Services;

public record ImportedRow(
    string Ticker,
    int Quantity,
    decimal CostBasis,
    DateTime PurchaseDate,
    decimal? MustSellPct,
    decimal? SellProfitPct,
    decimal? SellDropProfitPct);

public record ImportRowResult(int LineNumber, bool Success, string? Error, ImportedRow? Row);

/// <summary>
/// Parses the Import Holdings CSV format: ticker, quantity, costBasis, purchaseDate
/// are required; the three trigger percentage columns are optional and fall back to
/// app defaults when blank. Each row is validated independently so one bad row does
/// not block the rest of the file.
/// </summary>
public static class CsvImportService
{
    private static readonly string[] ExpectedHeader =
        { "ticker", "quantity", "costbasis", "purchasedate", "mustsellpct", "sellprofitpct", "selldropprofitpct" };

    public static List<ImportRowResult> Parse(string csvText)
    {
        var results = new List<ImportRowResult>();
        var lines = csvText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r'))
            .ToList();

        if (lines.Count == 0) return results;

        var headerFields = lines[0].Split(',').Select(h => h.Trim().ToLowerInvariant()).ToArray();
        var headerLooksValid = headerFields.Length >= 4 &&
            headerFields[0] == ExpectedHeader[0] && headerFields[1] == ExpectedHeader[1];
        var dataStart = headerLooksValid ? 1 : 0;

        for (int i = dataStart; i < lines.Count; i++)
        {
            var lineNumber = i + 1;
            var fields = lines[i].Split(',').Select(f => f.Trim()).ToArray();

            if (fields.Length < 4)
            {
                results.Add(new ImportRowResult(lineNumber, false, "Expected at least 4 columns (ticker, quantity, costBasis, purchaseDate).", null));
                continue;
            }

            var ticker = fields[0];
            if (string.IsNullOrWhiteSpace(ticker))
            {
                results.Add(new ImportRowResult(lineNumber, false, "Missing ticker.", null));
                continue;
            }

            if (!int.TryParse(fields[1], out var quantity) || quantity <= 0)
            {
                results.Add(new ImportRowResult(lineNumber, false, $"Invalid quantity '{fields[1]}' — must be a positive whole number.", null));
                continue;
            }

            if (!decimal.TryParse(fields[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var costBasis) || costBasis <= 0)
            {
                results.Add(new ImportRowResult(lineNumber, false, $"Invalid cost basis '{fields[2]}' — must be a positive number.", null));
                continue;
            }

            if (!DateTime.TryParseExact(fields[3], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var purchaseDate))
            {
                results.Add(new ImportRowResult(lineNumber, false, $"Invalid purchase date '{fields[3]}' — expected YYYY-MM-DD.", null));
                continue;
            }

            decimal? mustSellPct = ParseOptionalPct(fields, 4);
            decimal? sellProfitPct = ParseOptionalPct(fields, 5);
            decimal? sellDropProfitPct = ParseOptionalPct(fields, 6);

            var row = new ImportedRow(ticker.ToUpperInvariant(), quantity, costBasis, purchaseDate,
                mustSellPct, sellProfitPct, sellDropProfitPct);
            results.Add(new ImportRowResult(lineNumber, true, null, row));
        }

        return results;
    }

    private static decimal? ParseOptionalPct(string[] fields, int index)
    {
        if (fields.Length <= index) return null;
        var raw = fields[index];
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : null;
    }
}
