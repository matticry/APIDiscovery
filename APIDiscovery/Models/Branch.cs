using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace APIDiscovery.Models;

[Table("tbl_branch")]
public class Branch
{
    [Key]
    public int id_br { get; set; }
    
    [MaxLength(250)]
    public string? code { get; set; }
    
    [MaxLength(250)]
    public string? description { get; set; }
    
    public int id_enterprise { get; set; }
    
    [MaxLength(250)]
    public string? address { get; set; }
    
    [MaxLength(10)]
    public string? phone { get; set; }
    
    public char status { get; set; } = 'A';
    
    public DateTime created_at { get; set; } = DateTime.Now;
    
    [ForeignKey("id_enterprise")]
    [JsonIgnore]
    public Enterprise Enterprise { get; set; }
}