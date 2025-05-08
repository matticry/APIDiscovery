using APIDiscovery.Models.DTOs;

namespace APIDiscovery.Interfaces;

public interface ICedulaService
{
    Task<CedulaResponse> ConsultarCedulaAsync(string numeroCedula, string ipSolicitante);
    Task<RucResponse> ConsultarRucAsync(string numeroRuc, string ipSolicitante);


}