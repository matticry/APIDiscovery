using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDiscovery.Models;

[Table("tbl_user")]
public class Usuario
{
    [Key]
    public int id_us { get; set; }
        
    [Required]
    [MaxLength(250)]
    public string name_us { get; set; }
        
    [Required]
    [MaxLength(250)]
    public string lastname_us { get; set; }
        
    [MaxLength(250)]
    public string email_us { get; set; }
        
    [Required]
    [MaxLength(250)]
    public string password_us { get; set; }
        
    public DateTime created_at { get; set; } = DateTime.Now;
        
    public int? google_id { get; set; }
        
    public int id_rol { get; set; }
        
    [ForeignKey("id_rol")]
    
    public Rol Rol { get; set; }
        
    public int id_empresa { get; set; }
        
    [ForeignKey("id_empresa")]
    public Empresa Empresa { get; set; }
        
    [MaxLength(10)]
    public string dni_us { get; set; }
        
    [MaxLength(250)]
    public string image_us { get; set; }
}