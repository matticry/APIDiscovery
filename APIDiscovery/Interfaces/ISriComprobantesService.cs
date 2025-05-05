using APIDiscovery.Models.DTOs.SriDTOs;

namespace APIDiscovery.Interfaces;

public interface ISriComprobantesService
{
    
    Task<SriResponse> EnviarComprobanteAsync(int invoiceId);
    Task<SriAutorizacionResponse>  AutorizarComprobanteAsync(string claveAcceso, int invoiceId);

    
}