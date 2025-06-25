namespace APIDiscovery.Interfaces;

public interface IXmlCreditNoteService
{
    Task<string> GenerarXmlNotaCreditoAsync(int creditNoteId);

}