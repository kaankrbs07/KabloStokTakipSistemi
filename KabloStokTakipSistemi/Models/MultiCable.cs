// Models/MultipleCable.cs
using System.ComponentModel.DataAnnotations;

namespace KabloStokTakipSistemi.Models;

public class MultiCable
{
    [Key]
    public int MultiCableID { get; set; }                 // PK

    [MaxLength(100)]
    public string CableName { get; set; } = default!;     // nvarchar(100) not null

    public int Quantity { get; set; }                     // not null
    public bool IsActive { get; set; } = true;            // not null

    // Navigation
    public ICollection<MultiCableContent> Contents { get; set; } = new List<MultiCableContent>();
    public ICollection<SingleCable> SingleCables { get; set; } = new List<SingleCable>();
}