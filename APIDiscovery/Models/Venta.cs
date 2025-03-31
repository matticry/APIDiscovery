using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDiscovery.Models;

[Table("tbl_ventas")]
public class Venta
{
    [Key]
    public int id_ve { get; set; }
        
    public DateTime date_ve { get; set; } = DateTime.Now;
        
    public char status_ve { get; set; } = 'A';

    public decimal total_ve { get; set; }
        
    [NotMapped]
    public virtual ICollection<VentaProductoUsuario> DetallesVenta { get; set; }
}