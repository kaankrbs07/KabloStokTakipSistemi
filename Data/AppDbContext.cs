using Microsoft.EntityFrameworkCore;
using KabloStokTakipSistemi.Models;
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
    public DbSet<ColorThreshold> ColorThresholds => Set<ColorThreshold>(); // PK = Color
    public DbSet<CableThreshold> CableThresholds => Set<CableThreshold>(); // PK = MultiCableID

    // Rapor SP çıktısı (keyless)
    public DbSet<UserActivitySummaryDto> UserActivitySummary => Set<UserActivitySummaryDto>();
    

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // ===== Users / Admins / Employees / Departments =====
        mb.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.UserID);
            e.Property(x => x.FirstName).HasMaxLength(50).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(50).IsRequired();
            e.Property(x => x.Email).HasMaxLength(100);
            e.Property(x => x.PhoneNumber).HasMaxLength(20);
            e.Property(x => x.Role).HasMaxLength(10).IsRequired();
            e.Property(x => x.IsActive).IsRequired();
            e.HasIndex(x => x.Email);
            e.HasIndex(x => new { x.Role, x.IsActive });
        });

        mb.Entity<Admin>(e =>
        {
            e.ToTable("Admins");
            e.HasKey(x => x.AdminID);
            e.HasOne(x => x.User)
             .WithOne(u => u.Admin)
             .HasForeignKey<Admin>(x => x.UserID)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Employee>(e =>
        {
            e.ToTable("Employees");
            e.HasKey(x => x.EmployeeID);
            e.HasOne(x => x.User)
             .WithOne(u => u.Employee)
             .HasForeignKey<Employee>(x => x.UserID)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Department>(e =>
        {
            e.ToTable("Departments");
            e.HasKey(x => x.DepartmentID);
            e.Property(x => x.DepartmentName).HasMaxLength(100).IsRequired();
            e.HasMany(x => x.Users)
             .WithOne(u => u.Department)
             .HasForeignKey(u => u.DepartmentID)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.DepartmentName).IsUnique();
        });

        // ===== Cables =====
        mb.Entity<MultiCable>(e =>
        {
            e.ToTable("MultipleCables");
            e.HasKey(x => x.MultiCableID);
            e.Property(x => x.CableName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Quantity).IsRequired(); // stok adedi
            e.HasIndex(x => x.CableName);
            e.HasIndex(x => x.Quantity);
        });

        mb.Entity<SingleCable>(e =>
        {
            e.ToTable("SingleCables");
            e.HasKey(x => x.CableID);
            e.Property(x => x.Color).HasMaxLength(50).IsRequired();
            e.Property(x => x.IsActive).IsRequired();
            // (opsiyonel) parent çoklu kablo ilişkisi
            e.HasOne<MultiCable>()
             .WithMany()
             .HasForeignKey(x => x.MultiCableID)
             .OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.Color, x.IsActive });
        });

        mb.Entity<MultiCableContent>(e =>
        {
            e.ToTable("MultiCableContents");
            e.HasKey(x => new { x.MultiCableID, x.SingleCableID }); // bileşik PK
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

        // ===== Stock Movements =====
        mb.Entity<StockMovement>(e =>
        {
            e.ToTable("StockMovements");
            e.HasKey(x => x.MovementID);
            e.Property(x => x.TableName).HasMaxLength(10).IsRequired();     // 'Single' | 'Multi'
            e.Property(x => x.MovementType).HasMaxLength(10).IsRequired();  // 'Giriş' | 'Çıkış'
            e.Property(x => x.Quantity).IsRequired();
            e.Property(x => x.MovementDate).HasDefaultValueSql("GETDATE()").IsRequired();
            // FK tanımlamıyoruz; doğrulama SP/trigger’da
            e.HasIndex(x => x.MovementDate);
            e.HasIndex(x => new { x.TableName, x.MovementType });
            e.HasIndex(x => x.UserID);
        });

        // ===== Thresholds =====
        // ColorThresholds: PK = Color, MinQuantity zorunlu
        mb.Entity<ColorThreshold>(e =>
        {
            e.ToTable("ColorThresholds");
            e.HasKey(x => x.Color);
            e.Property(x => x.Color).HasMaxLength(50).IsRequired();
            e.Property(x => x.MinQuantity).IsRequired();
        });

        // CableThresholds: PK = MultiCableID, MinQuantity zorunlu
        mb.Entity<CableThreshold>(e =>
        {
            e.ToTable("CableThresholds");
            e.HasKey(x => x.MultiCableID);
            e.Property(x => x.MinQuantity).IsRequired();

            e.HasOne(ct => ct.MultiCable)
             .WithOne()
             .HasForeignKey<CableThreshold>(ct => ct.MultiCableID)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== Alerts =====
        mb.Entity<Alert>(e =>
        {
            e.ToTable("Alerts");
            e.HasKey(x => x.AlertID);
            e.Property(x => x.AlertType).HasMaxLength(20);
            e.Property(x => x.Color).HasMaxLength(50);
            e.Property(x => x.Description).HasMaxLength(255).IsRequired();
            e.Property(x => x.MinQuantity).IsRequired();
            e.Property(x => x.IsActive).IsRequired();
            e.Property(x => x.AlertDate).IsRequired();

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
            e.Property(x => x.TableName).HasMaxLength(50);
            e.Property(x => x.Operation).HasMaxLength(10).IsRequired(); 
            e.Property(x => x.Description).HasMaxLength(255).IsRequired();
            e.Property(x => x.LogDate).HasDefaultValueSql("GETDATE()").IsRequired();
            e.Property(x => x.UserID).IsRequired();
            e.HasIndex(x => x.LogDate);
            e.HasIndex(x => x.UserID);
            e.HasIndex(x => new { x.TableName, x.Operation });
        });

        // ===== Keyless DTO (SP çıktısı) =====
        mb.Entity<UserActivitySummaryDto>(e =>
        {
            e.HasNoKey();
            e.ToView(null); // gerçek view yok; FromSql/EXEC ile beslenecek
            // SP kolon isimleri birebir olduğu için ek ColumnName vermeye gerek yok. 
        });
    }
}


