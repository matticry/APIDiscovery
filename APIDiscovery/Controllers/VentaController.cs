using System.Security.Claims;
using APIDiscovery.Models.DTOs;
using APIDiscovery.Services;
using APIDiscovery.Services.Commands;
using Microsoft.AspNetCore.Mvc;

namespace APIDiscovery.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VentaController : ControllerBase
{
    private readonly VentaService _ventaService;
    private readonly RabbitMQService _rabbitMqService;

    public VentaController(VentaService ventaService, RabbitMQService rabbitMqService)
    {
        _ventaService = ventaService;
        _rabbitMqService = rabbitMqService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(VentaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CrearVenta([FromBody] VentaRequest ventaRequest)
    {
        // Obtener el ID del vendedor del token (asumiendo que estás usando JWT)
        int vendedorId;
            
        // Si estás usando JWT
        if (User.Identity.IsAuthenticated)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out vendedorId))
            {
                return BadRequest("No se pudo identificar al vendedor.");
            }
        }
        else
        {
            // Para pruebas o desarrollo, podrías permitir un ID de vendedor fijo
            // En producción, deberías siempre requerir autenticación
            vendedorId = 1; // Usuario de prueba
        }

        var response = await _ventaService.CrearVentaAsync(ventaRequest, vendedorId);
            
        // Registrar la acción en RabbitMQ
        _rabbitMqService.PublishUserAction(new UserActionEvent 
        { 
            Username = User.Identity?.Name ?? "admin@admin.com", 
            Action = "crear-venta",
            CreatedAt = DateTime.Now,
            Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
        });
            
        return Ok(response);
    }
}