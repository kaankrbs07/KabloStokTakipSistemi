using Microsoft.EntityFrameworkCore;
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.DTOs.Users; 

namespace KabloStokTakipSistemi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();

    public DbSet<SingleCable> SingleCables => Set<SingleCable>();
    public DbSet<MultipleCable> MultipleCables => Set<MultipleCable>();
    public DbSet<MultiCableContent> MultiCableContents => Set<MultiCableContent>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<CableThreshold> CableThresholds => Set<CableThreshold>();
    public DbSet<ColorThreshold> ColorThresholds => Set<ColorThreshold>();

    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<Log> Logs => Set<Log>();

    // EKLENDİ: Stored Procedure çıktısı olan DTO için DbSet tanımı
    public DbSet<UserActivitySummaryDto> UserActivitySummary { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MultiCableContent>()
            .HasKey(mc => new { mc.MultiCableID, mc.SingleCableID });

        modelBuilder.Entity<Admin>()
            .HasOne(a => a.User)
            .WithOne(u => u.Admin)
            .HasForeignKey<Admin>(a => a.UserID);

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.User)
            .WithOne(u => u.Employee)
            .HasForeignKey<Employee>(e => e.UserID);

        modelBuilder.Entity<Department>()
            .HasMany(d => d.Users)
            .WithOne(u => u.Department)
            .HasForeignKey(u => u.DepartmentID)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Log>()
            .Property(l => l.LogDate)
            .HasDefaultValueSql("GETDATE()");

        modelBuilder.Entity<StockMovement>()
            .Property(sm => sm.MovementDate)
            .HasDefaultValueSql("GETDATE()");

        modelBuilder.Entity<Alert>()
            .Property(a => a.AlertID)
            .ValueGeneratedOnAdd();

        modelBuilder.Entity<ColorThreshold>()
            .HasIndex(ct => ct.Color)
            .IsUnique();

        // EKLENDİ: Keyless DTO için yapılandırma
        modelBuilder.Entity<UserActivitySummaryDto>().HasNoKey().ToView(null);
    }
}
