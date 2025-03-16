using APIDiscovery.Models.DTOs;

namespace APIDiscovery.Interfaces;

public interface ICedulaService
{
    Task<CedulaResponse> ConsultarCedulaAsync(string numeroCedula, string ipSolicitante);

}