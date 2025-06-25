using APIDiscovery.Models.DTOs.InvoiceDTOs;

namespace APIDiscovery.Models.DTOs.CreditNoteDTOs;

public class CreditNoteDTO
{
    public int IdCreditNote { get; set; }
    public DateTime EmissionDate { get; set; }
    public decimal TotalWithoutTaxes { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal Tip { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; }
    public string CodDocModificado { get; set; }
    public string NumDocModificado { get; set; }
    public DateTime EmissionDateDocSustento { get; set; }
    public decimal ModificationValue { get; set; }
    public string Motive { get; set; }
    public string AccessKey { get; set; }
    public string AuthorizationNumber { get; set; }
    public DateTime? AuthorizationDate { get; set; }
    public string ElectronicStatus { get; set; }
    public string AditionalInfo { get; set; }
    public string Message { get; set; }
    public string Sequence { get; set; }
    public string Xml { get; set; }

    // Relaciones
    public EnterpriseDTO Enterprise { get; set; }
    public BranchDTO Branch { get; set; }
    public EmissionPointDto EmissionPoint { get; set; }
    public ClientDTO Client { get; set; }
    public DocumentTypeDTO DocumentType { get; set; }
    public InvoiceDTO InvoiceOriginal { get; set; }
    public List<CreditNoteDetailDTO> Details { get; set; } = [];
}