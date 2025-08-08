using System.ComponentModel.DataAnnotations;

namespace KabloStokTakipSistemi.Models;

public class Employee
{
    [Key]
    public long EmployeeID { get; set; }
    
    [Required]
    public long UserID { get; set; }
    public User? User { get; set; }
}
