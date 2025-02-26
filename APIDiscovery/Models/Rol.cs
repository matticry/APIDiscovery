using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDiscovery.Models;

[Table("tbl_rol")]
public class Rol
{
    [Key]
    public int id_rol { get; set; }
        
    [Required]
    [MaxLength(250)]
    public string name_rol { get; set; } = "User";
        
    public char status_rol { get; set; } = 'A';
}