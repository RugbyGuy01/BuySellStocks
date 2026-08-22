using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SellSignalLedger.App.Models;
using SellSignalLedger.App.Windows;

namespace SellSignalLedger.App.Views;

public partial class OwnedStocksView : UserControl
{
    public OwnedStocksView()
    {
        InitializeComponent();
    }

    public void Refresh()
    {
        var rows = App.Db.Positions.ToList().Select(OwnedStockRow.From).ToList();
        Grid.ItemsSource = rows;
    }

    private void Grid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (Grid.SelectedItem is not OwnedStockRow row) return;

        var win = new SellWindow(row.Id) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            (Window.GetWindow(this) as MainWindow)?.RefreshAll();
        }
    }
}
