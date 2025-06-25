using APIDiscovery.Models.DTOs.CreditNoteDTOs;

namespace APIDiscovery.Interfaces;

public interface ICreditNoteService
{
    Task<CreditNoteDTO> CreateCreditNoteAsync(CreateCreditNoteDTO creditNoteDto);
    Task<CreditNoteDTO> GetCreditNoteDtoById(int creditNoteId);
    Task<string> GenerateCreditNoteXmlAsync(int creditNoteId);
}