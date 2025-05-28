using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace APIDiscovery.Models;

[Table("tbl_client")]
public class Client
{
    [Key]
    public int id_client { get; set; }

    [MaxLength(250)]
    public string razon_social { get; set; }

    [MaxLength(13)]
    public string dni { get; set; }
    
    public char status { get; set; } = 'A';

    [MaxLength(250)]
    public string address { get; set; }

    [MaxLength(20)]
    public string phone { get; set; }

    [MaxLength(250)]
    public string email { get; set; }


    public string info { get; set; }

    [ForeignKey("id_type_dni")] public int id_type_dni { get; set; }
    
}