using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace APIDiscovery.Models;

[Table("tbl_payment")]
public class Payment
{
    [Key]
    public int id_payment { get; set; }

    public string sri_detail { get; set; }

    public string detail { get; set; }

    public bool status { get; set; }

}