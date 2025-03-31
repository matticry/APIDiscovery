using APIDiscovery.Core;
using APIDiscovery.Exceptions;
using APIDiscovery.Models;
using APIDiscovery.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Services.Commands;

public class VentaService
    {
        private readonly ApplicationDbContext _context;

        public VentaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<VentaResponse> CrearVentaAsync(VentaRequest ventaRequest, int vendedorId)
        {
            var startTime = DateTime.Now;
            
            // Validar que el comprador exista
            var comprador = await _context.Usuarios.FirstOrDefaultAsync(u => u.id_us == ventaRequest.CompradorId);
            if (comprador == null)
                throw new NotFoundException("El comprador especificado no existe.");

            // Validar que el vendedor exista
            var vendedor = await _context.Usuarios.FirstOrDefaultAsync(u => u.id_us == vendedorId);
            if (vendedor == null)
                throw new NotFoundException("El vendedor especificado no existe.");

            // Crear la transacción para garantizar la integridad
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Crear la venta
                var venta = new Venta
                {
                    date_ve = DateTime.Now,
                    status_ve = 'A',
                    total_ve = 0 // Se calculará después
                };

                _context.Ventas.Add(venta);
                await _context.SaveChangesAsync();

                decimal totalVenta = 0;
                var detallesResponse = new List<DetalleVentaDTO>();

                // Procesar cada producto
                foreach (var productoDTO in ventaRequest.Productos)
                {
                    // Obtener el producto
                    var producto = await _context.Products.FirstOrDefaultAsync(p => p.id_pro == productoDTO.ProductoId);
                    if (producto == null)
                        throw new NotFoundException($"El producto con ID {productoDTO.ProductoId} no existe.");

                    // Validar que el producto esté activo
                    if (producto.status_pro != 'A')
                        throw new BadRequestException($"El producto {producto.name_pro} no está disponible.");

                    // Validar stock disponible
                    if (producto.amount_pro < productoDTO.Cantidad)
                        throw new BadRequestException($"Stock insuficiente para el producto {producto.name_pro}. Disponible: {producto.amount_pro}");

                    // Calcular subtotal
                    decimal subtotal = producto.price_pro * productoDTO.Cantidad;
                    totalVenta += subtotal;

                    // Actualizar stock
                    producto.amount_pro -= productoDTO.Cantidad;

                    // Crear detalle de venta
                    var detalle = new VentaProductoUsuario
                    {
                        id_vendedor = vendedorId,
                        id_comprador = ventaRequest.CompradorId,
                        id_pro = productoDTO.ProductoId,
                        created_at = DateTime.Now
                    };

                    _context.VentaProductoUsuario.Add(detalle);

                    // Agregar al detalle de respuesta
                    detallesResponse.Add(new DetalleVentaDTO
                    {
                        ProductoId = producto.id_pro,
                        NombreProducto = producto.name_pro,
                        Cantidad = productoDTO.Cantidad,
                        PrecioUnitario = producto.price_pro,
                        Subtotal = subtotal
                    });
                }

                // Actualizar el total de la venta
                venta.total_ve = totalVenta;
                await _context.SaveChangesAsync();

                // Confirmar la transacción
                await transaction.CommitAsync();

                var endTime = DateTime.Now;
                var responseTimeMs = (endTime - startTime).TotalMilliseconds;

                // Preparar respuesta
                return new VentaResponse
                {
                    Message = "Venta registrada exitosamente",
                    ResponseTimeMs = responseTimeMs,
                    VentaId = venta.id_ve,
                    FechaVenta = venta.date_ve,
                    Total = venta.total_ve,
                    DetallesVenta = detallesResponse
                };
            }
            catch (Exception)
            {
                // Si algo falla, revertir la transacción
                await transaction.RollbackAsync();
                throw; // Re-lanzar la excepción para que el middleware la capture
            }
        }
    }