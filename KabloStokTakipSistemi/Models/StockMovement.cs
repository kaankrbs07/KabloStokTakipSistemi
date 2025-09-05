using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KabloStokTakipSistemi.Models;

public class StockMovement
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MovementID { get; set; }                  // PK (int, identity)

    [Required]
    public int CableID { get; set; }                     // int, not null

    [Required, MaxLength(10)]
    [Column(TypeName = "nvarchar(10)")]
    public string TableName { get; set; } = null!;       // "Single" | "Multi"

    [Required]
    public int Quantity { get; set; }                    // int, not null

    [Required, MaxLength(10)]
    [Column(TypeName = "nvarchar(10)")]
    public string MovementType { get; set; } = null!;    // "Giriş" | "Çıkış"

    // DB tarafında DEFAULT (GETDATE()) 
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime MovementDate { get; set; }           // datetime, not null

    [Required]
    [Column(TypeName = "numeric(10,0)")]
    public long UserID { get; set; }                     // FK -> Users(UserID)
     
    [MaxLength(50)]
    public string? color { get; set; }

    [ForeignKey(nameof(UserID))]
    public User? User { get; set; }                      // navigation (nullable)
}


