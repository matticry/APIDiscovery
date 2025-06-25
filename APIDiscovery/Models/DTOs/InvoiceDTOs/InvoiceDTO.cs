namespace APIDiscovery.Models.DTOs.InvoiceDTOs;

public class InvoiceDTO
{
    public int InvoiceId { get; set; }
    public string? InvoiceStatus { get; set; }
    public EnterpriseDTO Enterprise { get; set; }
    public ClientDTO Client { get; set; }
    public EmissionPointDto EmissionPoint { get; set; }
    public BranchDTO Branch { get; set; }
    public DocumentTypeDTO DocumentType { get; set; }
    public SequenceDTO Sequence { get; set; }
    public List<InvoiceDetailDTO> Details { get; set; }
    public List<InvoicePaymentDTO> Payments { get; set; }

    public DateTime EmissionDate { get; set; }
    public decimal TotalWithoutTaxes { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal Tip { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; }
    public string? AccessKey { get; set; }

    public string ElectronicStatus { get; set; }
    public string AuthorizationNumber { get; set; }
    public DateTime? AuthorizationDate { get; set; }
    public string? AdditionalInfo { get; set; }
    public string? Message { get; set; }
    public string? sequenceCode { get; set; } = string.Empty;
    

    // IDs para persistencia
    public int BranchId { get; set; }
    public int CompanyId { get; set; }
    public int EmissionPointId { get; set; }
    public int ReceiptId { get; set; }
    public int SequenceId { get; set; }
}