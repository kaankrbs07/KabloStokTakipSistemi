using System.ComponentModel.DataAnnotations;

namespace KabloStokTakipSistemi.Models;

public class MultipleCable
{
    [Key] public int MultiCableID { get; set; }

    [Required, MaxLength(100)] public string CableName { get; set; }

    public int Quantity { get; set; }

    public bool IsActive { get; set; }

    public ICollection<MultiCableContent>? Contents { get; set; }
}