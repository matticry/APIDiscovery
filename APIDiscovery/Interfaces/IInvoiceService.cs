using APIDiscovery.Models.DTOs.InvoiceDTOs;

namespace APIDiscovery.Interfaces;

public interface IInvoiceService
{
    Task<InvoiceDTO> CreateInvoiceAsync(InvoiceDTO invoiceDto);
    Task<InvoiceDTO> GetInvoiceDtoById(int invoiceId);
    Task<List<InvoiceDTO>> GetUnauthorizedInvoicesByEnterpriseId(int enterpriseId);
    Task<List<InvoiceDTO>> GetAuthorizedInvoicesByEnterpriseId(int enterpriseId);

}