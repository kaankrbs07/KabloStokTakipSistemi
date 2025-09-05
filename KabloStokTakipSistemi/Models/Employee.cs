using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace KabloStokTakipSistemi.Models;

[Index(nameof(UserID), IsUnique = true)] // 1-1 ilişkiyi zorlar (her User en fazla 1 employee)
public class Employee
{
    [Key]
    [Column(TypeName = "numeric(5,0)")]
    public long EmployeeID { get; set; }        // PK: numeric(5,0)

    [Required]
    [Column(TypeName = "numeric(10,0)")]
    public long UserID { get; set; }            // FK: Users(UserID)

    [ForeignKey(nameof(UserID))]
    public User? User { get; set; }
}