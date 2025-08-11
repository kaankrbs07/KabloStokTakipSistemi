using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KabloStokTakipSistemi.Models;

public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long UserID { get; set; } // numeric(10,0)

    [ MaxLength(50)] public string? FirstName { get; set; }

    [ MaxLength(50)] public string? LastName { get; set; }

    [MaxLength(100)] public string? Email { get; set; }

    [MaxLength(20)] public string? PhoneNumber { get; set; }

    [Required, MaxLength(100)] public string Password { get; set; } // Artık zorunlu

    [Required, MaxLength(10)] public string Role { get; set; }  // “Admin” veya “Employee”

    [Required] public bool IsActive { get; set; }

    // Foreign Key
    public int? DepartmentID { get; set; }

    // Navigation Properties
    public Department? Department { get; set; }

    public Admin? Admin { get; set; }
    public Employee? Employee { get; set; }
    public ICollection<Log>? Logs { get; set; }
    public ICollection<StockMovement>? StockMovements { get; set; }
}