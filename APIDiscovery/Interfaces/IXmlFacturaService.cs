namespace APIDiscovery.Interfaces;

public interface IXmlFacturaService
{
    Task<string> GenerarXmlFacturaAsync(int invoiceId);
    
}