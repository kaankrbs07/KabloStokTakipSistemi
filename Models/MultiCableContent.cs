using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KabloStokTakipSistemi.Models;

public class MultiCableContent
{
    [Key, Column(Order = 0)]
    public int MultiCableID { get; set; }

    [Key, Column(Order = 1)]
    public int SingleCableID { get; set; }

    public int Quantity { get; set; }

    public MultipleCable MultiCable { get; set; }
    public SingleCable SingleCable { get; set; }
}
