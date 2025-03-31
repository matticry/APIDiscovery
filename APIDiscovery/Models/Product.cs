using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDiscovery.Models;

[Table("tbl_producto")]

public class Product
{
    
    [Key]
    public int id_pro { get; set; }
    
    [Required (ErrorMessage = "El campo nombre del producto es obligatorio.")]
    public string name_pro { get; set; }
    
    [Required(ErrorMessage = "El campo cantidad del producto es obligatorio.")]
    public int amount_pro { get; set; }
    
    [Required(ErrorMessage = "El campo precio del producto es obligatorio.")]
    public decimal price_pro { get; set; }
    
    
    public char status_pro { get; set ;}
    
}