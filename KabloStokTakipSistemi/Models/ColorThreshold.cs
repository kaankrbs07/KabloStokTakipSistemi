using System.ComponentModel.DataAnnotations;

namespace KabloStokTakipSistemi.Models;

public class ColorThreshold
{
    [Key]
    [MaxLength(50)]
    public required string Color { get; set; }

    [Required]
    public int MinQuantity { get; set; }
}
