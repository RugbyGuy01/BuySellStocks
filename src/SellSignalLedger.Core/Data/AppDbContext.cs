using Microsoft.EntityFrameworkCore;
using SellSignalLedger.Core.Models;

namespace SellSignalLedger.Core.Data;

public class AppDbContext : DbContext
{
    private readonly string _dbPath;

    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<CashLedgerEntry> CashLedgerEntries => Set<CashLedgerEntry>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();

    public AppDbContext(string dbPath)
    {
        _dbPath = dbPath;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite($"Data Source={_dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Sale.PositionId is a plain historical reference, not an EF relationship —
        // a Sale must outlive its Position when the sale closes the position out,
        // and a real FK/cascade relationship can't represent that (see Sale.cs).
        modelBuilder.Entity<Sale>().Property(s => s.PositionId);

        modelBuilder.Entity<AppSettings>().HasData(new AppSettings { Id = 1 });
    }
}
