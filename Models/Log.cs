using System.ComponentModel.DataAnnotations;

namespace KabloStokTakipSistemi.Models;

public class Log
{
    [Key] public int LogID { get; set; }

    public string TableName { get; set; }
    public string? Operation { get; set; }
    public string? Description { get; set; }
    public DateTime LogDate { get; set; } = DateTime.Now;

    public long UserID { get; set; }
    public User User { get; set; }
}