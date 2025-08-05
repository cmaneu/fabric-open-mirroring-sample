using Microsoft.EntityFrameworkCore;
using OrderManagementCLI.Model;

namespace OrderManagementCLI;
public class OrderDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<MirroringState> States { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=data/orders.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>().HasQueryFilter(o => !o.IsDeleted);
        modelBuilder.Entity<MirroringState>().HasKey(s => s.TableName);
    }

    public void InitializeAndApplySchema()
    {
        Directory.CreateDirectory("data");
        Database.EnsureCreated();
    }
}