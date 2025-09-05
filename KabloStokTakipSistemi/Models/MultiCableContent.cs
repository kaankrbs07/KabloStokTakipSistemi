// Models/MultiCableContent.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace KabloStokTakipSistemi.Models;

public class MultiCableContent
{
    // Composite Key: MultiCableID + SingleCableID (DbContext'te tanımlanacak)
    public int MultiCableID { get; set; }                 // FK -> MultipleCables
    public int SingleCableID { get; set; }                // FK -> SingleCables

    public int Quantity { get; set; }                     // not null

    public MultiCable MultiCable { get; set; } = default!;
    public SingleCable SingleCable { get; set; } = default!;
}

