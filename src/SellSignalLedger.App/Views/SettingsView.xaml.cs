using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace SellSignalLedger.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    public void Refresh()
    {
        var s = App.Portfolio.GetSettings();
        MustSellBox.Text = s.DefaultMustSellPct.ToString(CultureInfo.InvariantCulture);
        SellProfitBox.Text = s.DefaultSellProfitPct.ToString(CultureInfo.InvariantCulture);
        SellDropProfitBox.Text = s.DefaultSellDropProfitPct.ToString(CultureInfo.InvariantCulture);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(MustSellBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var mustSell) || mustSell <= 0 ||
            !decimal.TryParse(SellProfitBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var sellProfit) || sellProfit <= 0 ||
            !decimal.TryParse(SellDropProfitBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var sellDrop) || sellDrop <= 0)
        {
            MessageBox.Show("Trigger percentages must be positive numbers.", "Invalid input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var settings = App.Portfolio.GetSettings();
        settings.DefaultMustSellPct = mustSell;
        settings.DefaultSellProfitPct = sellProfit;
        settings.DefaultSellDropProfitPct = sellDrop;
        App.Db.SaveChanges();

        MessageBox.Show("Settings saved.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
