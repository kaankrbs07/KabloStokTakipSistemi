using Microsoft.EntityFrameworkCore;
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.DTOs.Users;

namespace KabloStokTakipSistemi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ===== DbSets =====
    public DbSet<User> Users => Set<User>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();

    public DbSet<SingleCable> SingleCables => Set<SingleCable>();
    public DbSet<MultiCable> MultipleCables => Set<MultiCable>();
    public DbSet<MultiCableContent> MultiCableContents => Set<MultiCableContent>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<Log> Logs => Set<Log>();

    // Eşik tabloları 
    public DbSet<ColorThreshold> ColorThresholds => Set<ColorThreshold>();
    public DbSet<CableThreshold> CableThresholds => Set<CableThreshold>();

    // Rapor SP çıktısı (keyless)
    public DbSet<UserActivitySummaryDto> UserActivitySummary => Set<UserActivitySummaryDto>();
    
    // DTO'lar için DbSet'ler
    public DbSet<GetStockMovementDto> GetStockMovementDtos => Set<GetStockMovementDto>();
    public DbSet<CreateStockMovementDto> CreateStockMovementDtos => Set<CreateStockMovementDto>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // ===== Users =====
        mb.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.UserID);
            e.Property(x => x.UserID).HasColumnType("numeric(10,0)");
            e.Property(x => x.FirstName).HasMaxLength(50);
            e.Property(x => x.LastName).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(100);
            e.Property(x => x.PhoneNumber).HasMaxLength(20);
            e.Property(x => x.DepartmentID).HasColumnType("int");
            e.Property(x => x.Role).HasMaxLength(10).IsRequired();
            e.Property(x => x.IsActive).IsRequired();
            e.Property(x => x.Password).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Email);
            e.HasIndex(x => new { x.Role, x.IsActive });
        });

        // ===== Admins =====
        mb.Entity<Admin>(e =>
        {
            e.ToTable("Admins"); 
            e.HasKey(x => x.Username);
            e.Property(x => x.DepartmentName).HasMaxLength(50).IsRequired();
            e.Property(x => x.Username).HasMaxLength(50).IsRequired();
            e.Property(x => x.UserID).HasColumnType("numeric(10,0)").IsRequired();
            e.HasOne(x => x.User)
             .WithOne(u => u.Admin)
             .HasForeignKey<Admin>(x => x.UserID)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== Employees =====
        mb.Entity<Employee>(e =>
        {
            e.ToTable("Employees");
            e.HasKey(x => x.EmployeeID);
            e.Property(x => x.EmployeeID).HasColumnType("numeric(5,0)");
            e.Property(x => x.UserID).HasColumnType("numeric(10,0)").IsRequired();
            e.HasOne(x => x.User)
             .WithOne(u => u.Employee)
             .HasForeignKey<Employee>(x => x.UserID)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== Departments =====
        mb.Entity<Department>(e =>
        {
            e.ToTable("Departments");
            e.HasKey(x => x.DepartmentID);

            e.Property(x => x.DepartmentID).HasColumnType("int");
            e.Property(x => x.DepartmentName).HasMaxLength(50);

            // AdminID artık nullable long
            e.Property(x => x.AdminID).HasColumnType("numeric(10,0)").IsRequired(false);

            e.Property(x => x.CreatedAt).IsRequired();
            e.Property(x => x.IsActive).IsRequired();

            // Users ile ilişki
            e.HasMany(x => x.Users)
                .WithOne(u => u.Department)
                .HasForeignKey(u => u.DepartmentID)
                .OnDelete(DeleteBehavior.SetNull);

            // Admin (User) ile ilişki
            e.HasOne(d => d.Admin)
                .WithMany()
                .HasForeignKey(d => d.AdminID)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => x.DepartmentName).IsUnique();
        }); 
        
        // ===== SingleCables =====
        mb.Entity<SingleCable>(e =>
        {
            e.ToTable("SingleCables");
            e.HasKey(x => x.CableID);
            e.Property(x => x.CableID).HasColumnType("int");
            e.Property(x => x.Color).HasMaxLength(50).IsRequired();
            e.Property(x => x.IsActive).IsRequired();
            e.Property(x => x.MultiCableID).HasColumnType("int");
            e.HasOne<MultiCable>()
             .WithMany()
             .HasForeignKey(x => x.MultiCableID)
             .OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.Color, x.IsActive });
        });

        // ===== MultiCableContents =====
        mb.Entity<MultiCableContent>(e =>
        {
            e.ToTable("MultiCableContents");
            e.HasKey(x => new { x.MultiCableID, x.SingleCableID });
            e.Property(x => x.MultiCableID).HasColumnType("int");
            e.Property(x => x.SingleCableID).HasColumnType("int");
            e.Property(x => x.Quantity).IsRequired();

            e.HasOne(mc => mc.MultiCable)
             .WithMany(m => m.Contents)
             .HasForeignKey(mc => mc.MultiCableID)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(mc => mc.SingleCable)
             .WithMany()
             .HasForeignKey(mc => mc.SingleCableID)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.MultiCableID);
        });

        // ===== StockMovements =====
        mb.Entity<StockMovement>(e =>
        {
            e.ToTable("StockMovements");
            e.HasKey(x => x.MovementID);
            e.Property(x => x.MovementID).HasColumnType("int");
            e.Property(x => x.CableID).HasColumnType("int").IsRequired();
            e.Property(x => x.TableName).HasMaxLength(10).IsRequired();
            e.Property(x => x.Quantity).IsRequired();
            e.Property(x => x.MovementType).HasMaxLength(10).IsRequired();
            e.Property(x => x.MovementDate).IsRequired();
            e.Property(x => x.UserID).HasColumnType("numeric(10,0)").IsRequired();
            e.Property(x => x.color).HasMaxLength(50);
            e.HasIndex(x => x.MovementDate);
            e.HasIndex(x => new { x.TableName, x.MovementType });
            e.HasIndex(x => x.UserID);
        });

        // ===== Alerts =====
        mb.Entity<Alert>(e =>
        {
            e.ToTable("Alerts");
            e.HasKey(x => x.AlertID);
            e.Property(x => x.AlertID).HasColumnType("int");
            e.Property(x => x.AlertType).HasMaxLength(20);
            e.Property(x => x.AlertDate).IsRequired();
            e.Property(x => x.Color).HasMaxLength(50);
            e.Property(x => x.MultiCableID).HasColumnType("int");
            e.Property(x => x.MinQuantity).IsRequired();
            e.Property(x => x.Description).HasMaxLength(255);
            e.Property(x => x.IsActive).IsRequired();
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => x.AlertDate);
            e.HasIndex(x => new { x.AlertType, x.IsActive });
            e.HasIndex(x => new { x.Color, x.IsActive });
            e.HasIndex(x => new { x.MultiCableID, x.IsActive });
        });

        // ===== Logs =====
        mb.Entity<Log>(e =>
        {
            e.ToTable("Logs");
            e.HasKey(x => x.LogID);
            e.Property(x => x.LogID).HasColumnType("int");
            e.Property(x => x.TableName).HasMaxLength(50).IsRequired();
            e.Property(x => x.Operation).HasMaxLength(10);
            e.Property(x => x.Description).HasMaxLength(255).IsRequired();
            e.Property(x => x.LogDate).IsRequired();
            e.Property(x => x.UserID).HasColumnType("numeric(10,0)");
            e.HasIndex(x => x.LogDate);
            e.HasIndex(x => x.UserID);
            e.HasIndex(x => new { x.TableName, x.Operation });
        });

        // ===== ColorThresholds =====
        mb.Entity<ColorThreshold>(e =>
        {
            e.ToTable("ColorThresholds");
            e.HasKey(x => x.Color);
            e.Property(x => x.Color).HasMaxLength(50).IsRequired();
            e.Property(x => x.MinQuantity).IsRequired();
        });

        // ===== CableThresholds =====
        mb.Entity<CableThreshold>(e =>
        {
            e.ToTable("CableThresholds");
            e.HasKey(x => x.MultiCableID);
            e.Property(x => x.MultiCableID).HasColumnType("int");
            e.Property(x => x.MinQuantity).IsRequired();
            e.HasOne(ct => ct.MultiCable)
             .WithOne()
             .HasForeignKey<CableThreshold>(ct => ct.MultiCableID)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== Keyless DTO (SP çıktısı) =====
        mb.Entity<UserActivitySummaryDto>(e =>
        {
            e.HasNoKey();
            e.ToView(null);
        });

        // DTO'lar için keyless entity tanımlamaları
        mb.Entity<GetStockMovementDto>(e =>
        {
            e.HasNoKey();
            e.ToView(null);
        });

        mb.Entity<CreateStockMovementDto>(e =>
        {
            e.HasNoKey();
            e.ToView(null);
        });
    }
}