using System.ComponentModel.DataAnnotations;

namespace KabloStokTakipSistemi.Models;

public class ColorThreshold
{
    [Key]
    [MaxLength(50),Required]
    public string Color { get; set; }

    [Required]
    public int MinQuantity { get; set; }
}
