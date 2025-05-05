using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDiscovery.Models;

[Table("tbl_type_dni")]
public class TypeDni
{
    [Key]
    public int id_t_d { get; set; }

    public string name { get; set; }

}