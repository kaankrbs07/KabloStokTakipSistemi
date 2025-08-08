using System.ComponentModel.DataAnnotations;

namespace KabloStokTakipSistemi.Models;

public class Alert
{
    [Key]
    public int AlertID { get; set; }

    [MaxLength(20)]
    public string? AlertType { get; set; }

    public DateTime AlertDate { get; set; }

    [MaxLength(50)]
    public string? Color { get; set; }

    public int? MultiCableID { get; set; }

    [Required]
    public int MinQuantity { get; set; }

    [MaxLength(255)]
    public string Description { get; set; } = null!;

    [Required]
    public bool IsActive { get; set; }
}
