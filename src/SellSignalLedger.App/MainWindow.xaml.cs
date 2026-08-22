using System;
using System.Windows;
using SellSignalLedger.App.Windows;

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

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshButton.IsEnabled = false;
        RefreshButton.Content = "Refreshing...";
        try
        {
            var events = await App.Portfolio.RunDailyPriceCycle(App.PriceService, DateTime.Now);
            App.LastCycleEvents = events;
            RefreshAll();
        }
        finally
        {
            RefreshButton.IsEnabled = true;
            RefreshButton.Content = "Refresh Prices Now";
        }
    }
}
