using APIDiscovery.Core;
using APIDiscovery.Interfaces;
using APIDiscovery.Models.DTOs.InvoiceDTOs;
using Microsoft.AspNetCore.Mvc;

namespace APIDiscovery.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<InvoicesController> _logger;
    private readonly ISriComprobantesService _sriService;
    private readonly IXmlFacturaService _xmlService;
    private readonly ApplicationDbContext _context;

    public InvoicesController(
        IInvoiceService invoiceService,
        IXmlFacturaService xmlService,
        ApplicationDbContext context,
        ISriComprobantesService sriService,
        ILogger<InvoicesController> logger)
    {
        _invoiceService = invoiceService;
        _xmlService = xmlService;
        _sriService = sriService;
        _logger = logger;
        _context = context;
    }


    [HttpPost("send-to-sri/{invoiceId}")]
    public async Task<IActionResult> SendToSriAsync(int invoiceId)
    {
        try
        {
            // Llamar al servicio del SRI
            var sriResponse = await _sriService.EnviarComprobanteAsync(invoiceId);

            // Devolver respuesta estructurada
            return Ok(new
            {
                Status = sriResponse.Estado,
                Messages = sriResponse.Mensajes,
                RawResponse = sriResponse.XmlResponse // Opcional para depuración
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar factura al SRI: {InvoiceId}", invoiceId);
            return StatusCode(500, new { Error = ex.Message });
        }
    }

// Método alternativo que acepta explícitamente el ID de factura en la ruta
    [HttpPost("autorizar/{claveAcceso}/factura/{invoiceId}")]
    public async Task<IActionResult> AutorizarComprobanteConId(string claveAcceso, int invoiceId)
    {
        try
        {
            // 1) Validación de la clave de acceso (debe tener 49 caracteres)
            if (string.IsNullOrEmpty(claveAcceso) || claveAcceso.Length != 49)
                return BadRequest(new { Error = "Clave de acceso inválida. Debe tener 49 caracteres." });

            // 2) Validar que la factura exista
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null) return NotFound(new { Error = $"No se encontró la factura con ID: {invoiceId}" });

            // 3) Llamar al servicio de autorización con el ID de factura
            var resultado =
                await _sriService.AutorizarComprobanteAsync(claveAcceso, invoiceId);

            // 4) Verificar si hubo error interno
            if (!string.IsNullOrEmpty(resultado.Error))
            {
                _logger.LogWarning("Error en autorización SRI: {Error}", resultado.Error);
                return StatusCode(502, new { Error = "Error comunicándose con el SRI", Detalles = resultado.Error });
            }

            // 5) Devolver 200 OK con el payload
            return Ok(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción inesperada en AutorizarComprobanteConId");
            return StatusCode(500, new { Error = "Error inesperado en el servidor", Detalles = ex.Message });
        }
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
    ///     Genera el XML de una factura y lo devuelve como archivo para validación.
    /// </summary>
    [HttpGet("generate-xml/{invoiceId}")]
    public async Task<IActionResult> GenerateXmlAsync(int invoiceId)
    {
        try
        {
            var filePath = await _xmlService.GenerarXmlFacturaAsync(invoiceId);

            if (!System.IO.File.Exists(filePath)) return NotFound($"Archivo XML no encontrado: {filePath}");

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
}