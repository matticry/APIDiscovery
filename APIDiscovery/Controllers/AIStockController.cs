using APIDiscovery.Interfaces;
using APIDiscovery.Models.DTOs.IADTOs;
using Microsoft.AspNetCore.Mvc;

namespace APIDiscovery.Controllers;
[ApiController]
[Route("api/[controller]")]
public class AiStockController: ControllerBase
{
    private readonly IAiService _aiStockService;
    private readonly ILogger<AiStockController> _logger;
    
    public AiStockController(IAiService aiStockService, ILogger<AiStockController> logger)
    {
        _aiStockService = aiStockService;
        _logger = logger;
    }
    
    [HttpGet("empresa/{enterpriseId}/reporte-stock")]
    public async Task<IActionResult> GetLowStockReport(int enterpriseId)
    {
        try
        {
            _logger.LogInformation($"🤖 Solicitud de reporte de stock con IA para empresa {enterpriseId}");

            var result = await _aiStockService.GetLowStockReportAsync(enterpriseId);

            if (result.Success)
            {
                _logger.LogInformation($"✅ Reporte generado: {result.TotalLowStockItems} productos con stock bajo");
                return Ok(result);
            }
            else
            {
                _logger.LogWarning($"⚠️ Error en reporte: {result.Message}");
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error al generar reporte para empresa {enterpriseId}");
            return StatusCode(500, new AIStockReportResponse
            {
                Success = false,
                Message = $"Error interno: {ex.Message}",
                EnterpriseId = enterpriseId
            });
        }
    }
    
}