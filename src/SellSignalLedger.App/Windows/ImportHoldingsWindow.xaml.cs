using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using SellSignalLedger.Core.Services;

namespace SellSignalLedger.App.Windows;

public class ImportResultRow
{
    public int LineNumber { get; init; }
    public string Status { get; init; } = "";
    public string Ticker { get; init; } = "";
    public string Detail { get; init; } = "";
}

public partial class ImportHoldingsWindow : Window
{
    private List<ImportRowResult> _parsed = new();
    public bool AnyImported { get; private set; }

    public ImportHoldingsWindow()
    {
        InitializeComponent();
    }

    private void Choose_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*" };
        if (dialog.ShowDialog() != true) return;

        string text;
        try
        {
            text = File.ReadAllText(dialog.FileName);
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Couldn't read that file — it may be open in another program.\n\n{ex.Message}",
                "File in use", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show($"Couldn't read that file — access denied.\n\n{ex.Message}",
                "Access denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        FileNameText.Text = Path.GetFileName(dialog.FileName);
        _parsed = CsvImportService.Parse(text);

        ResultsGrid.ItemsSource = _parsed.Select(r => new ImportResultRow
        {
            LineNumber = r.LineNumber,
            Status = r.Success ? "OK" : "Rejected",
            Ticker = r.Row?.Ticker ?? "",
            Detail = r.Success
                ? $"{r.Row!.Quantity} sh @ {r.Row.CostBasis:C} on {r.Row.PurchaseDate:yyyy-MM-dd}"
                : r.Error ?? "Unknown error"
        }).ToList();
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var validRows = _parsed.Where(r => r.Success).Select(r => r.Row!).ToList();
        if (validRows.Count == 0)
        {
            MessageBox.Show("No valid rows to import.", "Nothing to import", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        foreach (var row in validRows)
        {
            App.Portfolio.ImportHolding(row.Ticker, row.Quantity, row.CostBasis, row.PurchaseDate,
                row.MustSellPct, row.SellProfitPct, row.SellDropProfitPct);
        }

        AnyImported = true;
        MessageBox.Show($"Imported {validRows.Count} position(s).", "Import complete", MessageBoxButton.OK, MessageBoxImage.Information);
        DialogResult = true;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = AnyImported;
    }
}
