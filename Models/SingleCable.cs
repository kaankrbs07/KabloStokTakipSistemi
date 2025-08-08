using System.ComponentModel.DataAnnotations;

namespace KabloStokTakipSistemi.Models;

public class SingleCable
{
    [Key]
    public int CableID { get; set; }

    [Required, MaxLength(50)]
    public string Color { get; set; }

    [Required]
    public bool IsActive { get; set; }

    // Nullable Foreign Key → Çoklu kabloya bağlıysa FK olacak
    public int? MultiCableID { get; set; }
    public MultipleCable? MultiCable { get; set; }
}