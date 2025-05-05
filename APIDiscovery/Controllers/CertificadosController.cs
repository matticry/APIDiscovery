using APIDiscovery.Interfaces;
using APIDiscovery.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace APIDiscovery.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CertificadosController : ControllerBase
{
    
    private readonly ICertificadoService _certificadoService;
    private readonly ILogger<CertificadosController> _logger;
    

    public CertificadosController(
        ICertificadoService certificadoService,
        ILogger<CertificadosController> logger)
    {
        _certificadoService = certificadoService;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadCertificado([FromForm] CertificadoRequestDto request)
    {
        try
        {
            if (request.Archivo == null || string.IsNullOrEmpty(request.Ruc) || string.IsNullOrEmpty(request.Clave))
                return BadRequest("Archivo, RUC y clave son obligatorios");

            var resultado = await _certificadoService.UploadCertificado(
                request.Archivo, request.Ruc, request.Clave);

            if (!resultado.Success)
                return BadRequest(resultado);

            return Ok(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar certificado");
            return StatusCode(500, "Error interno al procesar la solicitud");
        }
    }

    [HttpGet("validar/{ruc}")]
    public async Task<IActionResult> ValidarCertificado(string ruc)
    {
        try
        {
            var esValido = await _certificadoService.ValidarCertificado(ruc);
            return Ok(new { valido = esValido });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar certificado");
            return StatusCode(500, "Error interno al validar el certificado");
        }
    }
    
}