using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDiscovery.Models;

[Table("tbl_venta_producto_usuario")]
public class VentaProductoUsuario
{
    [Key]
    public int id_vpu { get; set; }
        
    public int id_vendedor { get; set; }
        
    public int id_comprador { get; set; }
        
    public int id_pro { get; set; }
        
    public DateTime created_at { get; set; } = DateTime.Now;
        
    [ForeignKey("id_vendedor")]
    public virtual Usuario Vendedor { get; set; }
        
    [ForeignKey("id_comprador")]
    public virtual Usuario Comprador { get; set; }
        
    [ForeignKey("id_pro")]
    public virtual Product Producto { get; set; }
}