using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace APIDiscovery.Models;

[Table("tbl_category")]
public class Category
{
    [Key]
    public int id_ca { get; set; }
    
    [MaxLength(250)]
    public string name { get; set; }
    
    public char status { get; set; } = 'A';
    
    public int id_enterprise { get; set; }
    
    public string description { get; set; }
    
    public DateTime created_at { get; set; } = DateTime.Now;
    
    public DateTime update_at { get; set; } = DateTime.Now;
    
    [ForeignKey("id_enterprise")]
    [JsonIgnore]
    public Enterprise? Enterprise { get; set; }
    
    [JsonIgnore]
    public ICollection<Article>? Articles { get; set; }
}