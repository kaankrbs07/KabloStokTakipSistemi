// Models/SingleCable.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KabloStokTakipSistemi.Models;

public class SingleCable
{
    [Key]
    public int CableID { get; set; }                      // PK

    [MaxLength(50)]
    public string Color { get; set; } = default!;         // nvarchar(50) not null

    public bool IsActive { get; set; } = true;            // bit not null

    public int? MultiCableID { get; set; }                // FK, nullable
    public MultiCable? MultipleCable { get; set; }     // nav 

    // Many-to-many (içerik tablosu) tarafı
    public ICollection<MultiCableContent> MultiCableContents { get; set; } = new List<MultiCableContent>();
}