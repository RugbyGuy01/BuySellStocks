using System.Collections.Generic;
using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using SellSignalLedger.Core.Data;
using SellSignalLedger.Core.Services;

namespace SellSignalLedger.App;

public partial class App : Application
{
    public static AppDbContext Db { get; private set; } = null!;
    public static PortfolioService Portfolio { get; private set; } = null!;
    public static IPriceService PriceService { get; private set; } = null!;
    public static List<DailyCycleEvent> LastCycleEvents { get; set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show($"Something went wrong:\n\n{args.Exception.Message}",
                "Unexpected error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SellSignalLedger");
        Directory.CreateDirectory(dataDir);
        var dbPath = Path.Combine(dataDir, "ledger.db");

        Db = new AppDbContext(dbPath);
        Db.Database.EnsureCreated();
        EnsureTriggerAnchorColumn();

        Portfolio = new PortfolioService(Db);
        Portfolio.RecalculateOpenPositionTriggersFromLatestPartialSale();
        PriceService = new YahooFinancePriceService();

        var main = new MainWindow();
        main.Show();
    }

    private static void EnsureTriggerAnchorColumn()
    {
        var connection = Db.Database.GetDbConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(Positions)";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), "TriggerAnchorPrice", StringComparison.Ordinal)) return;
        }

        reader.Close();
        command.CommandText = "ALTER TABLE Positions ADD COLUMN TriggerAnchorPrice TEXT NOT NULL DEFAULT 0";
        command.ExecuteNonQuery();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Db?.Dispose();
        base.OnExit(e);
    }
}
