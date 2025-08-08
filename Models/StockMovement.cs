using System.ComponentModel.DataAnnotations;

namespace KabloStokTakipSistemi.Models;

public class StockMovement
{
    [Key]
    public int MovementID { get; set; }

    public int CableID { get; set; }

    [MaxLength(10)]
    public string TableName { get; set; } // "Single" veya "Multi"

    public int Quantity { get; set; }

    [MaxLength(10)]
    public string MovementType { get; set; } // "Giriş" / "Çıkış"

    public DateTime MovementDate { get; set; } = DateTime.Now;

    public long UserID { get; set; }
    public User User { get; set; }
}
