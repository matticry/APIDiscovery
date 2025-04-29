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
    private readonly ISriComprobantesService _sriService;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(
        IInvoiceService invoiceService, 
        IXmlFacturaService xmlService,
        ISriComprobantesService sriService,
        ILogger<InvoicesController> logger)
    {
        _invoiceService = invoiceService;
        _xmlService = xmlService;
        _sriService = sriService;
        _logger = logger;
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
            _logger.LogError(ex, "Error al crear factura");
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

            _logger.LogError(ex, "Error al generar XML para factura ID: {InvoiceId}", invoiceId);
            return StatusCode(500, $"Error al generar el XML: {errorDetail}");
        }
    }
    
    /// <summary>
    /// Genera el XML en Base64 y lo envía al servicio del SRI.
    /// </summary>
    [HttpPost("send-to-sri/{invoiceId}")]
    public async Task<IActionResult> SendToSriAsync(int invoiceId)
    {
        try
        {
            // Generar el XML (usa el servicio existente)
            var filePath = await _xmlService.GenerarXmlFacturaAsync(invoiceId);
        
            if (!System.IO.File.Exists(filePath))
                return NotFound($"Archivo XML no encontrado para la factura {invoiceId}");

            // Leer el contenido del XML y codificarlo en Base64
            var xmlBytes = System.IO.File.ReadAllBytes(filePath);
            var base64Xml = Convert.ToBase64String(xmlBytes);

            // Enviar al servicio del SRI
            var sriResponse = await _sriService.EnviarComprobanteAsync(base64Xml);

            // Devolver respuesta del SRI
            return Ok(sriResponse);
        }
        catch (Exception ex)
        {
            var errorDetail = ex.InnerException != null
                ? $"{ex.Message} | Inner: {ex.InnerException.Message}"
                : ex.Message;

            _logger.LogError(ex, "Error al enviar factura al SRI: {InvoiceId}", invoiceId);
            return StatusCode(500, $"Error SRI: {errorDetail}");
        }
    }

   
}