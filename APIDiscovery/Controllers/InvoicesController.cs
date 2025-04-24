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
    /// Envía la factura al servicio web del SRI para validación.
    /// </summary>
    [HttpPost("send-to-sri/{invoiceId}")]
    public async Task<IActionResult> SendToSriAsync(int invoiceId)
    {
        try
        {
            // Primero generamos el XML si no existe
            string filePath;
            try
            {
                filePath = await _xmlService.GenerarXmlFacturaAsync(invoiceId);
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound($"No se pudo generar el archivo XML para la factura ID: {invoiceId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar XML para envío al SRI, factura ID: {InvoiceId}", invoiceId);
                return BadRequest($"Error al generar XML: {ex.Message}");
            }

            // Enviamos al SRI
            var sriResponse = await _sriService.EnviarComprobanteAsync(invoiceId);
            
            // Determinamos el código de estado HTTP basado en la respuesta del SRI
            if (sriResponse.Estado == "RECIBIDA")
            {
                return Ok(new
                {
                    Success = true,
                    Message = "Comprobante recibido correctamente por el SRI",
                    Response = sriResponse
                });
            }
            else if (sriResponse.Estado == "DEVUELTA")
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "El comprobante fue devuelto por el SRI",
                    Response = sriResponse
                });
            }
            else
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"Estado desconocido: {sriResponse.Estado}",
                    Response = sriResponse
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar factura al SRI, ID: {InvoiceId}", invoiceId);
            return StatusCode(500, $"Error al comunicarse con el SRI: {ex.Message}");
        }
    }

    /// <summary>
    /// Genera el XML y lo envía al SRI, devolviendo un informe completo del proceso.
    /// </summary>
    [HttpPost("process-electronic-invoice/{invoiceId}")]
    public async Task<IActionResult> ProcessElectronicInvoiceAsync(int invoiceId)
    {
        try
        {
            // Paso 1: Generar XML
            _logger.LogInformation("Iniciando proceso de facturación electrónica para factura ID: {InvoiceId}", invoiceId);
            var filePath = await _xmlService.GenerarXmlFacturaAsync(invoiceId);
            
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("No se pudo generar el XML de la factura.");
            }
            
            // Paso 2: Enviar al SRI
            var sriResponse = await _sriService.EnviarComprobanteAsync(invoiceId);
            
            // Paso 3: Devolver resultado completo
            return Ok(new
            {
                InvoiceId = invoiceId,
                XmlPath = filePath,
                SriResponse = sriResponse,
                ProcessDate = DateTime.Now,
                Status = sriResponse.Estado,
                Messages = sriResponse.Mensajes?.Select(m => $"{m.Identificador}: {m.Mensaje}").ToList() ?? new List<string>()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en el proceso de facturación electrónica para factura ID: {InvoiceId}", invoiceId);
            return StatusCode(500, new
            {
                Success = false,
                Message = "Error en el proceso de facturación electrónica",
                Error = ex.Message,
                InnerError = ex.InnerException?.Message,
                InvoiceId = invoiceId
            });
        }
    }
}