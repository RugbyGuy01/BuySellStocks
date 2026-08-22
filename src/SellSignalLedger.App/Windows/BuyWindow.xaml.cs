using System;
using System.Globalization;
using System.Windows;
using SellSignalLedger.Core.Services;

namespace SellSignalLedger.App.Windows;

public partial class BuyWindow : Window
{
    public BuyWindow()
    {
        InitializeComponent();
        PurchaseDatePicker.SelectedDate = DateTime.Today;
        var s = App.Portfolio.GetSettings();
        MustSellBox.Text = "";
        SellProfitBox.Text = "";
        SellDropProfitBox.Text = "";
        CostPreview.Text = $"Cash available: {App.Portfolio.GetCashBalance():C}   ·   defaults: {s.DefaultMustSellPct}% / {s.DefaultSellProfitPct}% / {s.DefaultSellDropProfitPct}%";
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Buy_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = "";

        var ticker = TickerBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(ticker))
        {
            ErrorText.Text = "Enter a ticker.";
            return;
        }

        if (!int.TryParse(QuantityBox.Text, out var quantity) || quantity <= 0)
        {
            ErrorText.Text = "Quantity must be a positive whole number of shares.";
            return;
        }

        if (!decimal.TryParse(PriceBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) || price <= 0)
        {
            ErrorText.Text = "Price must be a positive number.";
            return;
        }

        if (PurchaseDatePicker.SelectedDate is not DateTime purchaseDate)
        {
            ErrorText.Text = "Select a buy date.";
            return;
        }

        decimal? mustSell = ParseOptional(MustSellBox.Text);
        decimal? sellProfit = ParseOptional(SellProfitBox.Text);
        decimal? sellDrop = ParseOptional(SellDropProfitBox.Text);

        try
        {
            App.Portfolio.Buy(ticker, quantity, price, purchaseDate, mustSell, sellProfit, sellDrop);
        }
        catch (InsufficientFundsException ex)
        {
            ErrorText.Text = ex.Message;
            return;
        }

        DialogResult = true;
    }

    private static decimal? ParseOptional(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
    }
}
