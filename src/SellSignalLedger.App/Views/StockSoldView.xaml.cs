using System.Linq;
using System.Windows.Controls;
using SellSignalLedger.App.Models;

namespace SellSignalLedger.App.Views;

public partial class StockSoldView : UserControl
{
    public StockSoldView()
    {
        InitializeComponent();
    }

    public void Refresh()
    {
        var rows = App.Db.Sales
            .OrderByDescending(s => s.SellDate)
            .ToList()
            .Select(SoldStockRow.From)
            .ToList();
        Grid.ItemsSource = rows;
    }
}
