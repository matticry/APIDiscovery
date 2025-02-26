using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDiscovery.Models;

[Table("tbl_empresa")]
public class Empresa
{
    [Key]
    public int id_empresa { get; set; }
        
    [Required]
    [MaxLength(250)]
    public string name_empresa { get; set; }
        
    public char status_empresa { get; set; } = 'A';
        
    public DateTime created_at { get; set; } = DateTime.Now;
        
    [MaxLength(10)]
    public string phone_empresa { get; set; } = "9999999999";
        
    [MaxLength(13)]
    public string ruc_empresa { get; set; } = "NO DISPONIBLE";
}