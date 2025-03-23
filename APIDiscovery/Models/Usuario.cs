using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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
    public DateTime update_at { get; set; } = DateTime.Now;
        
    public int? google_id { get; set; }
        
    public int id_rol { get; set; }
        
    [ForeignKey("id_rol")]
    [JsonIgnore]
    public Rol Rol { get; set; }
        
    [MaxLength(10)]
    public string dni_us { get; set; }
        
    [MaxLength(250)]
    public string? image_us { get; set; }
    
    public int age_us { get; set; }
    
    public DateTime birthday_us { get; set; }
    
    [MaxLength(250)]
    public string nationality_us { get; set; } = "Ecuatoriano(a)";
    
    [MaxLength(10)]
    public string phone_us { get; set; }
    
    public char email_verified { get; set; } = 'N';
    
    public char terms_and_conditions { get; set; } = 'N';
    
    public string gender_us { get; set; }
    
    public char status_us { get; set; }
    public string reset_code { get; set; }
    public DateTime? reset_code_expiry { get; set; }
    
    
}