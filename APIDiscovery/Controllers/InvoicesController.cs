using APIDiscovery.Core;
using APIDiscovery.Exceptions;
using APIDiscovery.Interfaces;
using APIDiscovery.Models.DTOs.InvoiceDTOs;
using Microsoft.AspNetCore.Authorization;
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
            var sriResponse = await _sriService.EnviarComprobanteAsync(invoiceId);

            return Ok(new
            {
                Status = sriResponse.Estado,
                Messages = sriResponse.Mensajes,
                RawResponse = sriResponse.XmlResponse 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar factura al SRI: {InvoiceId}", invoiceId);
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    [HttpPost("autorizar/{claveAcceso}/factura/{invoiceId}")]
    public async Task<IActionResult> AutorizarComprobanteConId(string claveAcceso, int invoiceId)
    {
        try
        {
            if (string.IsNullOrEmpty(claveAcceso) || claveAcceso.Length != 49)
                return BadRequest(new { Error = "Clave de acceso inválida. Debe tener 49 caracteres." });

            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null) return NotFound(new { Error = $"No se encontró la factura con ID: {invoiceId}" });

            var resultado =
                await _sriService.AutorizarComprobanteAsync(claveAcceso, invoiceId);

            if (string.IsNullOrEmpty(resultado.Error)) return Ok(resultado);
            _logger.LogWarning("Error en autorización SRI: {Error}", resultado.Error);
            return StatusCode(502, new { Error = "Error comunicándose con el SRI", Detalles = resultado.Error });

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
    
    
    [HttpGet("GetInvoiceById/{invoiceId}")]
    public async Task<IActionResult> GetInvoiceById(int invoiceId)
    {
        try
        {
            var invoice = await _invoiceService.GetInvoiceDtoById(invoiceId);
            return Ok(invoice);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
    
    [HttpGet("GetXmlBase64ByInvoiceId/{invoiceId}")]
    public async Task<IActionResult> GetXmlBase64ByInvoiceId(int invoiceId)
    {
        try
        {
            var invoice = await _invoiceService.GetXmlBase64ByInvoiceId(invoiceId);
            return Ok(invoice);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
    
    [HttpGet("GetUnauthorizedInvoicesByEnterpriseId/{enterpriseId}")]
    public async Task<IActionResult> GetUnauthorizedInvoicesByEnterpriseId(int enterpriseId)
    {
        try
        {
            var invoices = await _invoiceService.GetUnauthorizedInvoicesByEnterpriseId(enterpriseId);
            return Ok(invoices);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(ex.Message);
            
        }
    }
    
    [HttpGet("GetAuthorizedBusinessInvoicesByEnterpriseId/{enterpriseId}")]
    public async Task<IActionResult> GetAuthorizedBusinessInvoicesByEnterpriseId(int enterpriseId)
    {
        try
        {
            var invoices = await _invoiceService.GetAuthorizedBusinessInvoicesByEnterpriseId(enterpriseId);
            return Ok(invoices);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(ex.Message);
            
        }
    }
    
    [HttpGet("GetTotalInvoiceAuthorizedCountByCompanyIdAsync/{enterpriseId}")]
    public async Task<IActionResult> GetTotalInvoiceAuthorizedCountByCompanyIdAsync(int enterpriseId)
    {
        try
        {
            var totalInvoices = await _invoiceService.GetTotalInvoiceAuthorizedCountByCompanyIdAsync(enterpriseId);
            return Ok(totalInvoices);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(ex.Message);
            
        }
    }
    
    [HttpGet("GetTotalInvoiceUnAuthorizedCountByCompanyIdAsync/{enterpriseId}")]
    public async Task<IActionResult> GetTotalInvoiceUnAuthorizedCountByCompanyIdAsync(int enterpriseId)
    {
        try
        {
            var totalInvoices = await _invoiceService.GetTotalInvoiceUnAuthorizedCountByCompanyIdAsync(enterpriseId);
            return Ok(totalInvoices);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(ex.Message);
            
        }
    }
    
    [HttpGet("GetTotalInvoiceCountByCompanyIdAsync/{enterpriseId}")]
    public async Task<IActionResult> GetTotalInvoiceCountByCompanyIdAsync(int enterpriseId)
    {
        try
        {
            var totalInvoices = await _invoiceService.GetTotalInvoiceCountByCompanyIdAsync(enterpriseId);
            return Ok(totalInvoices);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(ex.Message);
            
        }
    }
    
    [HttpGet("GetTopInvoicesByCompanyIdAsync/{enterpriseId}/{topCount:int}")]
    public async Task<IActionResult> GetTopInvoicesByCompanyIdAsync(int enterpriseId, int topCount)
    {
        try
        {
            var topInvoices = await _invoiceService.GetTopInvoicesByCompanyIdAsync(enterpriseId, topCount);
            return Ok(topInvoices);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(ex.Message);
            
        }
    }
    
    [HttpGet("GetTotalInvoiceAmountByCompanyIdAsync/{enterpriseId}")]
    public async Task<IActionResult> GetTotalInvoiceAmountByCompanyIdAsync(int enterpriseId)
    {
        try
        {
            var totalAmount = await _invoiceService.GetTotalInvoiceAmountByCompanyIdAsync(enterpriseId);
            return Ok(totalAmount);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(ex.Message);
            
        }
    }
    
    [HttpGet("GetAuthorizedInvoicesByEnterpriseId/{enterpriseId}")]
    public async Task<IActionResult> GetAuthorizedInvoicesByEnterpriseId(int enterpriseId)
    {
        try
        {
            var invoices = await _invoiceService.GetAuthorizedInvoicesByEnterpriseId(enterpriseId);
            return Ok(invoices);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(ex.Message);
            
        }
    }
    
    
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