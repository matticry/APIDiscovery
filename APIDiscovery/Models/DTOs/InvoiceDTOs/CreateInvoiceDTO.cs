namespace APIDiscovery.Models.DTOs.InvoiceDTOs;

public class CreateInvoiceDTO
{
    public DateTime EmissionDate { get; set; }
    public decimal TotalWithoutTaxes { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal Tip { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; }
    public int SequenceId { get; set; }
    public int EmissionPointId { get; set; }
    public int CompanyId { get; set; }
    public int BranchId { get; set; }
    public int ReceiptId { get; set; }  // Tipo de documento (factura)
    
    // Datos del cliente (solo si total > 50)
    public ClientDTO Client { get; set; }

    public List<InvoiceDetailDTO> Details { get; set; }
    public List<InvoicePaymentDTO> Payments { get; set; }
}