using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace APIDiscovery.Models;

[Table("tbl_fare")]
public class Fare
{
    [Key]
    public int id_fare { get; set; }
    
    [Column(TypeName = "decimal(5, 2)")]
    public decimal percentage { get; set; }
    
    public string code { get; set; }
    
    public string description { get; set; }
    
    public int id_tax { get; set; }
    
    [ForeignKey("id_tax")]
    [JsonIgnore]
    public Tax Tax { get; set; }
    
    [JsonIgnore]
    public ICollection<TariffArticle> TariffArticles { get; set; }
}