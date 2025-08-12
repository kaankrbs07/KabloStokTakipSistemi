using System.ComponentModel.DataAnnotations;

namespace KabloStokTakipSistemi.Models;

public class Department
{
    [Key]
    public int DepartmentID { get; set; }

    [MaxLength(50)]
    public string? DepartmentName { get; set; }

    [Required]
    public long AdminID { get; set; }

    public DateTime CreatedAt { get; set; }
    
    public bool IsActive { get; set; }

    public Admin? Admin { get; set; } 
    public ICollection<User> Users { get; set; } = new List<User>();
}

