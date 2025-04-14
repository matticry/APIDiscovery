using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace APIDiscovery.Models;

[Table("tbl_tax")]
public class Tax
{
    [Key]
    public int id_ta { get; set; }
    
    [MaxLength(100)]
    public string description { get; set; }
    
    [JsonIgnore]
    public ICollection<Fare> Fares { get; set; }
}