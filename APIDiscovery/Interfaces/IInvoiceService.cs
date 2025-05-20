using APIDiscovery.Models.DTOs.InvoiceDTOs;

namespace APIDiscovery.Interfaces;

public interface IInvoiceService
{
    Task<InvoiceDTO> CreateInvoiceAsync(InvoiceDTO invoiceDto);
    Task<InvoiceDTO> GetInvoiceDtoById(int invoiceId);
    Task<List<InvoiceDTO>> GetUnauthorizedInvoicesByEnterpriseId(int enterpriseId);
    Task<List<InvoiceDTO>> GetAuthorizedInvoicesByEnterpriseId(int enterpriseId);
    Task<int> GetTotalInvoiceCountByCompanyIdAsync(int companyId);
    Task<int> GetTotalInvoiceAuthorizedCountByCompanyIdAsync(int companyId);
    Task<int> GetTotalInvoiceUnAuthorizedCountByCompanyIdAsync(int companyId);

    
    Task<decimal> GetTotalInvoiceAmountByCompanyIdAsync(int companyId);
    Task<List<InvoiceSummaryDTO>> GetTopInvoicesByCompanyIdAsync(int companyId, int count = 3);
    Task<string> GetXmlBase64ByInvoiceId(int invoiceId);




}