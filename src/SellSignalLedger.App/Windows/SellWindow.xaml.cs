using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using SellSignalLedger.Core.Models;

namespace SellSignalLedger.App.Windows;

public class PositionOption
{
    public Guid Id { get; init; }
    public string Display { get; init; } = "";
    public int Quantity { get; init; }
    public decimal CostBasis { get; init; }
    public decimal CurrentPrice { get; init; }
}

public partial class SellWindow : Window
{
    public SellWindow(Guid? preselectPositionId = null)
    {
        InitializeComponent();
        SellDatePicker.SelectedDate = DateTime.Today;

        var options = App.Db.Positions.ToList().Select(p => new PositionOption
        {
            Id = p.Id,
            Display = $"{p.Ticker} — {p.Quantity} sh @ {p.CostBasis:C} (now {p.CurrentPrice:C})",
            Quantity = p.Quantity,
            CostBasis = p.CostBasis,
            CurrentPrice = p.CurrentPrice
        }).ToList();

        PositionCombo.ItemsSource = options;

        var preselectIndex = preselectPositionId.HasValue
            ? options.FindIndex(o => o.Id == preselectPositionId.Value)
            : -1;
        PositionCombo.SelectedIndex = preselectIndex >= 0 ? preselectIndex : (options.Count > 0 ? 0 : -1);
    }

    private void PositionCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (PositionCombo.SelectedItem is PositionOption opt)
        {
            HoldingInfo.Text = $"{opt.Quantity} shares held. Selling fewer than all shares leaves the remainder open under the same triggers.";
            PriceBox.Text = opt.CurrentPrice.ToString("F2", CultureInfo.InvariantCulture);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Sell_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = "";

        if (PositionCombo.SelectedItem is not PositionOption opt)
        {
            ErrorText.Text = "Select a position to sell.";
            return;
        }

        if (!int.TryParse(QuantityBox.Text, out var quantity) || quantity <= 0 || quantity > opt.Quantity)
        {
            ErrorText.Text = $"Quantity must be between 1 and {opt.Quantity}.";
            return;
        }

        if (!decimal.TryParse(PriceBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) || price <= 0)
        {
            ErrorText.Text = "Price must be a positive number.";
            return;
        }

        if (SellDatePicker.SelectedDate is not DateTime sellDate)
        {
            ErrorText.Text = "Select a sell date.";
            return;
        }

        App.Portfolio.ManualSell(opt.Id, quantity, price, sellDate);
        DialogResult = true;
    }
}
