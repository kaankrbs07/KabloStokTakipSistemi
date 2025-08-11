using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KabloStokTakipSistemi.Models;

public class Admin
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long AdminID { get; set; }

    [Required, MaxLength(50)]
    public string Username { get; set; }

    [Required]
    public long UserID { get; set; }
    public User? User { get; set; }
}