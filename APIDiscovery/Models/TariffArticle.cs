using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace APIDiscovery.Models;

[Table("tbl_tariff_article")]
public class TariffArticle
{
    [Key]
    public int id_t_a { get; set; }
    
    public int id_fare { get; set; }
    
    public int id_article { get; set; }
    
    [ForeignKey("id_fare")]
    [JsonIgnore]
    public Fare Fare { get; set; }
    
    [ForeignKey("id_article")]
    [JsonIgnore]
    public Article Article { get; set; }
}