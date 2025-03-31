using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDiscovery.Models;
[Table("tbl_childrens")]
public class Children
{
    [Key]
    public int id_ch { get; set; }
    
    [Required (ErrorMessage = "El campo name es obligatorio")]
    public string name_ch { get; set; }
    
    [Required (ErrorMessage = "El campo lastname es obligatorio")]
    public string lastname_ch { get; set; }
    
    [Required (ErrorMessage = "El campo dni es obligatorio")]
    public string dni_ch { get; set; }
    
    [Required (ErrorMessage = "El campo birthday es obligatorio")]
    public DateTime birthday_ch { get; set; }
    
    [Required (ErrorMessage = "El campo gender es obligatorio")]
    public string gender_ch { get; set; }
    
    public Decimal weight_ch { get; set; }
    
    public Decimal height_ch { get; set; }
    
    [Required (ErrorMessage = "El campo age es obligatorio")]
    public int age_ch { get; set; }
    
    [Required (ErrorMessage = "El campo id_usU es obligatorio")]
    public int id_usu { get; set; }
    
    
    
}