using System.Threading.Tasks;
using APIDiscovery.Interfaces;
using APIDiscovery.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace APIDiscovery.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CedulaController : ControllerBase
    {
        private readonly ICedulaService _cedulaService;

        public CedulaController(ICedulaService cedulaService)
        {
            _cedulaService = cedulaService;
        }

        /// <summary>
        /// Consulta información de una cédula ecuatoriana
        /// </summary>
        /// <param name="numeroCedula">Número de cédula a consultar</param>
        /// <returns>Información de la cédula</returns>
        [HttpGet("{numeroCedula}")]
        [ProducesResponseType(typeof(CedulaResponse), 200)]
        [ProducesResponseType(typeof(CedulaResponse), 400)]
        [ProducesResponseType(typeof(CedulaResponse), 404)]
        [ProducesResponseType(typeof(CedulaResponse), 500)]
        public async Task<IActionResult> ConsultarCedula(string numeroCedula)
        {
            // Obtener la IP del cliente
            string ipSolicitante = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? 
                                   Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? 
                                   "IP no disponible";            
            var resultado = await _cedulaService.ConsultarCedulaAsync(numeroCedula, ipSolicitante);
            
            // Devolver el código de estado correspondiente según el resultado
            return resultado.StatusCode switch
            {
                200 => Ok(resultado),
                400 => BadRequest(resultado),
                404 => NotFound(resultado),
                _ => StatusCode(resultado.StatusCode, resultado)
            };
        }
        

        [HttpGet ("ruc/{ruc}")]
        [ProducesResponseType(typeof(CedulaResponse), 200)]
        [ProducesResponseType(typeof(CedulaResponse), 400)]
        [ProducesResponseType(typeof(CedulaResponse), 404)]
        public async Task<IActionResult> ConsultarRuc(string ruc)
        {
            // Obtener la IP del cliente
            var ipSolicitante = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? 
                                   Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? 
                                   "IP no disponible";            
            var resultado = await _cedulaService.ConsultarRucAsync(ruc, ipSolicitante);
            
            // Devolver el código de estado correspondiente según el resultado
            return resultado.StatusCode switch
            {
                200 => Ok(resultado),
                400 => BadRequest(resultado),
                404 => NotFound(resultado),
                _ => StatusCode(resultado.StatusCode, resultado)
            };
        }
    }
}