using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SellSignalLedger.App.Models;
using SellSignalLedger.Core.Services;

namespace SellSignalLedger.App.Views;

public partial class BankingView : UserControl
{
    public BankingView()
    {
        InitializeComponent();
    }

    public void Refresh()
    {
        BalanceText.Text = App.Portfolio.GetCashBalance().ToString("C");
        var rows = App.Db.CashLedgerEntries
            .OrderByDescending(e => e.Timestamp)
            .ToList()
            .Select(LedgerRow.From)
            .ToList();
        Grid.ItemsSource = rows;

        foreach (var column in Grid.Columns) column.SortDirection = null;
        var whenColumn = Grid.Columns[0];
        whenColumn.SortDirection = System.ComponentModel.ListSortDirection.Descending;
        Grid.Items.SortDescriptions.Clear();
        Grid.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("TimestampSort", System.ComponentModel.ListSortDirection.Descending));
    }

    private void Deposit_Click(object sender, RoutedEventArgs e) => DoTransfer(isDeposit: true);
    private void Withdraw_Click(object sender, RoutedEventArgs e) => DoTransfer(isDeposit: false);

    private void DoTransfer(bool isDeposit)
    {
        if (!decimal.TryParse(AmountBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            MessageBox.Show("Enter a positive amount.", "Invalid amount", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var note = string.IsNullOrWhiteSpace(NoteBox.Text) ? null : NoteBox.Text;

        try
        {
            if (isDeposit) App.Portfolio.Deposit(amount, note);
            else App.Portfolio.Withdraw(amount, note);
        }
        catch (InsufficientFundsException ex)
        {
            MessageBox.Show(ex.Message, "Insufficient funds", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        AmountBox.Text = "";
        NoteBox.Text = "";
        (Window.GetWindow(this) as MainWindow)?.RefreshAll();
    }
}
