using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace APIDiscovery.Models;

[Table("tbl_enterprise_client")]
public class EnterpriseClient
{
    [Key] public int id_en_cl { get; set; }

    [Column ("enterprise_id")] public int enterprise_id { get; set; }

    [Column ("client_id")] public int client_id { get; set; }
    
    [Column ("status")] public char status { get; set; }

    [ForeignKey("client_id")] [JsonIgnore] public virtual Client Client { get; set; }
    [ForeignKey("enterprise_id")] [JsonIgnore] public virtual Enterprise Enterprise { get; set; }
}