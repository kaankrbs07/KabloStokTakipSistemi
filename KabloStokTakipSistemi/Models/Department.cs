﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KabloStokTakipSistemi.Models;

public class Department
{
    [Key]
    public int DepartmentID { get; set; }

    [MaxLength(50)]
    public string? DepartmentName { get; set; }

    // FK -> Users(UserID), nullable
    [Column(TypeName = "numeric(10,0)")]
    public long? AdminID { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }

    // Navigation property
    [ForeignKey(nameof(AdminID))]
    public User? Admin { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}

