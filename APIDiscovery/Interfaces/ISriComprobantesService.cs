using APIDiscovery.Models.DTOs.SriDTOs;

namespace APIDiscovery.Interfaces;

public interface ISriComprobantesService
{
    
    Task<SriResponse> EnviarComprobanteAsync(string base64Xml);

    
}