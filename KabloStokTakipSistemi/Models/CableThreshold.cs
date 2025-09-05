using System.ComponentModel.DataAnnotations;

namespace KabloStokTakipSistemi.Models;

public class CableThreshold
{
    [Key,Required]
    public int MultiCableID { get; set; }

    [Required]
    public int MinQuantity { get; set; }

    public MultiCable? MultiCable { get; set; }
}
