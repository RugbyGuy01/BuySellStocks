using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using SellSignalLedger.App.Models;
using SellSignalLedger.Core.Services;

namespace SellSignalLedger.App.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();

        // Assembly.Location is empty in a single-file publish, so prefer the running
        // process's own exe path (works for both `dotnet run` and the published exe).
        var modulePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
        var assemblyPath = Assembly.GetExecutingAssembly().Location;
        var path = !string.IsNullOrEmpty(modulePath) && File.Exists(modulePath) ? modulePath
            : (File.Exists(assemblyPath) ? assemblyPath : null);

        BuildDateText.Text = path != null
            ? $"Build {File.GetLastWriteTime(path):yyyy-MM-dd HH:mm}"
            : "Build date unavailable";
    }

    public void Refresh()
    {
        var cash = App.Portfolio.GetCashBalance();
        var positions = App.Db.Positions.ToList();
        var holdingsValue = positions.Sum(p => p.CurrentPrice * p.Quantity);

        CashText.Text = cash.ToString("C");
        HoldingsText.Text = holdingsValue.ToString("C");
        TotalText.Text = (cash + holdingsValue).ToString("C");

        var lastRefresh = App.Portfolio.GetSettings().LastPriceRefresh;
        LastRefreshText.Text = lastRefresh.HasValue
            ? $"Prices last updated {lastRefresh.Value:yyyy-MM-dd HH:mm}"
            : "Prices never updated — click Refresh Prices Now";

        var rows = App.LastCycleEvents.Select(ev => ev.Outcome switch
        {
            TriggerOutcome.MustSellTriggered => new AlertRow
            {
                Kind = "MUST SELL",
                Ticker = ev.Position.Ticker,
                Detail = $"At {ev.Price:C} — below the Must Sell floor. Flagged red in Owned Stocks; not sold automatically.",
                Accent = Brushes2.Loss
            },
            TriggerOutcome.EnteredTrailing => new AlertRow
            {
                Kind = "ENTERED TRAILING",
                Ticker = ev.Position.Ticker,
                Detail = $"Sell Profit target hit at {ev.Price:C} — floor and trailing stop re-anchored.",
                Accent = Brushes2.Accent
            },
            TriggerOutcome.TrailingStopTriggered => new AlertRow
            {
                Kind = "TRAILING STOP",
                Ticker = ev.Position.Ticker,
                Detail = $"At {ev.Price:C} — pulled back from peak. Flagged yellow in Owned Stocks; not sold automatically.",
                Accent = Brushes2.Accent
            },
            _ => null
        }).Where(r => r != null).ToList();

        AlertsList.ItemsSource = rows;
    }
}
