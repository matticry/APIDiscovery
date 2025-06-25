using APIDiscovery.Models.DTOs.SriDTOs;

namespace APIDiscovery.Interfaces;

public interface ISriCreditNoteService
{
    Task<SriResponse> EnviarNotaCreditoAsync(int creditNoteId);
    Task<SriAutorizacionResponse> AutorizarNotaCreditoAsync(string claveAcceso, int creditNoteId);
}