using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using SellSignalLedger.App.Windows;
using SellSignalLedger.Core.Services;

namespace SellSignalLedger.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshAll();
    }

    public void RefreshAll()
    {
        DashboardTab.Refresh();
        OwnedStocksTab.Refresh();
        StockSoldTab.Refresh();
        BankingTab.Refresh();
        SettingsTab.Refresh();
    }

    private void BuyButton_Click(object sender, RoutedEventArgs e)
    {
        var win = new BuyWindow { Owner = this };
        if (win.ShowDialog() == true) RefreshAll();
    }

    private void SellButton_Click(object sender, RoutedEventArgs e)
    {
        var win = new SellWindow { Owner = this };
        if (win.ShowDialog() == true) RefreshAll();
    }

    private void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var win = new ImportHoldingsWindow { Owner = this };
        if (win.ShowDialog() == true) RefreshAll();
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var positions = App.Db.Positions.OrderBy(p => p.Ticker).ToList();
        if (positions.Count == 0)
        {
            MessageBox.Show("No holdings to export.", "Nothing to export", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            FileName = $"holdings-{DateTime.Now:yyyy-MM-dd}.csv"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            File.WriteAllText(dialog.FileName, CsvExportService.Export(positions));
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Couldn't write that file — it may be open in another program.\n\n{ex.Message}",
                "File in use", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show($"Couldn't write that file — access denied.\n\n{ex.Message}",
                "Access denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        MessageBox.Show($"Exported {positions.Count} position(s) to {Path.GetFileName(dialog.FileName)}.",
            "Export complete", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshButton.IsEnabled = false;
        RefreshButton.Content = "Refreshing...";
        try
        {
            var events = await App.Portfolio.RunDailyPriceCycle(App.PriceService, DateTime.Now);
            App.LastCycleEvents = events;
            App.LastBuySignals = App.Portfolio.LastBuySignals;
            RefreshAll();
        }
        finally
        {
            RefreshButton.IsEnabled = true;
            RefreshButton.Content = "Refresh Prices Now";
        }
    }
}
