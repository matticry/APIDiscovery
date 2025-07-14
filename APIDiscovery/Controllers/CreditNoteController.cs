using APIDiscovery.Exceptions;
using APIDiscovery.Interfaces;
using APIDiscovery.Models.DTOs.CreditNoteDTOs;
using APIDiscovery.Models.DTOs.SriDTOs;
using Microsoft.AspNetCore.Mvc;

namespace APIDiscovery.Controllers;
[ApiController]
[Route("api/[controller]")]

public class CreditNoteController(
    ICreditNoteService creditNoteService,
    ILogger<CreditNoteController> logger,
    ISriCreditNoteService sriCreditNoteService)
    : ControllerBase
{
    [HttpPost("{creditNoteId}/enviar")]
    public async Task<ActionResult<SriResponse>> EnviarNotaCredito(int creditNoteId)
    {
        try
        {
            logger.LogInformation($"Iniciando envío de nota de crédito {creditNoteId} al SRI");

            var response = await sriCreditNoteService.EnviarNotaCreditoAsync(creditNoteId);

            // ✅ CORRECCIÓN: Estados válidos del SRI para envío exitoso
            var estadosExitosos = new[] { "RECIBIDA", "OK", "DEVUELTA" };
            
            if (estadosExitosos.Contains(response.Estado))
            {
                logger.LogInformation($"Nota de crédito {creditNoteId} enviada exitosamente al SRI. Estado: {response.Estado}");
                
                return Ok(response);
            }

            logger.LogWarning($"Nota de crédito {creditNoteId} no fue aceptada por el SRI. Estado: {response.Estado}");
                
            return BadRequest(new
            {
                error = "Error al enviar nota de crédito al SRI",
                creditNoteId = creditNoteId,
                estado = response.Estado,
                mensajes = response.Mensajes?.Select(m => m.Mensaje).ToList() ?? new List<string>()
            });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error al enviar nota de crédito {CreditNoteId} al SRI", creditNoteId);
            return StatusCode(500, "Error interno del servidor");
        }
    }

    [HttpPost("{creditNoteId}/autorizar")]
    public async Task<ActionResult<SriAutorizacionResponse>> AutorizarNotaCredito(int creditNoteId, [FromQuery] string? claveAcceso = null)
    {
        try
        {
            logger.LogInformation($"Iniciando autorización de nota de crédito {creditNoteId}");

            // Si no se proporciona clave de acceso, obtenerla de la base de datos
            if (string.IsNullOrEmpty(claveAcceso))
            {
                var creditNote = await creditNoteService.GetCreditNoteDtoById(creditNoteId);
                if (creditNote == null)
                {
                    return NotFound($"No se encontró la nota de crédito con ID {creditNoteId}");
                }

                claveAcceso = creditNote.AccessKey;
                if (string.IsNullOrEmpty(claveAcceso))
                {
                    return BadRequest($"La nota de crédito {creditNoteId} no tiene clave de acceso generada");
                }
            }

            logger.LogInformation($"Consultando autorización para clave de acceso: {claveAcceso}");

            var response = await sriCreditNoteService.AutorizarNotaCreditoAsync(claveAcceso, creditNoteId);

            if (!string.IsNullOrEmpty(response.Error))
            {
                logger.LogError($"Error al autorizar nota de crédito {creditNoteId}: {response.Error}");
                return BadRequest(new
                {
                    error = "Error al autorizar en el SRI",
                    message = response.Error,
                    claveAcceso = response.ClaveAccesoConsultada,
                    creditNoteId
                });
            }

            var estadoAutorizacion = response.Autorizaciones?.FirstOrDefault()?.Estado ?? "DESCONOCIDO";
            logger.LogInformation($"Nota de crédito {creditNoteId} procesada. Estado: {estadoAutorizacion}");

            return Ok(response);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error al autorizar nota de crédito {CreditNoteId} al SRI", creditNoteId);
            return StatusCode(500, "Error interno del servidor");
        }
    }

    [HttpPost]
    public async Task<ActionResult<CreditNoteDTO>> CreateCreditNote([FromBody] CreateCreditNoteDTO createCreditNoteDto)
    {
        try
        {
            logger.LogInformation("Creando nota de crédito para factura {InvoiceId}", createCreditNoteDto.InvoiceOriginalId);

            var creditNoteDto = await creditNoteService.CreateCreditNoteAsync(createCreditNoteDto);
            logger.LogInformation("Nota de crédito {CreditNoteId} creada exitosamente", creditNoteDto.IdCreditNote);

            return Ok(creditNoteDto);

        }
        catch (EntityNotFoundException ex)
        {
            logger.LogWarning(ex, "Entidad no encontrada al crear nota de crédito");
            return NotFound(ex.Message);
        }
        catch (BusinessException ex)
        {
            logger.LogWarning(ex, "Error de validación al crear nota de crédito");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error interno al crear nota de crédito");
            return StatusCode(500, "Error interno del servidor");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CreditNoteDTO>> GetCreditNote(int id)
    {
        try
        {
            var creditNote = await creditNoteService.GetCreditNoteDtoById(id);
            return Ok(creditNote);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener nota de crédito {CreditNoteId}", id);
            return StatusCode(500, "Error interno del servidor");
        }
    }

    [HttpGet("{id}/xml")]
    public async Task<ActionResult<string>> GetCreditNoteXml(int id)
    {
        try
        {
            var xml = await creditNoteService.GenerateCreditNoteXmlAsync(id);
            return Ok(xml);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al generar XML de nota de crédito {CreditNoteId}", id);
            return StatusCode(500, "Error interno del servidor");
        }
        
    }
    
    [HttpGet("GetAllCreditNotesByEnterpriseId/{enterpriseId}")]
    public async Task<IActionResult> GetAllCreditNotesByEnterpriseId(int enterpriseId)
    {
        try
        {
            var creditNotes = await creditNoteService.GetAllCreditNotesByEnterpriseId(enterpriseId);
            return Ok(creditNotes);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener notas de crédito para la empresa {EnterpriseId}", enterpriseId);
            return StatusCode(500, "Error interno del servidor");
        }
    }
    
}