
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace APIDiscovery.Models;

[Table("tbl_article")]
public class Article
{
    [Key]
    public int id_ar { get; set; }
    
    public string name { get; set; }
    
    [MaxLength(250)]
    public string code { get; set; }
    
    [Column(TypeName = "decimal(10, 2)")]
    public decimal price_unit { get; set; }
    
    public int stock { get; set; }
    
    public char status { get; set; } = 'A';
    
    public DateTime created_at { get; set; } = DateTime.Now;
    
    public DateTime update_at { get; set; } = DateTime.Now;
    
    public string image { get; set; }
    
    public string description { get; set; }
    
    public int id_enterprise { get; set; }
    
    public int id_category { get; set; }
    
    [ForeignKey("id_enterprise")]
    [JsonIgnore]
    public Enterprise Enterprise { get; set; }
    
    [ForeignKey("id_category")]
    [JsonIgnore]
    public Category Category { get; set; }
    
    [JsonIgnore]
    public ICollection<TariffArticle> TariffArticles { get; set; }
}