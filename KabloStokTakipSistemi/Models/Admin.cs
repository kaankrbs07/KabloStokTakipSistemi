// Models/Admin.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace KabloStokTakipSistemi.Models;

[Index(nameof(UserID), IsUnique = true)]
public class Admin
{
    [Required, MaxLength(50)]
    public string DepartmentName { get; set; } = null!; // yeni sütun

    [Required, MaxLength(50),Key]
    public string Username { get; set; } = null!;

    [Required]
    [Column(TypeName = "numeric(10,0)")]
    public long UserID { get; set; }                  // FK Users(UserID)

    [ForeignKey(nameof(UserID))]
    public User? User { get; set; }
}