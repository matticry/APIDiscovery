using APIDiscovery.Models.DTOs.InvoiceDTOs;

namespace APIDiscovery.Interfaces;

public interface IInvoiceService
{
    Task<InvoiceDTO> CreateInvoiceAsync(InvoiceDTO invoiceDto);
}