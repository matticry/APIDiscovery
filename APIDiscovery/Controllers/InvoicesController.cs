using APIDiscovery.Interfaces;
using APIDiscovery.Models.DTOs.InvoiceDTOs;
using Microsoft.AspNetCore.Mvc;

namespace APIDiscovery.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IXmlFacturaService _xmlService;

    public InvoicesController(IInvoiceService invoiceService, IXmlFacturaService xmlService)
    {
        _invoiceService = invoiceService;
        _xmlService = xmlService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateInvoice([FromBody] InvoiceDTO invoiceDto)
    {
        try
        {
            var createdInvoice = await _invoiceService.CreateInvoiceAsync(invoiceDto);
            return Ok(createdInvoice);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            // Log error
            return StatusCode(500, "Error interno del servidor");
        }
    }
    
    /// <summary>
    /// Genera el XML de una factura y lo devuelve como archivo para validación.
    /// </summary>
    [HttpGet("generate-xml/{invoiceId}")]
    public async Task<IActionResult> GenerateXmlAsync(int invoiceId)
    {
        try
        {
            var filePath = await _xmlService.GenerarXmlFacturaAsync(invoiceId);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"Archivo XML no encontrado: {filePath}");
            }

            var xmlContent = System.IO.File.ReadAllBytes(filePath);
            var fileName = Path.GetFileName(filePath);

            return File(xmlContent, "application/xml", fileName);
        }
        catch (Exception ex)
        {
            var errorDetail = ex.InnerException != null
                ? $"{ex.Message} | Inner: {ex.InnerException.Message} | StackTrace: {ex.InnerException.StackTrace}"
                : $"{ex.Message} | StackTrace: {ex.StackTrace}";

            return StatusCode(500, $"Error al generar el XML: {errorDetail}");
        }
    }
}