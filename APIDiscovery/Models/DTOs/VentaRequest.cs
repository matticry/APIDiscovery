using System.ComponentModel.DataAnnotations;

namespace APIDiscovery.Models.DTOs;

public class VentaRequest
{
    [Required]
    public int CompradorId { get; set; }
        
    [Required]
    public List<ProductoVentaDTO> Productos { get; set; }
}

public class ProductoVentaDTO
{
    [Required]
    public int ProductoId { get; set; }
        
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
    public int Cantidad { get; set; }
}

public class VentaResponse
{
    public string Message { get; set; }
    public double ResponseTimeMs { get; set; }
    public int VentaId { get; set; }
    public DateTime FechaVenta { get; set; }
    public decimal Total { get; set; }
    public List<DetalleVentaDTO> DetallesVenta { get; set; }
}

public class DetalleVentaDTO
{
    public int ProductoId { get; set; }
    public string NombreProducto { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}