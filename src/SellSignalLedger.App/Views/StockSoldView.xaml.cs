using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SellSignalLedger.App.Models;
using SellSignalLedger.App.Windows;

namespace SellSignalLedger.App.Views;

public partial class StockSoldView : UserControl
{
    public StockSoldView()
    {
        InitializeComponent();
    }

    public void Refresh()
    {
        var buySignalTickers = App.LastBuySignals.Select(s => s.Ticker).ToHashSet();
        var rows = App.Db.Sales
            .OrderByDescending(s => s.SellDate)
            .ToList()
            .Select(s => SoldStockRow.From(s, buySignalTickers.Contains(s.Ticker)))
            .ToList();
        Grid.ItemsSource = rows;
    }

    private void Grid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (Grid.SelectedItem is not SoldStockRow row || !row.IsBuySignal) return;

        var win = new BuyWindow(row.Ticker) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            App.LastBuySignals.RemoveAll(s => s.Ticker == row.Ticker);
            (Window.GetWindow(this) as MainWindow)?.RefreshAll();
        }
    }
}
