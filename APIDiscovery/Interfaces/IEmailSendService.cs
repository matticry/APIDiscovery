namespace APIDiscovery.Interfaces;

public interface IEmailSendService
{
    Task<bool> SendInvoiceEmailAsync(int invoiceId);

}